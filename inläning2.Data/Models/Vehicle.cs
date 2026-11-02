namespace Inlamning2.Data.Models;

public abstract class Vehicle
{
    protected Vehicle(string typeCode, string licensePlate, DateTimeOffset parkedAt, bool hasParkingPermit)
    {
        TypeCode = typeCode;
        LicensePlate = licensePlate;
        ParkedAt = parkedAt;
        HasParkingPermit = hasParkingPermit;
    }

    public string TypeCode { get; }
    public string LicensePlate { get; }
    public DateTimeOffset ParkedAt { get; }
    public bool HasParkingPermit { get; }
}

public sealed class Car : Vehicle
{
    public Car(string licensePlate, DateTimeOffset parkedAt, bool hasParkingPermit)
        : base("BIL", licensePlate, parkedAt, hasParkingPermit) { }
}

public sealed class Motorcycle : Vehicle
{
    public Motorcycle(string licensePlate, DateTimeOffset parkedAt)
        : base("MC", licensePlate, parkedAt, false) { }
}

public sealed class ConfiguredVehicle : Vehicle
{
    public ConfiguredVehicle(string typeCode, string licensePlate, DateTimeOffset parkedAt, bool hasParkingPermit)
        : base(typeCode, licensePlate, parkedAt, hasParkingPermit) { }
}

public static class VehicleFactory
{
    public static Vehicle Create(string typeCode, string plate, DateTimeOffset parkedAt, bool hasPermit)
    {
        return typeCode.ToUpperInvariant() switch
        {
            "BIL" => new Car(plate, parkedAt, hasPermit),
            "MC" => new Motorcycle(plate, parkedAt),
            _ => new ConfiguredVehicle(typeCode, plate, parkedAt, hasPermit)
        };
    }
}