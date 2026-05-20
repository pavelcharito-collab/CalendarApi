using FluentValidation;

namespace CalendarApi.DTO;

public class InviteParticipantRequestValidator : AbstractValidator<InviteParticipantRequest>
{
    public InviteParticipantRequestValidator() =>
        RuleFor(x => x.UserId).NotEmpty();
}
