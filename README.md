# Calendar API

Web API pro správu uživatelů a kalendářových událostí (C# / .NET 10). Řeší CRUD událostí, pozvánky účastníků, kontrolu časových konfliktů včetně opakujících se událostí a dotazování v časovém rozsahu.

## Spuštění

```bash
dotnet run --project CalendarApi
```

V Development prostředí běží OpenAPI dokumentace na `/swagger` (výchozí URL: `http://localhost:5262`). Při startu se automaticky aplikují EF Core migrace na SQLite databázi (`calendar.db`).

Autentizované požadavky vyžadují hlavičku:

```http
X-User-Id: <guid uživatele>
```

---

## Rozhodnutí a kompromisy

### Autentizace přes `X-User-Id` místo JWT

Zadání nepožadovalo konkrétní auth mechanismus. Místo plné JWT/OAuth infrastruktury (issuer, signing keys, refresh tokeny) je identita volajícího reprezentována hlavičkou `X-User-Id`. Rozhraní `ICurrentUserAccessor` je ale oddělené od controllerů — v produkci by šlo snadno nahradit validací JWT claimu bez změny domény ani služeb.

**Kompromis:** žádná kryptografická důvěra v identitu; kdokoli s platným GUID může vystupovat jako daný uživatel. Pro technický úkol a lokální vývoj je to dostatečné a výrazně zjednodušuje testování (Swagger, curl).

### SQLite a automatické migrace při startu

Relační databáze přes EF Core + SQLite — bez externích závislostí (Docker, cloud DB). Migrace se spouštějí při každém startu aplikace, aby šlo repozitář rozbalit a hned spustit.

**Kompromis:** SQLite není ideální pro vysokou souběžnost zápisů; u produkčního nasazení bych zvolil PostgreSQL/SQL Server a migrace by běžely v CI/CD, ne implicitně v aplikaci.

### Jeden záznam = celá opakující se série

Opakující se událost je uložena jako jeden řádek s `RecurrencePattern` (frekvence, interval, `Until` nebo `Count`) a sloupcem **`SeriesEnd`** (efektivní konec série pro prefilter a konflikty). Jednotlivé instance se **počítají za běhu** (`RecurrenceExpander`) při dotazu v rozsahu i při kontrole konfliktů.

**Výhody:** jednoduchý model, úprava celé série jedním `PUT`, konzistentní pravidlo „celá série se chová jako jedna entita“.

**Kompromisy:**
- nelze upravit/smazat jednu ojedinělou instanci bez změny celé série (jako výjimka v iCal);
- kontrola konfliktů prochází kandidáty v paměti a expanduje recurrence — u velkého počtu sérií by bylo vhodnější materializovat instance nebo posílit DB dotaz.

### Účastníci jako seznam GUID v jednom sloupci

`ParticipantIds` jsou v SQLite uloženy jako CSV řetězec (EF value converter), dotazy používají `LIKE` pro členství.

**Kompromis:** rychlá implementace bez junction tabulky; horší škálovatelnost a indexování oproti `EventParticipants(user_id, event_id)`.

### Vrstvená architektura

- **Domain** — `CalendarEvent`, `User`, pravidla (viditelnost, validace recurrence), doménové výjimky.
- **Services** — orchestrace (`CalendarEventSchedulingService`), expandace recurrence, kontrola konfliktů.
- **Infrastructure** — EF Core, `CurrentUserAccessor`, mapování výjimek na Problem Details.
- **Controllers + DTO** — HTTP, FluentValidation.

Doménová logika není v controllerech; perzistence jde přes rozhraní `IUserRepository`, `ICalendarEventRepository`, `IUnitOfWork`.

### Veřejné list endpointy

`GET /api/v1/events` a `GET /api/v1/users` vrací stránkovaná data bez autentizace — záměrně pro vývoj a ruční testování. V produkci by byly zakázané nebo za admin rolí.

### Chybí unit testy

V repozitáři nejsou automatizované testy — U produkční aplikace by to byl záměrný dluh.

**Kompromis:** rychlejší dodání řešení úkolu, ale bez regresní sítě při refaktoringu (zejména u recurrence a konfliktů).

**Co by mělo smysl doplnit jako první:**
- unit testy `RecurrenceExpander` (hranice rozsahu, `Count`/`Until`, limit 500 instancí);
- testy kontroly překryvů v `CalendarEventSchedulingService` (jednorázové vs. opakující se události, více účastníků, vyloučení upravované události);
- testy domény (`CalendarEvent`, `RecurrencePattern.Validate`);
- integrační testy API (např. `WebApplicationFactory` + in-memory/SQLite) pro hlavní HTTP scénáře a kódy 403/409.

---

## Rozhodnutí tam, kde zadání nebylo jednoznačné

| Téma | Rozhodnutí | Důvod |
|------|------------|--------|
| Kdo může měnit/mazat událost | Pouze **vlastník** | Pozvaný účastník má konfliktové povinnosti, ale nemění definici události — běžný model kalendáře. |
| Kdo může pozvat dalšího uživatele | Pouze vlastník (`POST …/participants`) | Zadání mluví o „pozvání“, ne o tom, že účastník může přidávat další. |
| Viditelnost události | Vlastník + všichni v `ParticipantIds` | Splnění požadavku na přístup k datům; ostatní uživatelé dostanou 403. |
| Dotaz kalendáře uživatele | `GET /users/{userId}/events` jen pokud `userId` = volající | Cizí `userId` v URL by umožnilo odhadovat existenci účtu; vlastní události jako účastník se stejně vrátí přes filtr viditelnosti. |
| Čtení profilu uživatele | `GET /users/{id}` jen vlastní profil | **403** pro cizí ID; **401** bez hlavičky. |
| Hranice časového rozsahu | `from` inclusive, `to` **exclusive** | Konzistentní s běžnými API; snadné skládání intervalů. |
| Povinný rozsah u kalendáře | `from` a `to` povinné, `from < to`, max **366 dní** (`Calendar:MaxRangeDays`) | Ochrana proti dotazům na celou časovou osu. |
| Úprava opakující se události | Celá série najednou | Bez specifikace „výjimky z pravidla“ je editace jedné instance nejasná; jeden záznam v DB to podporuje přirozeně. |
| Recurrence bez konce | Povinné `Until` **nebo** `Count` | Nekonečná série by v praxi vyžadovala jiný model ukládání; validace v `RecurrencePattern` to vynucuje. |
| `Until` vs. začátek série | `Until` musí být ≥ `Start` | Validace v doméně i FluentValidation na DTO. |
| Frekvence opakování | Daily / Weekly / Monthly + `Interval` | Pokrývá bonus bez implementace RRULE/iCal kompletní sady. |
| Konflikt účastníků | Kontrola pro **každého** účastníka včetně vlastníka | Zadání: „pro žádného z účastníků“ — vlastník je v `ParticipantIds` od vytvoření. |
| HTTP kód při konfliktu rozvrhu | **409 Conflict** | Obecná zpráva bez `participantId`; detail v logu. |
| Chybějící `X-User-Id` | **401 Unauthorized** | `UnauthorizedException` — ne závislost na textu výjimky. |
| Paginace list endpointů | Default `take=50`, max `200`; odpověď `{ totalCount, pageSize, items }` | Klient zná celkový počet záznamů. |

---

## Poznámky k implementaci

- **`SeriesEnd`:** při vytvoření/úpravě se počítá přes `RecurrenceSeriesBounds` (`Until`, konec poslední instance z `Count`, nebo 2letý fallback). Používá se v DB prefiltru (`MayHaveInstancesInRange`), okně konfliktů a `ExpandForConflictCheck`.
- **Kontrola překryvů:** před uložením v **serializable transakci** se pro kandidátní událost expandují instance přes `ExpandForConflictCheck` (bez limitu 500 u `Count`). Pro každou instanci a účastníka se hledají jiné události (`HasOverlapForParticipantAsync`). Při kontrole se aktuální událost vylučuje (`excludeEventId`).
- **Limit expandace u range API:** `RecurrenceExpander.Expand` generuje maximálně **500** instancí na volání — ochrana proti runaway smyčce u `GET …/events`; konflikty tento limit nepoužívají.
- **`Until` inclusivity:** instance začínající přesně v čase `Until` je při expandaci **zahrnuta** (podmínka ukončení je `cursor > Until`).
- **Identifikátory:** `Guid.CreateVersion7()` pro časově řazitelné ID uživatelů i událostí.
- **Validace vstupu:** FluentValidation na DTO (včetně `application/problem+json` pro chyby modelu); doména doplňuje pravidla (recurrence, `End > Start`).
- **Chyby:** `DomainExceptionMiddleware` mapuje doménové výjimky na `application/problem+json`.
- **SQLite:** `ORDER BY` na `DateTimeOffset` v listu událostí není použito (omezení provideru) — řazení podle `Id` (komentář v `CalendarDbContext`).
- **Swagger:** pouze v `Development` (`MapOpenApi` + Swagger UI).

### Příklad toku

1. `POST /api/v1/users` — vytvoření uživatele, získání `id`.
2. Další požadavky s `X-User-Id: {id}`.
3. `POST /api/v1/events` — vytvoření události (volitelně `recurrence`).
4. `POST /api/v1/events/{id}/participants` — pozvání (kontrola konfliktu u pozvaného).
5. `GET /api/v1/users/{userId}/events?from=…&to=…` — instance v rozsahu (recurrence rozbalené v odpovědi).

### Breaking API změny (dev)

### Příklad toku

1. `POST /api/v1/users` — vytvoření uživatele, získání `id`.
2. Další požadavky s `X-User-Id: {id}`.
3. `POST /api/v1/events` — vytvoření události (volitelně `recurrence`).
4. `POST /api/v1/events/{id}/participants` — pozvání (kontrola konfliktu u pozvaného).
5. `GET /api/v1/users/{userId}/events?from=…&to=…` — instance v rozsahu (recurrence rozbalené v odpovědi).