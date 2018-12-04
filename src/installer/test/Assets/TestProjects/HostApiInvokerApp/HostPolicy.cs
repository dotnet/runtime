using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace HostApiInvokerApp
{
    public static class HostPolicy
    {
        internal static class hostpolicy
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = Utils.OSCharSet)]
            internal delegate void corehost_resolve_component_dependencies_result_fn(
                string assembly_paths,
                string native_search_paths,
                string resource_search_paths);

            [DllImport(nameof(hostpolicy), CharSet = Utils.OSCharSet, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int corehost_resolve_component_dependencies(
                string component_main_assembly_path, 
                corehost_resolve_component_dependencies_result_fn result);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = Utils.OSCharSet)]
            internal delegate void corehost_error_writer_fn(
                string message);

            [DllImport(nameof(hostpolicy), CharSet = Utils.OSCharSet, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr corehost_set_error_writer(
                corehost_error_writer_fn error_writer);
        }

        static void Test_corehost_resolve_component_dependencies_internal(string prefix, string assemblyPath)
        {
            StringBuilder errorBuilder = new StringBuilder();

            hostpolicy.corehost_set_error_writer((message) =>
            {
                errorBuilder.AppendLine(message);
            });

            string assemblies = null;
            string nativeSearchPaths = null;
            string resourceSearcPaths = null;
            int rc;
            try
            {
                rc = hostpolicy.corehost_resolve_component_dependencies(
                    assemblyPath,
                    (assembly_paths, native_search_paths, resource_search_paths) => 
                    {
                        assemblies = assembly_paths;
                        nativeSearchPaths = native_search_paths;
                        resourceSearcPaths = resource_search_paths;
                    });
            }
            finally
            {
                hostpolicy.corehost_set_error_writer(null);
            }

            if (assemblies != null)
            {
                // Sort the assemblies since in the native code we store it in a hash table
                // which gives random order. The native code always adds the separator at the end
                // so mimic that behavior as well.
                assemblies = string.Join(Path.PathSeparator, assemblies.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).OrderBy(a => a)) + Path.PathSeparator;
            }

            if (rc == 0)
            {
                Console.WriteLine($"{prefix}corehost_resolve_component_dependencies:Success");
                Console.WriteLine($"{prefix}corehost_resolve_component_dependencies assemblies:[{assemblies}]");
                Console.WriteLine($"{prefix}corehost_resolve_component_dependencies native_search_paths:[{nativeSearchPaths}]");
                Console.WriteLine($"{prefix}corehost_resolve_component_dependencies resource_search_paths:[{resourceSearcPaths}]");
            }
            else
            {
                Console.WriteLine($"{prefix}corehost_resolve_component_dependencies:Fail[0x{rc.ToString("X8")}]");
            }

            if (errorBuilder.Length > 0)
            {
                IEnumerable<string> errorLines = errorBuilder.ToString().Split(new string[] { Environment.NewLine }, StringSplitOptions.None)
                    .Select(line => prefix + line);
                Console.WriteLine($"{prefix}corehost reported errors:{Environment.NewLine}{string.Join(Environment.NewLine, errorLines)}");
            }
        }

        static void Test_corehost_resolve_component_dependencies(string[] args)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException("Invalid number of arguments passed");
            }

            Test_corehost_resolve_component_dependencies_internal("", args[1]);
        }

        static void Test_corehost_resolve_component_dependencies_multithreaded(string[] args)
        {
            if (args.Length != 3)
            {
                throw new ArgumentException("Invalid number of arguments passed");
            }

            Func<string, Thread> createThread = (string assemblyPath) =>
            {
                return new Thread(() =>
                {
                    Test_corehost_resolve_component_dependencies_internal(
                        Path.GetFileNameWithoutExtension(assemblyPath) + ": ",
                        assemblyPath
                    );
                });
            };

            Thread t1 = createThread(args[1]);
            Thread t2 = createThread(args[2]);

            t1.Start();
            t2.Start();

            if (!t1.Join(TimeSpan.FromSeconds(30)))
            {
                throw new ApplicationException("Thread 1 didn't finish in time.");
            }

            if (!t2.Join(TimeSpan.FromSeconds(30)))
            {
                throw new ApplicationException("Thread 1 didn't finish in time.");
            }
        }

        static void Test_corehost_set_error_writer(string[] args)
        {
            hostpolicy.corehost_error_writer_fn writer1 = (message) => { Console.WriteLine(nameof(writer1)); };
            IntPtr writer1Ptr = Marshal.GetFunctionPointerForDelegate(writer1);

            if (hostpolicy.corehost_set_error_writer(writer1) != IntPtr.Zero)
            {
                throw new ApplicationException("Error writer should be null by default.");
            }

            hostpolicy.corehost_error_writer_fn writer2 = (message) => { Console.WriteLine(nameof(writer2)); };
            IntPtr writer2Ptr = Marshal.GetFunctionPointerForDelegate(writer2);
            IntPtr previousWriterPtr = hostpolicy.corehost_set_error_writer(writer2);

            if (previousWriterPtr != writer1Ptr)
            {
                throw new ApplicationException("First: The previous writer returned is not the one expected.");
            }

            previousWriterPtr = hostpolicy.corehost_set_error_writer(null);
            if (previousWriterPtr != writer2Ptr)
            {
                throw new ApplicationException("Second: The previous writer returned is not the one expected.");
            }
        }

        public static bool RunTest(string apiToTest, string[] args)
        {
            switch (apiToTest)
            {
                case nameof(hostpolicy.corehost_resolve_component_dependencies):
                    Test_corehost_resolve_component_dependencies(args);
                    break;
                case nameof(hostpolicy.corehost_resolve_component_dependencies) + "_multithreaded":
                    Test_corehost_resolve_component_dependencies_multithreaded(args);
                    break;
                case nameof(Test_corehost_set_error_writer):
                    Test_corehost_set_error_writer(args);
                    break;
                default:
                    return false;
            }

            Utils.LogModulePath("hostpolicy");

            return true;
        }
    }
}