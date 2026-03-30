using ArbinInsight.Models;
using FluentValidation;

namespace ArbinInsight.Validators
{
    public class MachineDataUpdateDtoValidator : AbstractValidator<MachineData>
    {
        public MachineDataUpdateDtoValidator()
        {
            RuleFor(x => x.MachineName)
                .NotEmpty().WithMessage("Machine name is required.")
                .MaximumLength(100).WithMessage("Machine name cannot exceed 100 characters.");

            RuleFor(x => x.Status)
                .NotEmpty().WithMessage("Status is required.")
                .MaximumLength(50).WithMessage("Status cannot exceed 50 characters.");

        }
    }
}