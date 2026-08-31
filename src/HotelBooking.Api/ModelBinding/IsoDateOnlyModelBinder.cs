using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HotelBooking.Api.ModelBinding;

/// <summary>
/// Binds DateOnly query/route parameters strictly as yyyy-MM-dd, rejecting anything else
/// with a 400 instead of silently guessing. The default model binder parses using the
/// server's current culture, so "07/09/2026" could mean 7 September or 9 July depending
/// on where it's hosted - an ambiguity an API should never resolve by guessing.
/// </summary>
public class IsoDateOnlyModelBinder : IModelBinder
{
    private const string Format = "yyyy-MM-dd";

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var valueProviderResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueProviderResult == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueProviderResult);

        var value = valueProviderResult.FirstValue;
        if (string.IsNullOrEmpty(value))
        {
            return Task.CompletedTask;
        }

        if (DateOnly.TryParseExact(value, Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
        {
            bindingContext.Result = ModelBindingResult.Success(date);
        }
        else
        {
            bindingContext.ModelState.TryAddModelError(
                bindingContext.ModelName, $"'{value}' is not a valid date. Use the {Format} format.");
        }

        return Task.CompletedTask;
    }
}

public class IsoDateOnlyModelBinderProvider : IModelBinderProvider
{
    public IModelBinder? GetBinder(ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var modelType = Nullable.GetUnderlyingType(context.Metadata.ModelType) ?? context.Metadata.ModelType;

        return modelType == typeof(DateOnly) ? new IsoDateOnlyModelBinder() : null;
    }
}
