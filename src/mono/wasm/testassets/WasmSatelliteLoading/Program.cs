using System;
using System.Globalization;
using System.Threading.Tasks;
using System.Resources;
using System.Runtime.InteropServices.JavaScript;

var rm = new ResourceManager("WasmSatelliteLoading.words", typeof(Program).Assembly);
Console.WriteLine("TestOutput -> default: " + rm.GetString("hello", CultureInfo.CurrentCulture));
Console.WriteLine("TestOutput -> es-ES without satellite: " + rm.GetString("hello", new CultureInfo("es-ES")));

await Interop.LoadSatelliteAssemblies(new[] { "es-ES" });

rm = new ResourceManager("WasmSatelliteLoading.words", typeof(Program).Assembly);
Console.WriteLine("TestOutput -> default: " + rm.GetString("hello", CultureInfo.CurrentCulture));
Console.WriteLine("TestOutput -> es-ES with satellite: " + rm.GetString("hello", new CultureInfo("es-ES")));

public partial class Interop
{
    [JSImport("INTERNAL.loadSatelliteAssemblies")]
    public static partial Task LoadSatelliteAssemblies(string[] culturesToLoad);
}
