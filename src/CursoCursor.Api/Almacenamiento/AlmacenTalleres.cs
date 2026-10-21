namespace CursoCursor.Api.Almacenamiento;

public sealed class AlmacenTalleres
{
    private readonly object _lock = new();
    private readonly List<object> _talleres = [];
    private readonly List<object> _reservas = [];
}
