using System.Globalization;
using System.Text.Json;
using Inlamning2.Data.Models;

namespace Inlamning2.Data;

public sealed class FileStorage
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public GarageConfiguration ReadConfiguration(string path)
    {
        var configuration = JsonSerializer.Deserialize<GarageConfiguration>(
            File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException("Konfigurationsfilen är tom eller ogiltig.");

        configuration.Validate();
        return configuration;
    }

    public PriceList ReadPriceList(string path, GarageConfiguration configuration)
    {
        var rates = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);

        foreach (var originalLine in File.ReadLines(path))
        {
            var line = originalLine.Split('#', 2)[0].Trim();
            if (line.Length == 0)
                continue;

            var separator = line.IndexOf('=');
            if (separator <= 0 || separator == line.Length - 1)
                throw new InvalidDataException($"Ogiltig rad i prislistan: {originalLine}");

            var code = line[..separator].Trim().ToUpperInvariant();
            if (!decimal.TryParse(line[(separator + 1)..].Trim(),
                    NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) ||
                rate < 0)
            {
                throw new InvalidDataException($"Ogiltigt pris på raden: {originalLine}");
            }

            rates[code] = rate;
        }

        foreach (var type in configuration.VehicleTypes)
        {
            if (!rates.ContainsKey(type.Code))
                throw new InvalidDataException($"Pris saknas för {type.Code} i prislistan.");
        }

        return new PriceList
        {
            FreeMinutes = configuration.FreeMinutes,
            HourlyRates = rates
        };
    }

    public List<VehicleRecord> ReadState(string path)
    {
        if (!File.Exists(path))
            return [];

        var state = JsonSerializer.Deserialize<GarageState>(
            File.ReadAllText(path), JsonOptions);

        return state?.Vehicles ?? [];
    }

    public void SaveState(string path, IEnumerable<VehicleRecord> vehicles)
    {
        var fullPath = Path.GetFullPath(path);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        var temporaryPath = fullPath + ".tmp";
        var state = new GarageState { Vehicles = vehicles.ToList() };

        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(state, JsonOptions));
        File.Move(temporaryPath, fullPath, true);
    }
}