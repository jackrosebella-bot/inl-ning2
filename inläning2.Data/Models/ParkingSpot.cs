namespace Inlamning2.Data.Models;

public sealed class ParkingSpot
{
    public ParkingSpot(int number, bool isReserved)
    {
        Number = number;
        IsReserved = isReserved;
    }

    public int Number { get; }
    public bool IsReserved { get; }
    public List<Vehicle> Vehicles { get; } = [];
    public bool IsEmpty => Vehicles.Count == 0;

    public bool CanAccept(Vehicle vehicle, VehicleTypeConfiguration type)
    {
        if (IsReserved &&
            (!type.CanUseReservedSpots ||
             (type.RequiresPermitForReservedSpots && !vehicle.HasParkingPermit)))
        {
            return false;
        }

        if (Vehicles.Count >= type.VehiclesPerSpot)
            return false;

        return Vehicles.Count == 0 ||
               Vehicles.All(existing => existing.TypeCode == vehicle.TypeCode);
    }
}