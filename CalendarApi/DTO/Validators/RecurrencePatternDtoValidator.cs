using FluentValidation;

namespace CalendarApi.DTO.Validators;

public class RecurrencePatternDtoValidator : AbstractValidator<RecurrencePatternDto>
{
    public RecurrencePatternDtoValidator()
    {
        RuleFor(x => x.Interval).GreaterThanOrEqualTo(1);
        RuleFor(x => x.Frequency).Must(f => new[] { "Daily", "Weekly", "Monthly" }.Contains(f, StringComparer.OrdinalIgnoreCase));
        RuleFor(x => x).Must(x => x.Until is not null || x.Count is not null)
            .WithMessage("Recurrence requires Until or Count.");
        RuleFor(x => x).Must(x => x.Until is null || x.Count is null)
            .WithMessage("Recurrence cannot have both Until and Count.");
        RuleFor(x => x.Count).GreaterThanOrEqualTo(1).When(x => x.Count is not null);
    }
}
