using System;
using System.Runtime.InteropServices.JavaScript;
public partial class Test
{
    [JSExport]
    public static int MyExport()
    {
        Console.WriteLine("TestOutput -> WASM Library MyExport is called");
        return 100;
    }
}
