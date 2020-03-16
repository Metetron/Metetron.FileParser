using FluentValidation;

namespace Parsnet.WatcherConfiguration
{
    internal class WatcherOptionsValidator : AbstractValidator<WatcherOptions>
    {
        public WatcherOptionsValidator()
        {
            RuleFor(o => o.ParserName)
                .NotEmpty()
                .MinimumLength(3)
                .MaximumLength(20);

            RuleFor(o => o.DirectoryToWatch)
                .NotEmpty();

            RuleFor(o => o.FileSearchPattern)
                .NotEmpty();

            RuleFor(o => o.PollingInterval)
                .GreaterThanOrEqualTo(5000)
                .WithMessage("Minimal polling interval is 5 seconds.");

            RuleFor(o => o.WorkingDirectoryPath)
                .NotEmpty();

            RuleFor(o => o.BackupDirectoryPath)
                .NotEmpty();

            RuleFor(o => o.CheckMainDirectory)
                .Empty()
                .When(o => string.IsNullOrWhiteSpace(o.SubDirectorySearchPattern));
        }
    }
}