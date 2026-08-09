namespace SIPV2.OccupancyApi.Contracts;

/// <summary>Entrada del catálogo de parkings (MDParking).</summary>
public record ParkingDto(string Id, string Name);

/// <summary>Detalle de un parking: identificación + todos sus contadores (vista VOccupationActual).</summary>
public record ParkingDetailDto(string ParkingId, string ParkingName, IReadOnlyList<CounterDto> Counters);

/// <summary>Información de un contador individual dentro de un parking.</summary>
public record CounterDto(
    string CounterId,
    string CounterCode,
    string? CounterName,
    short? Capacity,
    short CurrentLevel,
    decimal? Percentage,
    DateTime? UpdatedAtUtc);
