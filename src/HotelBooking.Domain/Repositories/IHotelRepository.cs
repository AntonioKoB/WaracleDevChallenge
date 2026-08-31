using HotelBooking.Domain.Entities;

namespace HotelBooking.Domain.Repositories;

public interface IHotelRepository
{
    /// <summary>
    /// Case-insensitive, substring search on the hotel name - implicitly wrapped and with
    /// spaces treated as wildcards, so "waracle" matches "The Grand Waracle" and
    /// "grand waracle" matches even with other words in between.
    /// </summary>
    Task<IReadOnlyCollection<Hotel>> SearchByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<Hotel?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
}
