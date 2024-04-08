using System;
using System.Globalization;

try
{
    CompareInfo compareInfo = new CultureInfo("es-ES").CompareInfo;
    int shouldBeEqual = compareInfo.Compare("A\u0300", "\u00C0", CompareOptions.None);
    if (shouldBeEqual != 0)
    {
        return 1;
    }
    int shouldThrow = compareInfo.Compare("A\u0300", "\u00C0", CompareOptions.IgnoreNonSpace);
    Console.WriteLine($"Did not throw as expected but returned {shouldThrow} as a result. Using CompareOptions.IgnoreNonSpace option alone should be unavailable in HybridGlobalization mode.");
}
catch (PlatformNotSupportedException pnse)
{
    Console.WriteLine($"HybridGlobalization works, thrown exception as expected: {pnse}.");
    return 42;
}
catch (Exception ex)
{
    Console.WriteLine($"HybridGlobalization failed, unexpected exception was thrown: {ex}.");
    return 2;
}
return 3;
