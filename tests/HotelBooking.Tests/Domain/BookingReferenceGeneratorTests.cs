using HotelBooking.Domain;

namespace HotelBooking.Tests.Domain;

public class BookingReferenceGeneratorTests
{
    [Fact]
    public void Generate_ReturnsANineDigitReference()
    {
        var reference = BookingReferenceGenerator.Generate();

        Assert.Equal(9, reference.Length);
    }

    [Fact]
    public void Generate_OnlyUsesDigits()
    {
        var reference = BookingReferenceGenerator.Generate();

        Assert.All(reference, c => Assert.True(char.IsDigit(c)));
    }

    [Fact]
    public void Generate_ProducesDifferentReferencesAcrossCalls()
    {
        var references = Enumerable.Range(0, 20)
            .Select(_ => BookingReferenceGenerator.Generate())
            .ToHashSet();

        Assert.True(references.Count > 1, "Expected at least some variation across 20 generated references.");
    }
}
