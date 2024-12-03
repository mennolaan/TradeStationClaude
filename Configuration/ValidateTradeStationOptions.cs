using FluentValidation;
using Microsoft.Extensions.Options;

namespace TradeStation.Configuration;

public class ValidateTradeStationOptions : IValidateOptions<TradeStationOptions>
{
    private readonly IValidator<TradeStationOptions> _validator;

    public ValidateTradeStationOptions(IValidator<TradeStationOptions> validator)
    {
        _validator = validator;
    }

    public ValidateOptionsResult Validate(string? name, TradeStationOptions options)
    {
        var validationResult = _validator.Validate(options);
        if (validationResult.IsValid)
            return ValidateOptionsResult.Success;

        var errors = validationResult.Errors.Select(x => x.ErrorMessage);
        return ValidateOptionsResult.Fail(errors);
    }
}