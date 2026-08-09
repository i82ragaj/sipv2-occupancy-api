namespace SIPV2.OccupancyApi.Contracts;

/// <summary>Entrada del catálogo de parkings (MDParking).</summary>
public record ParkingDto(string Id, string Name, string Type, bool Active);

/// <summary>Ocupación actual de un parking/contador (vista VOccupationActual).</summary>
public record CurrentOccupancyDto(
    string ParkingCode,
    string ParkingName,
    string CounterCode,
    string? CounterName,
    short? Capacity,
    short CurrentLevel,
    decimal? Percentage,
    DateTime? UpdatedAtUtc);
