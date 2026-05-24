using FluentValidation;

namespace CalendarApi.DTO.Validators;

public class ListEventsInRangeQueryValidator : AbstractValidator<ListEventsInRangeQuery>
{
    public ListEventsInRangeQueryValidator(IConfiguration configuration)
    {
        var maxRangeDays = configuration.GetValue("Calendar:MaxRangeDays", 366);

        RuleFor(x => x.From)
            .Must(f => f != default)
            .WithMessage("Query parameter 'from' is required.");
        RuleFor(x => x.To)
            .Must(t => t != default)
            .WithMessage("Query parameter 'to' is required.");
        RuleFor(x => x.To).GreaterThan(x => x.From)
            .WithMessage("'to' must be after 'from'.");
        RuleFor(x => x)
            .Must(x => (x.To - x.From).TotalDays <= maxRangeDays)
            .WithMessage($"Time range cannot exceed {maxRangeDays} days.");
    }
}
