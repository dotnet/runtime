using System;
using System.Runtime.InteropServices;
using System.Text;

namespace StandaloneApp
{
    public static class Program
    {
        [DllImport("hostfxr", CharSet = CharSet.Unicode)]
        static extern uint hostfxr_get_native_search_directories(int argc, IntPtr argv, StringBuilder buffer, int bufferSize, ref int required_buffer_size);

        const uint HostApiBufferTooSmall = 0x80008098;

        public static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine(string.Join(Environment.NewLine, args));

            // A small operation involving NewtonSoft.Json to ensure the assembly is loaded properly
            var t = typeof(Newtonsoft.Json.JsonReader);

            if (args.Length == 0)
            {
                throw new Exception("Invalid number of arguments passed");
            }

            string apiToTest = args[0];
            if (apiToTest == "hostfxr_get_native_search_directories")
            {
                Test_hostfxr_get_native_search_directories(args);
            }
            else
            {
                throw new Exception("Invalid args[0]");
            }
        }

        /// <summary>
        /// Test invoking the native hostfxr api hostfxr_get_native_search_directories
        /// </summary>
        /// <param name="args[0]">hostfxr_get_native_search_directories</param>
        /// <param name="args[1]">Path to dotnet.exe</param>
        /// <param name="args[2]">Path to application</param>
        static void Test_hostfxr_get_native_search_directories(string[] args)
        {
            if (args.Length != 3)
            {
                throw new Exception("Invalid number of arguments passed");
            }

            string pathToDotnet = args[1];
            string pathToApp = args[2];

            IntPtr[] argv = new IntPtr[2];
            argv[0] = Marshal.StringToHGlobalUni(pathToDotnet);
            argv[1] = Marshal.StringToHGlobalUni(pathToApp);

            GCHandle gch = GCHandle.Alloc(argv, GCHandleType.Pinned);

            // Start with 0 bytes allocated to test re-entry and required_buffer_size
            StringBuilder buffer = new StringBuilder(0);
            int required_buffer_size = 0;

            uint rc = 0;
            for (int i = 0; i < 2; i++)
            {
                rc = hostfxr_get_native_search_directories(argv.Length, gch.AddrOfPinnedObject(), buffer, buffer.Capacity + 1, ref required_buffer_size);
                if (rc != HostApiBufferTooSmall)
                {
                    break;
                }

                buffer = new StringBuilder(required_buffer_size);
            }

            gch.Free();
            for (int i = 0; i < argv.Length; ++i)
            {
                Marshal.FreeHGlobal(argv[i]);
            }

            if (rc == 0)
            {
                Console.WriteLine("hostfxr_get_native_search_directories:Success");
                Console.WriteLine($"hostfxr_get_native_search_directories buffer:[{buffer}]");
            }
            else
            {
                Console.WriteLine($"hostfxr_get_native_search_directories:Fail[{rc}]");
            }
        }
    }
}
