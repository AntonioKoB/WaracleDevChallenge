using System.Diagnostics;
using HotelBooking.Api;
using HotelBooking.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Polly.CircuitBreaker;

namespace HotelBooking.Tests;

public class GlobalExceptionHandlerTests
{
    private static (GlobalExceptionHandler Handler, DefaultHttpContext HttpContext) CreateHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddProblemDetails();
        var provider = services.BuildServiceProvider();

        var handler = new GlobalExceptionHandler(
            provider.GetRequiredService<IProblemDetailsService>(),
            provider.GetRequiredService<ILogger<GlobalExceptionHandler>>());

        var httpContext = new DefaultHttpContext
        {
            RequestServices = provider,
            Response = { Body = new MemoryStream() },
        };

        return (handler, httpContext);
    }

    /// <summary>
    /// Activity.RecordException/SetStatus only do anything when a listener is actually
    /// subscribed - in the real app that's the OpenTelemetry SDK; here it's this listener,
    /// standing in for it so the test can observe the same effect.
    /// </summary>
    private static ActivityListener SubscribeActivityListener()
    {
        var listener = new ActivityListener
        {
            ShouldListenTo = _ => true,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
        };
        ActivitySource.AddActivityListener(listener);
        return listener;
    }

    [Fact]
    public async Task TryHandleAsync_ForAGenuineFault_RecordsExceptionTelemetryOnTheActivity()
    {
        using var listener = SubscribeActivityListener();
        using var source = new ActivitySource(nameof(TryHandleAsync_ForAGenuineFault_RecordsExceptionTelemetryOnTheActivity));
        using var activity = source.StartActivity("test-operation");
        var (handler, httpContext) = CreateHandler();

        await handler.TryHandleAsync(httpContext, new BrokenCircuitException(), CancellationToken.None);

        Assert.Equal(ActivityStatusCode.Error, activity!.Status);
        Assert.Contains(activity.Events, e => e.Name == "exception");
    }

    [Fact]
    public async Task TryHandleAsync_ForAnExpectedBusinessOutcome_DoesNotRecordExceptionTelemetry()
    {
        using var listener = SubscribeActivityListener();
        using var source = new ActivitySource(nameof(TryHandleAsync_ForAnExpectedBusinessOutcome_DoesNotRecordExceptionTelemetry));
        using var activity = source.StartActivity("test-operation");
        var (handler, httpContext) = CreateHandler();

        await handler.TryHandleAsync(httpContext, new BookingConflictException(), CancellationToken.None);

        Assert.Equal(ActivityStatusCode.Unset, activity!.Status);
        Assert.DoesNotContain(activity.Events, e => e.Name == "exception");
    }

    [Fact]
    public void Map_RoomCapacityExceededException_ReturnsBadRequest()
    {
        var (statusCode, _, detail) = GlobalExceptionHandler.Map(new RoomCapacityExceededException(2, 3));

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
        Assert.Contains("3", detail);
    }

    [Fact]
    public void Map_InvalidReservationDatesException_ReturnsBadRequest()
    {
        var (statusCode, _, _) = GlobalExceptionHandler.Map(
            new InvalidReservationDatesException(new DateOnly(2027, 1, 2), new DateOnly(2027, 1, 1)));

        Assert.Equal(StatusCodes.Status400BadRequest, statusCode);
    }

    [Fact]
    public void Map_BookingConflictException_ReturnsConflict()
    {
        var (statusCode, _, _) = GlobalExceptionHandler.Map(new BookingConflictException());

        Assert.Equal(StatusCodes.Status409Conflict, statusCode);
    }

    [Fact]
    public void Map_BrokenCircuitException_ReturnsServiceUnavailable()
    {
        var (statusCode, _, _) = GlobalExceptionHandler.Map(new BrokenCircuitException());

        Assert.Equal(StatusCodes.Status503ServiceUnavailable, statusCode);
    }

    [Fact]
    public void Map_UnexpectedException_ReturnsInternalServerErrorWithoutLeakingTheMessage()
    {
        var (statusCode, _, detail) = GlobalExceptionHandler.Map(new InvalidOperationException("some internal detail"));

        Assert.Equal(StatusCodes.Status500InternalServerError, statusCode);
        Assert.DoesNotContain("some internal detail", detail);
    }
}
