using FluentValidation;

namespace CalendarApi.DTO;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator() =>
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
}
