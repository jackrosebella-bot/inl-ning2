# Loggbok – Prague Parking 2.0

**Arbetsdatum:** 2026-11-02  
**Arbetssätt:** Projektet granskades och ändrades med stöd av Codex. Jag ska själv gå igenom koden, köra programmet och kontrollera att jag kan förklara lösningen.

## Arbetsanteckningar

### 2026-11-02 – Jämförelse med version 1

- Gick igenom funktionerna i den tidigare parkeringen: parkera, flytta, hämta ut, söka, visa plats, visa alla platser och visa karta.
- Jämförde den gamla strängbaserade lagringen med version 2:s fordons- och parkeringsobjekt.
- Behöll samma grundfunktioner i version 2 och lade till krav på priser och sparning.
- Nästa steg: gå igenom koden i Visual Studio och prova menyvalen.

### 2026-11-02 – Lösningsstruktur och inställningar

- Säkerställde att lösningen innehåller både konsolappen och det separata klassbiblioteket `inläning2.Data`.
- Inställningarna läses från JSON och innehåller 100 platser som standard, fordonstyper, kapacitet per plats och antal reserverade platser.
- Prislistan är en kommenterad textfil med priser i CZK. Parkeringsdata sparas i JSON.
- Nästa steg: kontrollera att parkering och uthämtning sparar rätt uppgifter efter omstart.

### 2026-11-02 – Parkeringskarta och byggkontroll

- Uppdaterade kartan så att en plats med kvarvarande kapacitet visas som delvis belagd i stället för helt full.
- Lade till tydligare sammanställning av tomma och delvis belagda platser.
- Byggde hela lösningen. Resultat: 0 fel och 0 varningar.
- Nästa steg: starta appen i Visual Studio och manuellt prova bil, MC, prisberäkning, karta och sparning.

## Reflektion – utkast att personifiera

- **Arv och objektorientering:** Fordon delar gemensamma egenskaper genom basklassen `Vehicle`. `Car` och `Motorcycle` ärver från den. Läs igenom klasserna och skriv med egna ord hur det gör det enklare att lägga till fler fordonstyper.
- **Filhantering och sparning:** JSON används för inställningar och parkerade fordon. Priserna läses från en separat textfil, som kan läsas in på nytt från menyn. Prova flödet och beskriv vad som händer när programmet startar och avslutas.
- **Fortsatt arbete:** Testa fler fordonstyper och olika kapaciteter, och kontrollera hur systemet hanterar felaktiga inmatningar och fulla platser.

> Ändra reflektionsdelen så att den beskriver vad du själv faktiskt har förstått och testat.
