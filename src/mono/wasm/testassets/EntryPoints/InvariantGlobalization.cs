using System;
using System.Globalization;
using System.Linq;

// https://github.com/dotnet/runtime/blob/main/docs/design/features/globalization-invariant-mode.md#cultures-and-culture-data


void WriteTestOutput(string output) => Console.WriteLine($"TestOutput -> {output}");
try
{    
    CultureInfo culture = new ("es-ES", false);
    WriteTestOutput($"es-ES: Is Invariant LCID: {culture.LCID == CultureInfo.InvariantCulture.LCID}");
   
    string expectedNativeName = "espa\u00F1ol (Espa\u00F1a)";
    string nativeName = culture.NativeName;
    if (nativeName != expectedNativeName)
        throw new ArgumentException($"Expected es-ES NativeName: {expectedNativeName}, but got: {nativeName}");
}
catch (CultureNotFoundException cnfe)
{
    WriteTestOutput($"Could not create es-ES culture: {cnfe.Message}");
}

WriteTestOutput($"CurrentCulture.NativeName: {CultureInfo.CurrentCulture.NativeName}");
return 42;
