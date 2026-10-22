namespace CursoCursor.Api.Features.Workshops;

public sealed class ReservationStore
{
    public static readonly Guid SeedWorkshopId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    private readonly object _lock = new();
    private readonly List<StoredWorkshop> _workshops;

    public ReservationStore()
    {
        _workshops =
        [
            new StoredWorkshop(SeedWorkshopId, "Taller de Cursor", 2)
        ];
    }

    public IReadOnlyList<WorkshopResponse> GetWorkshops()
    {
        lock (_lock)
        {
            var snapshot = new List<WorkshopResponse>(_workshops.Count);
            foreach (var workshop in _workshops)
            {
                snapshot.Add(workshop.ToSnapshot());
            }

            return snapshot.AsReadOnly();
        }
    }

    public WorkshopResponse? GetWorkshop(Guid id)
    {
        lock (_lock)
        {
            foreach (var workshop in _workshops)
            {
                if (workshop.Id == id)
                {
                    return workshop.ToSnapshot();
                }
            }

            return null;
        }
    }

    private sealed class StoredWorkshop
    {
        private readonly List<Reservation> _reservations = [];

        public StoredWorkshop(Guid id, string title, int capacity)
        {
            Id = id;
            Title = title;
            Capacity = capacity;
        }

        public Guid Id { get; }

        public string Title { get; }

        public int Capacity { get; }

        public WorkshopResponse ToSnapshot()
        {
            // El llamador ya tiene el bloqueo: cupo y reservas salen de esta instantánea.
            var activeReservations = _reservations.Count;
            return new WorkshopResponse(
                Id,
                Title,
                Capacity,
                activeReservations,
                Capacity - activeReservations);
        }
    }

    private sealed class Reservation
    {
    }
}
