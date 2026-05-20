using FluentValidation;

namespace CalendarApi.DTO.Validators;

public class InviteParticipantRequestValidator : AbstractValidator<InviteParticipantRequest>
{
    public InviteParticipantRequestValidator() =>
        RuleFor(x => x.UserId).NotEmpty();
}
