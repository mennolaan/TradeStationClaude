namespace TradeStation.Configuration.ValidationRules;

using FluentValidation;

public class TradeStationOptionsValidation : AbstractValidator<TradeStationOptions>
{
    public TradeStationOptionsValidation()
    {
        RuleFor(x => x.ApiKey)
            .NotEmpty()
            .WithMessage("API Key is required");

        RuleFor(x => x.ApiSecret)
            .NotEmpty()
            .WithMessage("API Secret is required");

        RuleFor(x => x.RefreshToken)
            .NotEmpty()
            .WithMessage("Refresh Token is required");

        RuleFor(x => x.AccountId)
            .NotEmpty()
            .WithMessage("Account ID is required");

        RuleFor(x => x.BaseUrl)
            .NotEmpty()
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("Base URL must be a valid URI");

        RuleFor(x => x.ApiUrl)
            .NotEmpty()
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _))
            .WithMessage("API URL must be a valid URI");
    }
}