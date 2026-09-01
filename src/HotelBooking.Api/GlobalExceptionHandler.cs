using System.Diagnostics;
using HotelBooking.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Polly.CircuitBreaker;

namespace HotelBooking.Api;

/// <summary>
/// Catches everything that escapes a controller and turns it into a consistent
/// ProblemDetails response, so every action doesn't need its own try/catch for the same
/// handful of domain exceptions.
/// </summary>
public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = Map(exception);

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            // A genuine fault - either truly unexpected, or the circuit breaker telling us
            // the database is down. Recorded as real exception telemetry (not just a log
            // line), which is what actually makes it show up in Application Insights'
            // exceptions table and Failures view, not just Traces.
            logger.LogError(exception, "{ExceptionType} handling {Path}", exception.GetType().Name, httpContext.Request.Path);
            Activity.Current?.SetStatus(ActivityStatusCode.Error, exception.Message);
            Activity.Current?.AddException(exception);
        }
        else
        {
            // A 400/409 from a domain rule or a booking conflict is a correct business
            // outcome, not a fault - it stays a log line, not exception telemetry, so it
            // doesn't pollute Failures with things that aren't actually failures.
            logger.LogWarning(exception, "{ExceptionType} handling {Path}", exception.GetType().Name, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
            },
        });
    }

    /// <summary>
    /// Pure mapping, kept separate from the HTTP plumbing above so it can be unit tested
    /// directly. A 500's detail is deliberately generic - the real exception is logged
    /// server-side, not handed to the caller.
    /// </summary>
    internal static (int StatusCode, string Title, string Detail) Map(Exception exception) => exception switch
    {
        RoomCapacityExceededException ex => (StatusCodes.Status400BadRequest, "Room capacity exceeded", ex.Message),
        InvalidReservationDatesException ex => (StatusCodes.Status400BadRequest, "Invalid reservation dates", ex.Message),
        BookingConflictException ex => (StatusCodes.Status409Conflict, "Booking conflict", ex.Message),
        BrokenCircuitException => (
            StatusCodes.Status503ServiceUnavailable,
            "Service temporarily unavailable",
            "The database is currently unreachable. Please try again shortly."),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "An unexpected error occurred. Please try again later."),
    };
}
