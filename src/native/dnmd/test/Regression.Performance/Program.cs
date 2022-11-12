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

        delegate* unmanaged<int, int> _BaselineCreateImport;
        delegate* unmanaged<int, int> _CurrentCreateImport;
        delegate* unmanaged<int, int> _BaselineEnumTypeDefs;
        delegate* unmanaged<int, int> _CurrentEnumTypeDefs;

        public Compare(string currentPath)
        {
            // Load System.Private.CoreLib
            var spcl = typeof(object).Assembly.Location;
            _peReader = new(File.OpenRead(spcl));
            _metadataBlock = _peReader.GetMetadata();

            // Acquire native functions
            nint mod = NativeLibrary.Load(currentPath);
            _Initialize = (delegate* unmanaged<void*, int, nint, int>)NativeLibrary.GetExport(mod, "Initialize");
            _BaselineCreateImport = (delegate* unmanaged<int, int>)NativeLibrary.GetExport(mod, "BaselineCreateImport");
            _CurrentCreateImport = (delegate* unmanaged<int, int>)NativeLibrary.GetExport(mod, "CurrentCreateImport");
            _BaselineEnumTypeDefs = (delegate* unmanaged<int, int>)NativeLibrary.GetExport(mod, "BaselineEnumTypeDefs");
            _CurrentEnumTypeDefs = (delegate* unmanaged<int, int>)NativeLibrary.GetExport(mod, "CurrentEnumTypeDefs");

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
            const string msg = "Falure";
            var sw = new Stopwatch();

            Console.WriteLine("CreateImport");
            sw.Restart();
            hr = _BaselineCreateImport(iter);
            Console.WriteLine($"  Baseline: {sw.ElapsedMilliseconds,width}");
            if (hr < 0) throw new Exception(msg);
            sw.Restart();
            hr = _CurrentCreateImport(iter);
            Console.WriteLine($"  Current:  {sw.ElapsedMilliseconds,width}");
            if (hr < 0) throw new Exception(msg);

            Console.WriteLine("EnumTypeDefs");
            sw.Restart();
            hr = _BaselineEnumTypeDefs(iter);
            Console.WriteLine($"  Baseline: {sw.ElapsedMilliseconds,width}");
            if (hr < 0) throw new Exception(msg);
            sw.Restart();
            hr = _CurrentEnumTypeDefs(iter);
            Console.WriteLine($"  Current:  {sw.ElapsedMilliseconds,width}");
            if (hr < 0) throw new Exception(msg);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.Error.WriteLine("Must provide path to regperf.");
                return;
            }

            var path = args[0];

            var test = new Compare(path);

            Console.WriteLine("Warm-up");
            test.Run(100);

            const int iter = 1_000_000;
            Console.WriteLine($"\nRun iterations - {iter}");
            test.Run(iter);
        }
    }
}
