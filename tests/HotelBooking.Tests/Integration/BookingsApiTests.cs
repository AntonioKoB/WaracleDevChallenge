using System.Net;
using System.Net.Http.Json;
using HotelBooking.Api.Contracts;

namespace HotelBooking.Tests.Integration;

[Collection(DatabaseCollection.Name)]
public class BookingsApiTests(ApiFactory apiFactory, DatabaseFixture databaseFixture) : IClassFixture<ApiFactory>
{
    [IntegrationFact]
    public async Task FindHotelByName_WhenHotelExists_ReturnsHotel()
    {
        await using var context = databaseFixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context);
        using var client = apiFactory.CreateClient();

        var response = await client.GetAsync($"/api/hotels?name={Uri.EscapeDataString(data.Hotel.Name)}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var hotels = await response.Content.ReadFromJsonAsync<List<HotelResponse>>();
        Assert.Contains(hotels!, h => h.Name == data.Hotel.Name);
    }

    [IntegrationFact]
    public async Task FindHotelByName_WithASubstringInAnyCase_StillMatches()
    {
        // Every seeded IntegrationTestData hotel is named "Test Hotel <suffix>" - searching
        // for an upper-cased, partial slice of that should still find it.
        await using var context = databaseFixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context);
        using var client = apiFactory.CreateClient();
        var partialUpperCase = data.Hotel.Name[5..].ToUpperInvariant(); // "HOTEL <suffix>"

        var response = await client.GetAsync($"/api/hotels?name={Uri.EscapeDataString(partialUpperCase)}");

        var hotels = await response.Content.ReadFromJsonAsync<List<HotelResponse>>();
        Assert.Contains(hotels!, h => h.Name == data.Hotel.Name);
    }

    [IntegrationFact]
    public async Task FindHotelByName_WhenHotelDoesNotExist_ReturnsEmptyList()
    {
        using var client = apiFactory.CreateClient();

        var response = await client.GetAsync($"/api/hotels?name={Guid.NewGuid():N}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var hotels = await response.Content.ReadFromJsonAsync<List<HotelResponse>>();
        Assert.Empty(hotels!);
    }

    [IntegrationFact]
    public async Task FindAvailableRooms_WithAnAmbiguousDateFormat_ReturnsBadRequestInsteadOfGuessing()
    {
        // "07/09/2026" is ambiguous (7 September or 9 July?) - this must be rejected, not
        // silently misparsed using whatever culture the server happens to be running under.
        using var client = apiFactory.CreateClient();

        var response = await client.GetAsync("/api/rooms/available?checkInDate=07/09/2026&checkOutDate=08/09/2026&guests=1");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [IntegrationFact]
    public async Task FindAvailableRooms_ExcludesARoomBookedForTheRequestedNights()
    {
        await using var context = databaseFixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context);
        using var client = apiFactory.CreateClient();

        var bookRequest = new BookRoomRequest(
            data.Room.Id, new DateOnly(2027, 8, 10), new DateOnly(2027, 8, 12),
            [new GuestRequest("Guest One", "guest.one@example.com")]);
        var bookResponse = await client.PostAsJsonAsync("/api/bookings", bookRequest);
        Assert.Equal(HttpStatusCode.Created, bookResponse.StatusCode);

        var available = await client.GetFromJsonAsync<List<AvailableRoomResponse>>(
            $"/api/rooms/available?checkInDate=2027-08-11&checkOutDate=2027-08-12&guests=1&hotelId={data.Hotel.Id}");

        Assert.DoesNotContain(available!, r => r.RoomId == data.Room.Id);
    }

    [IntegrationFact]
    public async Task FindAvailableRooms_WithNoHotelId_FindsTheRoomAcrossAnyHotel()
    {
        await using var context = databaseFixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context);
        using var client = apiFactory.CreateClient();

        var available = await client.GetFromJsonAsync<List<AvailableRoomResponse>>(
            "/api/rooms/available?checkInDate=2027-08-20&checkOutDate=2027-08-22&guests=1");

        var found = Assert.Single(available!, r => r.RoomId == data.Room.Id);
        Assert.Equal(data.Hotel.Id, found.HotelId);
        Assert.Equal(data.Hotel.Name, found.HotelName);
    }

    [IntegrationFact]
    public async Task BookRoom_WithValidRequest_ReturnsCreatedBookingWithReference()
    {
        await using var context = databaseFixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context);
        using var client = apiFactory.CreateClient();

        var request = new BookRoomRequest(
            data.Room.Id, new DateOnly(2027, 9, 1), new DateOnly(2027, 9, 3),
            [new GuestRequest("Guest One", "guest.one@example.com")]);

        var response = await client.PostAsJsonAsync("/api/bookings", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.NotNull(booking);
        Assert.Equal(9, booking!.BookingReference.Length);
        Assert.Equal(data.Hotel.Name, booking.HotelName);
        Assert.Equal(data.Room.Number, booking.RoomNumber);
    }

    [IntegrationFact]
    public async Task BookRoom_WhenNightIsAlreadyBooked_ReturnsConflict()
    {
        await using var context = databaseFixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context);
        using var client = apiFactory.CreateClient();

        var request = new BookRoomRequest(
            data.Room.Id, new DateOnly(2027, 10, 1), new DateOnly(2027, 10, 3),
            [new GuestRequest("Guest One", "guest.one@example.com")]);
        await client.PostAsJsonAsync("/api/bookings", request);

        var conflicting = await client.PostAsJsonAsync("/api/bookings", request with
        {
            Guests = [new GuestRequest("Guest Two", "guest.two@example.com")],
        });

        Assert.Equal(HttpStatusCode.Conflict, conflicting.StatusCode);
    }

    [IntegrationFact]
    public async Task BookRoom_WhenGuestCountExceedsCapacity_ReturnsBadRequest()
    {
        await using var context = databaseFixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context, capacity: 1);
        using var client = apiFactory.CreateClient();

        var request = new BookRoomRequest(
            data.Room.Id, new DateOnly(2027, 11, 1), new DateOnly(2027, 11, 3),
            [new GuestRequest("Guest One", "g1@example.com"), new GuestRequest("Guest Two", "g2@example.com")]);

        var response = await client.PostAsJsonAsync("/api/bookings", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [IntegrationFact]
    public async Task GetBookingByReference_ReturnsBookingDetails()
    {
        await using var context = databaseFixture.CreateContext();
        await using var data = await IntegrationTestData.CreateAsync(context);
        using var client = apiFactory.CreateClient();

        var bookRequest = new BookRoomRequest(
            data.Room.Id, new DateOnly(2027, 12, 1), new DateOnly(2027, 12, 3),
            [new GuestRequest("Guest One", "guest.one@example.com")]);
        var bookResponse = await client.PostAsJsonAsync("/api/bookings", bookRequest);
        var created = await bookResponse.Content.ReadFromJsonAsync<BookingResponse>();

        var response = await client.GetAsync($"/api/bookings/{created!.BookingReference}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var booking = await response.Content.ReadFromJsonAsync<BookingResponse>();
        Assert.Equal(created.BookingReference, booking!.BookingReference);
        Assert.Single(booking.Guests);
    }

    [IntegrationFact]
    public async Task GetBookingByReference_WhenUnknown_ReturnsNotFound()
    {
        using var client = apiFactory.CreateClient();

        var response = await client.GetAsync("/api/bookings/000000000");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
