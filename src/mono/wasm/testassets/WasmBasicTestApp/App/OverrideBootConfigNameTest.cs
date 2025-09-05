using System.Runtime.InteropServices.JavaScript;

public partial class OverrideBootConfigNameTest
{
    [JSExport]
    public static void Run()
    {
        TestOutput.WriteLine("Managed code has run");
    }
}
