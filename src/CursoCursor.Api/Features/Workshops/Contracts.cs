namespace CursoCursor.Api.Features.Workshops;

public sealed record WorkshopResponse(
    Guid Id,
    string Title,
    int Capacity,
    int ActiveReservations,
    int AvailableSeats);
