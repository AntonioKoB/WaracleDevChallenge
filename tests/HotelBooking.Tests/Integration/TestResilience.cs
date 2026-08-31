using HotelBooking.Infrastructure.Resilience;

namespace HotelBooking.Tests.Integration;

/// <summary>
/// A real DatabaseResiliencePipeline with production defaults, shared across integration
/// tests that construct repositories directly. It behaves as a transparent pass-through
/// unless a test deliberately drives it into an open state - normal test runs never
/// accumulate enough failures to trip it.
/// </summary>
internal static class TestResilience
{
    public static IDatabaseResiliencePipeline Pipeline { get; } = new DatabaseResiliencePipeline();
}
