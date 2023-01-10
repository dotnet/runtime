using System.Diagnostics;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;

using Common;

namespace Regression.Performance
{
    public unsafe class Compare
    {
        private PEReader _peReader;
        private PEMemoryBlock _metadataBlock;

        delegate* unmanaged<void*, int, nint, int> _Initialize;

        private readonly struct Scenario
        {
            public string Name { get; init; }
            public delegate* unmanaged<int, int> Baseline { get; init; }
            public delegate* unmanaged<int, int> Current { get; init; }
        }

        List<Scenario> _scenarios = new();

        public Compare(string currentPath)
        {
            // Load System.Private.CoreLib
            var spcl = typeof(object).Assembly.Location;
            _peReader = new(File.OpenRead(spcl));
            _metadataBlock = _peReader.GetMetadata();

            // Acquire native functions
            nint mod = NativeLibrary.Load(currentPath);
            _Initialize = (delegate* unmanaged<void*, int, nint, int>)NativeLibrary.GetExport(mod, "Initialize");

            string[] scenarioNames = new[]
            {
                "CreateImport",
                "EnumTypeDefs",
                "GetScopeProps",
                "EnumUserStrings",
                "GetCustomAttributeByName",
            };

            // Look up each scenario test export.
            foreach (var name in scenarioNames)
            {
                _scenarios.Add(new Scenario()
                {
                    Name = name,
                    Baseline = (delegate* unmanaged<int, int>)NativeLibrary.GetExport(mod, $"Baseline{name}"),
                    Current = (delegate* unmanaged<int, int>)NativeLibrary.GetExport(mod, $"Current{name}"),
                });
            }

            int hr = _Initialize(_metadataBlock.Pointer, _metadataBlock.Length, Dispensers.BaselineRaw);
            if (hr < 0)
            {
                throw new Exception($"Initialization failed: 0x{hr:x}");
            }
        }

        public void Run(int iter)
        {
            int hr;
            const int width = 12;
            var sw = new Stopwatch();

            foreach (var scenario in _scenarios)
            {
                Console.WriteLine(scenario.Name);
                sw.Restart();
                hr = scenario.Baseline(iter);
                Console.WriteLine($"  Baseline: {sw.ElapsedMilliseconds,width}");
                if (hr < 0) throw new Exception($"Failure 0x{hr:x}");
                sw.Restart();
                hr = scenario.Current(iter);
                Console.WriteLine($"  Current:  {sw.ElapsedMilliseconds,width}");
                if (hr < 0) throw new Exception($"Failure 0x{hr:x}");
            }
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            string regperfPath;
            if (args.Length > 0)
            {
                regperfPath = args[0];
            }
            else
            {
                regperfPath =
                    OperatingSystem.IsWindows() ? "regperf.dll"
                    : OperatingSystem.IsMacOS() ? "libregperf.dylib"
                    : "libregperf.so";
            }

            var test = new Compare(regperfPath);

            Console.WriteLine("Warm-up");
            test.Run(100);

            const int iter = 100_000;
            Console.WriteLine($"\nRun iterations - {iter}");
            test.Run(iter);
        }
    }
}
