using Microsoft.AspNetCore.Mvc;

namespace CursoCursor.Api.Features.Workshops;

public static class WorkshopEndpoints
{
    public static IEndpointRouteBuilder MapWorkshopEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/api/workshops", (ReservationStore store) =>
                Results.Ok(store.GetWorkshops()))
            .Produces<WorkshopResponse[]>(StatusCodes.Status200OK);

        endpoints.MapGet("/api/workshops/{id:guid}", (Guid id, ReservationStore store) =>
            {
                var workshop = store.GetWorkshop(id);
                if (workshop is null)
                {
                    var problem = new ProblemDetails
                    {
                        Status = StatusCodes.Status404NotFound,
                        Title = "Recurso no encontrado",
                        Detail = "No se encontró el taller."
                    };
                    problem.Extensions["code"] = "workshop_not_found";
                    return Results.Problem(problem);
                }

                return Results.Ok(workshop);
            })
            .Produces<WorkshopResponse>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }
}
