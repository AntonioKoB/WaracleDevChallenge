using HotelBooking.Domain.Exceptions;
using HotelBooking.Infrastructure.Resilience;
using Polly.CircuitBreaker;

namespace HotelBooking.Tests.Infrastructure;

public class DatabaseResiliencePipelineTests
{
    // Tight settings for tests - production uses much longer windows. 500ms is Polly's
    // enforced minimum for these two, but the tests never actually wait that long: every
    // call in a test happens instantly, well within a single sampling window regardless.
    private static DatabaseResiliencePipeline CreatePipeline() => new(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromMilliseconds(500),
        MinimumThroughput = 2,
        BreakDuration = TimeSpan.FromMilliseconds(500),
        ShouldHandle = args => ValueTask.FromResult(args.Outcome.Exception is TimeoutException),
    });

    [Fact]
    public async Task ExecuteAsync_WhenOperationSucceeds_ReturnsResult()
    {
        var pipeline = CreatePipeline();

        var result = await pipeline.ExecuteAsync(_ => Task.FromResult(42));

        Assert.Equal(42, result);
    }

    [Fact]
    public async Task ExecuteAsync_WhenOperationThrowsAnUnhandledExceptionType_PropagatesWithoutTrippingTheBreaker()
    {
        // InvalidOperationException isn't in ShouldHandle, so it must never count as a
        // circuit-breaker failure - this is what keeps a genuine BookingConflictException
        // from ever tripping the real pipeline.
        var pipeline = CreatePipeline();
        var attempts = 0;

        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<InvalidOperationException>(() => pipeline.ExecuteAsync<int>(_ =>
            {
                attempts++;
                throw new InvalidOperationException("not a database fault");
            }));
        }

        Assert.Equal(5, attempts); // every call actually ran the operation - the breaker never opened
    }

    [Fact]
    public async Task ExecuteAsync_WhenABookingConflictExceptionIsThrown_DoesNotTripTheBreaker()
    {
        var pipeline = CreatePipeline();
        var attempts = 0;

        for (var i = 0; i < 5; i++)
        {
            await Assert.ThrowsAsync<BookingConflictException>(() => pipeline.ExecuteAsync<int>(_ =>
            {
                attempts++;
                throw new BookingConflictException();
            }));
        }

        Assert.Equal(5, attempts);
    }

    [Fact]
    public async Task ExecuteAsync_AfterRepeatedTimeoutFailures_OpensTheCircuitAndFailsFastWithoutCallingTheOperation()
    {
        var pipeline = CreatePipeline();
        var attempts = 0;

        Task<int> FailingOperation(CancellationToken _)
        {
            attempts++;
            throw new TimeoutException("simulated database timeout");
        }

        // Exactly MinimumThroughput failures, at a 100% failure rate - comfortably past the
        // configured FailureRatio, so the breaker opens right on the next call after this.
        for (var i = 0; i < 2; i++)
        {
            await Assert.ThrowsAsync<TimeoutException>(() => pipeline.ExecuteAsync(FailingOperation));
        }

        var attemptsBeforeOpen = attempts;

        // Once open, a call should fail immediately with BrokenCircuitException and never
        // reach the operation at all - that's the entire point: fail fast, don't pile up.
        await Assert.ThrowsAsync<BrokenCircuitException>(() => pipeline.ExecuteAsync(FailingOperation));

        Assert.Equal(attemptsBeforeOpen, attempts);
    }
}
