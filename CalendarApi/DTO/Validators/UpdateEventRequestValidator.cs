using FluentValidation;

namespace CalendarApi.DTO.Validators;

public class UpdateEventRequestValidator : AbstractValidator<UpdateEventRequest>
{
    public UpdateEventRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Description).MaximumLength(4000);
        RuleFor(x => x.End).GreaterThan(x => x.Start);
        RuleFor(x => x.Recurrence!).SetValidator(new RecurrencePatternDtoValidator())
            .When(x => x.Recurrence is not null);
    }
}
