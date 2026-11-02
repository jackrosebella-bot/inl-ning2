namespace Inlamning2.Data.Models;

public sealed class GarageConfiguration
{
    public int SpotCount { get; set; } = 100;
    public int ReservedSpotCount { get; set; } = 5;
    public int FreeMinutes { get; set; } = 10;
    public List<VehicleTypeConfiguration> VehicleTypes { get; set; } = [];

    public void Validate()
    {
        if (SpotCount < 1)
            throw new InvalidDataException("SpotCount måste vara minst 1.");
        if (ReservedSpotCount < 0 || ReservedSpotCount > SpotCount)
            throw new InvalidDataException("ReservedSpotCount måste vara mellan 0 och SpotCount.");
        if (FreeMinutes < 0)
            throw new InvalidDataException("FreeMinutes får inte vara negativt.");
        if (VehicleTypes is null || VehicleTypes.Count == 0)
            throw new InvalidDataException("Minst en fordonstyp måste anges.");

        var codes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var type in VehicleTypes)
        {
            type.Code = (type.Code ?? "").Trim().ToUpperInvariant();
            type.Name = (type.Name ?? "").Trim();

            if (type.Code.Length == 0 || !codes.Add(type.Code))
                throw new InvalidDataException("Fordonstyper behöver ha unika, ifyllda koder.");
            if (type.Name.Length == 0)
                throw new InvalidDataException($"Fordonstypen {type.Code} saknar namn.");
            if (type.VehiclesPerSpot < 1)
                throw new InvalidDataException($"VehiclesPerSpot för {type.Code} måste vara minst 1.");
        }
    }
}

public sealed class VehicleTypeConfiguration
{
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int VehiclesPerSpot { get; set; } = 1;
    public bool CanUseReservedSpots { get; set; }
    public bool RequiresPermitForReservedSpots { get; set; }
}

public sealed class VehicleRecord
{
    public int SpotNumber { get; set; }
    public string TypeCode { get; set; } = string.Empty;
    public string LicensePlate { get; set; } = string.Empty;
    public DateTimeOffset ParkedAt { get; set; }
    public bool HasParkingPermit { get; set; }
}

public sealed class GarageState
{
    public List<VehicleRecord> Vehicles { get; set; } = [];
}

public sealed class PriceList
{
    public int FreeMinutes { get; init; }
    public Dictionary<string, decimal> HourlyRates { get; init; } =
        new(StringComparer.OrdinalIgnoreCase);

    public decimal Calculate(string typeCode, TimeSpan duration)
    {
        if (duration.TotalMinutes < FreeMinutes)
            return 0m;

        if (!HourlyRates.TryGetValue(typeCode, out var hourlyRate))
            throw new InvalidOperationException($"Pris saknas för fordonstypen {typeCode}.");

        var startedHours = Math.Max(1, (int)Math.Ceiling(duration.TotalMinutes / 60d));
        return startedHours * hourlyRate;
    }
}