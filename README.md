# Prague Parking 2.0

Konsolappen använder Spectre.Console. Lösningen är uppdelad i två projekt:

- inläning2: meny och användargränssnitt.
- inläning2.Data: fordons- och parkeringsmodeller samt läsning och sparning av filer.

## Starta i Visual Studio

Öppna inläning2.sln och välj projektet inläning2 som startprojekt.

## Filer

- inläning2/config.json styr antal platser, reserverade platser, gratis minuter, fordonstyper och antal fordon per plats.
- inläning2/prices.txt innehåller pris per påbörjad timme. Allt efter # på en rad är kommentar.
- inläning2/parking-data.json skapas och uppdateras automatiskt lokalt efter ändringar. Den personliga parkeringshistoriken versionshanteras inte.

Prislistan kan läsas in igen via menyn. Parkering kortare än tio minuter är gratis.

## Loggbok och GitHub

Fyll i LOGGBOK_TEMPLATE.md med dina egna anteckningar. GitHub-repot ska göras publikt och dess länk lämnas in separat.
