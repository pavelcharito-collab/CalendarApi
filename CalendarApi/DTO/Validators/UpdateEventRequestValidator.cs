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
        RuleFor(x => x)
            .Must(x => x.Recurrence!.Until is null || x.Recurrence.Until >= x.Start)
            .WithMessage("Recurrence Until must be on or after series start.")
            .When(x => x.Recurrence?.Until is not null);
    }
}
