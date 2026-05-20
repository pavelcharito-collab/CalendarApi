using FluentValidation;

namespace CalendarApi.DTO.Validators;

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator() =>
        RuleFor(x => x.DisplayName).NotEmpty().MaximumLength(200);
}
