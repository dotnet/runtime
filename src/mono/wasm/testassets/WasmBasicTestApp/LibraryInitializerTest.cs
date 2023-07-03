using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Resources;
using System.Runtime.InteropServices.JavaScript;

public partial class LibraryInitializerTest
{
    [JSExport]
    public static void Run()
    {
        TestOutput.WriteLine("Run from LibraryInitializer");
    }
}