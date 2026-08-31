using HotelBooking.Api;
using HotelBooking.Domain.Exceptions;
using Microsoft.AspNetCore.Http;
using Polly.CircuitBreaker;

namespace HotelBooking.Tests;

public class GlobalExceptionHandlerTests
{
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
