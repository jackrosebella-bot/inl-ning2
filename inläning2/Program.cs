using Inlamning2.Data;
using Inlamning2.Data.Models;
using Spectre.Console;

internal static class Program
{
    private static void Main()
    {
        try
        {
            var storage = new FileStorage();
            var dataFolder = FindDataFolder();
            var configPath = Path.Combine(dataFolder, "config.json");
            var pricesPath = Path.Combine(dataFolder, "prices.txt");
            var statePath = Path.Combine(dataFolder, "parking-data.json");

            var configuration = storage.ReadConfiguration(configPath);
            var prices = storage.ReadPriceList(pricesPath, configuration);
            var garage = new ParkingGarage(configuration, storage.ReadState(statePath));

            new ParkingConsole(garage, prices, storage, pricesPath, statePath).Run();
        }
        catch (Exception exception)
        {
            AnsiConsole.MarkupLine(
                $"[red]Programmet kunde inte starta:[/] {Markup.Escape(exception.Message)}");
            Environment.ExitCode = 1;
        }
    }

    private static string FindDataFolder()
    {
        var projectFolder = Path.GetFullPath(
            Path.Combine(AppContext.BaseDirectory, "..", "..", ".."));

        if (File.Exists(Path.Combine(projectFolder, "config.json")) &&
            File.Exists(Path.Combine(projectFolder, "prices.txt")))
        {
            return projectFolder;
        }

        return AppContext.BaseDirectory;
    }
}

internal sealed class ParkingConsole
{
    private readonly ParkingGarage _garage;
    private readonly FileStorage _storage;
    private readonly string _pricesPath;
    private readonly string _statePath;
    private PriceList _prices;

    public ParkingConsole(ParkingGarage garage, PriceList prices, FileStorage storage,
        string pricesPath, string statePath)
    {
        _garage = garage;
        _prices = prices;
        _storage = storage;
        _pricesPath = pricesPath;
        _statePath = statePath;
    }

    public void Run()
    {
        var running = true;
        while (running)
        {
            AnsiConsole.Clear();
            AnsiConsole.Write(
                new FigletText("PRAGUE PARKING").Centered().Color(Color.DeepSkyBlue1));
            AnsiConsole.MarkupLine(
                $"[grey]Platser: {_garage.Spots.Count} | " +
                $"Lediga: {_garage.Spots.Count(spot => spot.IsEmpty)}[/]");

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<MenuAction>()
                    .Title("[bold]Vad vill du göra?[/]")
                    .PageSize(10)
                    .UseConverter(item => item.Label)
                    .AddChoices(MenuAction.All));

            AnsiConsole.Clear();
            switch (action.Key)
            {
                case "park": ParkVehicle(); break;
                case "move": MoveVehicle(); break;
                case "checkout": CheckoutVehicle(); break;
                case "search": SearchVehicle(); break;
                case "spot": ShowSpot(); break;
                case "all": ShowAllSpots(); break;
                case "map": ShowMap(); break;
                case "prices": ReloadPrices(); break;
                case "exit": running = false; break;
            }

            if (running)
            {
                AnsiConsole.MarkupLine("[grey]Tryck på valfri tangent för att gå tillbaka.[/]");
                Console.ReadKey(true);
            }
        }

        Save();
        AnsiConsole.MarkupLine("[green]Parkeringen har sparats. Tack och hej![/]");
    }

    private void ParkVehicle()
    {
        AnsiConsole.Write(new Rule("[bold]Parkera fordon[/]"));
        var type = AnsiConsole.Prompt(
            new SelectionPrompt<VehicleTypeConfiguration>()
                .Title("Välj fordonstyp:")
                .UseConverter(item => $"{item.Name} ({item.Code})")
                .AddChoices(_garage.VehicleTypes));

        var plate = AskPlate();
        var hasPermit = type.CanUseReservedSpots &&
                        type.RequiresPermitForReservedSpots &&
                        AnsiConsole.Confirm("Har fordonet ett giltigt parkeringstillstånd?");

        if (_garage.TryPark(type.Code, plate, hasPermit, DateTimeOffset.Now,
                out var spot, out var error))
        {
            Save();
            AnsiConsole.MarkupLine($"[green]Fordonet är parkerat på plats {spot}.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(error)}[/]");
        }
    }

    private void MoveVehicle()
    {
        AnsiConsole.Write(new Rule("[bold]Flytta fordon[/]"));
        var plate = AskPlate();
        var destination = AskSpotNumber("Ny plats", 1, _garage.Spots.Count);

        if (_garage.TryMove(plate, destination, out var previous, out var error))
        {
            Save();
            AnsiConsole.MarkupLine(
                $"[green]Fordonet har flyttats från plats {previous} till plats {destination}.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{Markup.Escape(error)}[/]");
        }
    }

    private void CheckoutVehicle()
    {
        AnsiConsole.Write(new Rule("[bold]Hämta ut fordon[/]"));
        var plate = AskPlate();

        if (!_garage.FindVehicle(plate, out var vehicle, out var spot))
        {
            AnsiConsole.MarkupLine("[red]Vi hittar inte fordonet.[/]");
            return;
        }

        var duration = DateTimeOffset.Now - vehicle!.ParkedAt;
        var amount = _prices.Calculate(vehicle.TypeCode, duration);
        _garage.TryRemove(plate, out _, out _);
        Save();

        var typeName = _garage.GetTypeConfiguration(vehicle.TypeCode).Name;
        AnsiConsole.MarkupLine(
            $"[green]{Markup.Escape(typeName)} {Markup.Escape(vehicle.LicensePlate)} " +
            $"har hämtats från plats {spot!.Number}.[/]");
        AnsiConsole.MarkupLine($"Parkeringstid: {FormatDuration(duration)}");
        AnsiConsole.MarkupLine(
            $"Pris: [bold yellow]{amount.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture)} CZK[/]");
    }

    private void SearchVehicle()
    {
        AnsiConsole.Write(new Rule("[bold]Sök fordon[/]"));
        var plate = AskPlate();

        if (!_garage.FindVehicle(plate, out var vehicle, out var spot))
        {
            AnsiConsole.MarkupLine("[red]Vi hittar inte fordonet.[/]");
            return;
        }

        var type = _garage.GetTypeConfiguration(vehicle!.TypeCode);
        AnsiConsole.MarkupLine(
            $"[green]{Markup.Escape(type.Name)} {Markup.Escape(vehicle.LicensePlate)} " +
            $"står på plats {spot!.Number}.[/]");
        AnsiConsole.MarkupLine(
            $"Incheckad: {vehicle.ParkedAt.LocalDateTime:yyyy-MM-dd HH:mm}");
    }

    private void ShowSpot()
    {
        AnsiConsole.Write(new Rule("[bold]Visa parkeringsplats[/]"));
        var number = AskSpotNumber("Platsnummer", 1, _garage.Spots.Count);
        var spot = _garage.Spots[number - 1];

        var table = new Table()
            .Border(TableBorder.Rounded)
            .AddColumn("Egenskap")
            .AddColumn("Värde");

        table.AddRow("Plats", number.ToString());
        table.AddRow("Typ", spot.IsReserved ? "Reserverad" : "Vanlig");
        table.AddRow("Status", GetSpotStatus(spot));

        foreach (var vehicle in spot.Vehicles)
        {
            var type = _garage.GetTypeConfiguration(vehicle.TypeCode);
            table.AddRow("Fordon", $"{type.Name} – {vehicle.LicensePlate}");
        }

        AnsiConsole.Write(table);
    }

    private void ShowAllSpots()
    {
        AnsiConsole.Write(new Rule("[bold]Alla parkeringsplatser[/]"));
        var table = new Table()
            .Border(TableBorder.Simple)
            .AddColumn("Plats")
            .AddColumn("Typ")
            .AddColumn("Status")
            .AddColumn("Fordon");

        foreach (var spot in _garage.Spots)
        {
            var occupants = string.Join(", ", spot.Vehicles.Select(vehicle =>
                $"{_garage.GetTypeConfiguration(vehicle.TypeCode).Name} {vehicle.LicensePlate}"));

            table.AddRow(
                spot.Number.ToString("D3"),
                spot.IsReserved ? "Reserverad" : "Vanlig",
                GetSpotStatus(spot),
                occupants.Length == 0 ? "–" : occupants);
        }

        AnsiConsole.Write(table);
    }

    private void ShowMap()
    {
        AnsiConsole.Write(new Rule("[bold]Parkeringskarta[/]"));
        const int columns = 5;
        var table = new Table().Border(TableBorder.Rounded).HideHeaders();

        for (var i = 0; i < columns; i++)
            table.AddColumn(new TableColumn(string.Empty).Centered());

        for (var offset = 0; offset < _garage.Spots.Count; offset += columns)
        {
            var cells = new List<string>();
            for (var i = offset; i < Math.Min(offset + columns, _garage.Spots.Count); i++)
            {
                var spot = _garage.Spots[i];
                var color = spot.IsEmpty
                    ? (spot.IsReserved ? "yellow" : "green")
                    : "red";
                var status = "LEDIG";
                if (!spot.IsEmpty)
                {
                    var type = _garage.GetTypeConfiguration(spot.Vehicles[0].TypeCode);
                    var isPartiallyOccupied = spot.Vehicles.Count < type.VehiclesPerSpot;
                    color = isPartiallyOccupied ? "cyan" : "red";
                    status = isPartiallyOccupied
                        ? $"{spot.Vehicles.Count}/{type.VehiclesPerSpot}"
                        : "FULL";
                }

                cells.Add($"[{color}]{spot.Number:D3} {status}[/]");
            }

            while (cells.Count < columns)
                cells.Add(string.Empty);

            table.AddRow(cells.ToArray());
        }

        AnsiConsole.Write(table);
        AnsiConsole.MarkupLine(
            "[green]Grön: ledig[/]  [yellow]Gul: reserverad[/]  [cyan]Turkos: delvis belagd[/]  [red]Röd: full[/]");
        var partiallyOccupied = _garage.Spots.Count(IsPartiallyOccupied);
        AnsiConsole.MarkupLine(
            $"Tomma platser: {_garage.Spots.Count(spot => spot.IsEmpty)} av {_garage.Spots.Count} | " +
            $"Delvis belagda: {partiallyOccupied}");
    }

    private bool IsPartiallyOccupied(ParkingSpot spot)
    {
        if (spot.IsEmpty)
            return false;

        var type = _garage.GetTypeConfiguration(spot.Vehicles[0].TypeCode);
        return spot.Vehicles.Count < type.VehiclesPerSpot;
    }

    private string GetSpotStatus(ParkingSpot spot)
    {
        if (spot.IsEmpty)
            return "Ledig";

        var type = _garage.GetTypeConfiguration(spot.Vehicles[0].TypeCode);
        return spot.Vehicles.Count < type.VehiclesPerSpot
            ? $"Delvis upptagen ({spot.Vehicles.Count}/{type.VehiclesPerSpot})"
            : "Full";
    }

    private void ReloadPrices()
    {
        try
        {
            _prices = _storage.ReadPriceList(_pricesPath, _garage.Configuration);
            AnsiConsole.MarkupLine("[green]Prislistan har lästs in på nytt.[/]");
        }
        catch (Exception exception)
        {
            AnsiConsole.MarkupLine(
                $"[red]Prislistan kunde inte läsas: {Markup.Escape(exception.Message)}[/]");
        }
    }

    private string AskPlate()
    {
        while (true)
        {
            var plate = AnsiConsole.Ask<string>("Registreringsnummer:")
                .Trim()
                .ToUpperInvariant();
            if (plate.Length is > 0 and <= 10 && plate.All(char.IsLetterOrDigit))
                return plate;

            AnsiConsole.MarkupLine(
                "[red]Använd 1–10 bokstäver eller siffror utan mellanslag.[/]");
        }
    }

    private static int AskSpotNumber(string prompt, int minimum, int maximum)
    {
        while (true)
        {
            var text = AnsiConsole.Ask<string>($"{prompt} ({minimum}–{maximum}):");
            if (int.TryParse(text, out var number) && number >= minimum && number <= maximum)
                return number;

            AnsiConsole.MarkupLine(
                $"[red]Skriv ett heltal mellan {minimum} och {maximum}.[/]");
        }
    }

    private static string FormatDuration(TimeSpan duration)
    {
        return $"{Math.Max(0, (int)duration.TotalDays)} d {duration.Hours} h {duration.Minutes} min";
    }

    private void Save()
    {
        _storage.SaveState(_statePath, _garage.ToRecords());
    }

    private sealed record MenuAction(string Key, string Label)
    {
        public static readonly MenuAction[] All =
        [
            new("park", "Parkera fordon"),
            new("move", "Flytta fordon"),
            new("checkout", "Hämta ut fordon och betala"),
            new("search", "Sök efter fordon"),
            new("spot", "Visa en parkeringsplats"),
            new("all", "Visa alla platser"),
            new("map", "Visa parkeringskartan"),
            new("prices", "Läs in prislistan på nytt"),
            new("exit", "Avsluta")
        ];
    }
}