namespace Inlamning2.Data.Models;

public sealed class ParkingGarage
{
    private readonly Dictionary<string, VehicleTypeConfiguration> _types;

    public ParkingGarage(GarageConfiguration configuration, IEnumerable<VehicleRecord>? parkedVehicles = null)
    {
        Configuration = configuration;
        _types = configuration.VehicleTypes.ToDictionary(
            type => type.Code,
            StringComparer.OrdinalIgnoreCase);

        Spots = Enumerable.Range(1, configuration.SpotCount)
            .Select(number => new ParkingSpot(number, number <= configuration.ReservedSpotCount))
            .ToList();

        if (parkedVehicles is not null)
            Restore(parkedVehicles);
    }

    public GarageConfiguration Configuration { get; }
    public List<ParkingSpot> Spots { get; }
    public IReadOnlyCollection<VehicleTypeConfiguration> VehicleTypes => _types.Values;

    public bool TryPark(string typeCode, string licensePlate, bool hasPermit,
        DateTimeOffset parkedAt, out int spotNumber, out string error)
    {
        spotNumber = 0;
        error = string.Empty;

        if (!_types.TryGetValue(typeCode, out var type))
        {
            error = $"Fordonstypen '{typeCode}' finns inte i konfigurationen.";
            return false;
        }

        if (FindVehicle(licensePlate, out _, out _))
        {
            error = "Det finns redan ett fordon med det registreringsnumret.";
            return false;
        }

        var vehicle = VehicleFactory.Create(type.Code, licensePlate, parkedAt, hasPermit);
        var candidates = Spots.Where(spot => spot.IsReserved)
            .Concat(Spots.Where(spot => !spot.IsReserved));
        var spot = candidates.FirstOrDefault(candidate => candidate.CanAccept(vehicle, type));

        if (spot is null)
        {
            error = "Det finns ingen ledig plats som passar fordonet.";
            return false;
        }

        spot.Vehicles.Add(vehicle);
        spotNumber = spot.Number;
        return true;
    }

    public bool TryMove(string licensePlate, int destinationNumber,
        out int oldNumber, out string error)
    {
        oldNumber = 0;
        error = string.Empty;

        if (!FindVehicle(licensePlate, out var vehicle, out var source))
        {
            error = "Vi hittar inte fordonet.";
            return false;
        }

        oldNumber = source!.Number;
        if (destinationNumber < 1 || destinationNumber > Spots.Count)
        {
            error = $"Skriv ett platsnummer mellan 1 och {Spots.Count}.";
            return false;
        }

        var destination = Spots[destinationNumber - 1];
        if (destination == source)
        {
            error = "Fordonet står redan på den platsen.";
            return false;
        }

        var type = _types[vehicle!.TypeCode];
        if (!destination.CanAccept(vehicle, type))
        {
            error = "Den platsen är upptagen eller passar inte fordonet.";
            return false;
        }

        source.Vehicles.Remove(vehicle);
        destination.Vehicles.Add(vehicle);
        return true;
    }

    public bool TryRemove(string licensePlate, out Vehicle? vehicle, out int spotNumber)
    {
        if (!FindVehicle(licensePlate, out vehicle, out var spot))
        {
            spotNumber = 0;
            return false;
        }

        spotNumber = spot!.Number;
        spot.Vehicles.Remove(vehicle!);
        return true;
    }

    public bool FindVehicle(string licensePlate, out Vehicle? vehicle, out ParkingSpot? spot)
    {
        foreach (var candidate in Spots)
        {
            vehicle = candidate.Vehicles.FirstOrDefault(item =>
                string.Equals(item.LicensePlate, licensePlate, StringComparison.OrdinalIgnoreCase));

            if (vehicle is not null)
            {
                spot = candidate;
                return true;
            }
        }

        vehicle = null;
        spot = null;
        return false;
    }

    public VehicleTypeConfiguration GetTypeConfiguration(string typeCode) => _types[typeCode];

    public List<VehicleRecord> ToRecords()
    {
        return Spots.SelectMany(spot => spot.Vehicles.Select(vehicle => new VehicleRecord
        {
            SpotNumber = spot.Number,
            TypeCode = vehicle.TypeCode,
            LicensePlate = vehicle.LicensePlate,
            ParkedAt = vehicle.ParkedAt,
            HasParkingPermit = vehicle.HasParkingPermit
        })).ToList();
    }

    private void Restore(IEnumerable<VehicleRecord> records)
    {
        foreach (var record in records)
        {
            if (record.SpotNumber < 1 ||
                record.SpotNumber > Spots.Count ||
                !_types.TryGetValue(record.TypeCode, out var type))
            {
                continue;
            }

            var vehicle = VehicleFactory.Create(
                type.Code, record.LicensePlate, record.ParkedAt, record.HasParkingPermit);
            var spot = Spots[record.SpotNumber - 1];

            if (spot.CanAccept(vehicle, type) &&
                !FindVehicle(record.LicensePlate, out _, out _))
            {
                spot.Vehicles.Add(vehicle);
            }
        }
    }
}