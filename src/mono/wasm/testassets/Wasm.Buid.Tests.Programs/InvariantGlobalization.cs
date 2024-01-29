using System;
using System.Globalization;

// https://github.com/dotnet/runtime/blob/main/docs/design/features/globalization-invariant-mode.md#cultures-and-culture-data
try
{
    CultureInfo culture = new ("es-ES", false);
    Console.WriteLine($"es-ES: Is Invariant LCID: {culture.LCID == CultureInfo.InvariantCulture.LCID}, NativeName: {culture.NativeName}");
}
catch (CultureNotFoundException cnfe)
{
    Console.WriteLine($"Could not create es-ES culture: {cnfe.Message}");
}

Console.WriteLine($"CurrentCulture.NativeName: {CultureInfo.CurrentCulture.NativeName}");
return 42;
