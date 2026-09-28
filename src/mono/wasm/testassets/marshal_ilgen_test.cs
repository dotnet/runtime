using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.JavaScript;

int[] x = new int[0];
MyClass.call_needing_marhsal_ilgen(x);
Console.WriteLine("TestOutput -> call_needing_marhsal_ilgen got called");

return 42;

public partial class MyClass
{
    [DllImport("incompatible_type")]
    public static extern void call_needing_marhsal_ilgen(int[] numbers);
}
