using System;
using System.Runtime.InteropServices.JavaScript;
using System.Threading.Tasks;

namespace Wasm.Bench;

public partial class FrameApp
{
    [JSImport("globalThis.frameApp.ReachedCallback")]
    public static partial Task ReachedCallback();

    public static void Main()
    {
        Console.WriteLine("!!!AAAA");
        ReachedCallback();
    }
}
