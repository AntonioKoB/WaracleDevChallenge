using Microsoft.Data.SqlClient;
using Polly;
using Polly.CircuitBreaker;

namespace HotelBooking.Infrastructure.Resilience;

/// <summary>
/// One shared circuit breaker for every repository, since they all hit the same database -
/// if it's down, all of them should fail fast together rather than each independently
/// piling up EF Core's own retries before giving up.
///
/// This sits outside EF Core's connection resiliency (EnableRetryOnFailure), not instead of
/// it: EF retries an individual operation through a handful of transient blips; this breaker
/// trips only after a sustained run of failures across many (already-retried) operations,
/// and then fails new calls immediately without touching the database at all.
///
/// It deliberately only counts SqlException/TimeoutException as failures - a
/// BookingConflictException from a genuine double-booking is a correct business outcome,
/// not a sign the database is unhealthy, and must never trip the breaker.
/// </summary>
public class DatabaseResiliencePipeline : IDatabaseResiliencePipeline
{
    private readonly ResiliencePipeline _pipeline;

    public DatabaseResiliencePipeline() : this(DefaultOptions())
    {
    }

    public DatabaseResiliencePipeline(CircuitBreakerStrategyOptions options)
    {
        _pipeline = new ResiliencePipelineBuilder().AddCircuitBreaker(options).Build();
    }

    public async Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken = default) =>
        await _pipeline.ExecuteAsync(async ct => await operation(ct), cancellationToken);

    public async Task ExecuteAsync(Func<CancellationToken, Task> operation, CancellationToken cancellationToken = default) =>
        await _pipeline.ExecuteAsync(
            async ct =>
            {
                await operation(ct);
                return true;
            },
            cancellationToken);

    private static CircuitBreakerStrategyOptions DefaultOptions() => new()
    {
        FailureRatio = 0.5,
        SamplingDuration = TimeSpan.FromSeconds(30),
        MinimumThroughput = 5,
        BreakDuration = TimeSpan.FromSeconds(15),
        ShouldHandle = args => ValueTask.FromResult(args.Outcome.Exception is SqlException or TimeoutException),
    };
}
