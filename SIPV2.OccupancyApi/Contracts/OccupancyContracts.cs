namespace SIPV2.OccupancyApi.Contracts;

/// <summary>Petición para filtrar la ocupación actual por un listado de ids de contador.</summary>
public record CurrentOccupancyByCounterIdsRequest(IReadOnlyList<string> CounterIds);

/// <summary>Ocupación actual de un parking/contador (vista VOccupationActual).</summary>
public record CurrentOccupancyDto(
    string ParkingId,
    string ParkingName,
    string CounterId,
    string CounterCode,
    string? CounterName,
    short? Capacity,
    short CurrentLevel,
    decimal? Percentage,
    DateTime? UpdatedAtUtc);
