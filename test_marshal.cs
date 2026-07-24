using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

partial class Program
{
    [LibraryImport("kernel32.dll", EntryPoint = "SetLastError")]
    public static partial void Dummy(
        uint rangesLen,
        [In, Out, MarshalUsing(CountElementName = nameof(rangesLen))] int[]? addressRanges);

    static void Main()
    {
        try
        {
            Dummy(5, null);
            Console.WriteLine("Success");
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception: " + ex.GetType().Name + " - " + ex.Message);
        }
    }
}
