using System;
using System.Globalization;
using System.Linq;

// https://github.com/dotnet/runtime/blob/main/docs/design/features/globalization-invariant-mode.md#cultures-and-culture-data
try
{
    CultureInfo culture = new ("es-ES", false);
    Console.WriteLine($"es-ES: Is Invariant LCID: {culture.LCID == CultureInfo.InvariantCulture.LCID}");
    
    var nativeNameArg = args.FirstOrDefault(arg => arg.StartsWith("nativename="));
    if (nativeNameArg == null)
        throw new ArgumentException($"When not in invariant mode, InvariantGlobalization.cs expects nativename argument with expected es-ES NativeName.");
    string expectedNativeName = nativeNameArg.Substring(11).Trim('"'); // skip nativename=
    string nativeName = culture.NativeName;
    if (nativeName != expectedNativeName)
        throw new ArgumentException($"Expected es-ES NativeName: {expectedNativeName}, but got: {nativeName}");
}
catch (CultureNotFoundException cnfe)
{
    Console.WriteLine($"Could not create es-ES culture: {cnfe.Message}");
}

Console.WriteLine($"CurrentCulture.NativeName: {CultureInfo.CurrentCulture.NativeName}");
return 42;
