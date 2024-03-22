using System;
using System.Globalization;

// https://github.com/dotnet/runtime/blob/main/docs/design/features/globalization-invariant-mode.md#cultures-and-culture-data
try
{
    CultureInfo culture = new ("de-DE", false);
    Console.WriteLine($"de-DE: Is Invariant LCID: {culture.LCID == CultureInfo.InvariantCulture.LCID}, NativeName: {culture.NativeName}");
}
catch (CultureNotFoundException cnfe)
{
    Console.WriteLine($"Could not create de-DE culture: {cnfe.Message}");
}

Console.WriteLine($"CurrentCulture.NativeName: {CultureInfo.CurrentCulture.NativeName}");
return 42;
