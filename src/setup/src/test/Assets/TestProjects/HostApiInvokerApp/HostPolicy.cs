using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

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
        }

        static void Test_corehost_resolve_component_dependencies(string[] args)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException("Invalid number of arguments passed");
            }

            string assemblies = null;
            string nativeSearchPaths = null;
            string resourceSearcPaths = null;
            int rc = hostpolicy.corehost_resolve_component_dependencies(
                args[1],
                (assembly_paths, native_search_paths, resource_search_paths) => 
                {
                    assemblies = assembly_paths;
                    nativeSearchPaths = native_search_paths;
                    resourceSearcPaths = resource_search_paths;
                });

            if (assemblies != null)
            {
                // Sort the assemblies since in the native code we store it in a hash table
                // which gives random order. The native code always adds the separator at the end
                // so mimic that behavior as well.
                assemblies = string.Join(System.IO.Path.PathSeparator, assemblies.Split(System.IO.Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries).OrderBy(a => a)) + System.IO.Path.PathSeparator;
            }

            if (rc == 0)
            {
                Console.WriteLine("corehost_resolve_component_dependencies:Success");
                Console.WriteLine($"corehost_resolve_component_dependencies assemblies:[{assemblies}]");
                Console.WriteLine($"corehost_resolve_component_dependencies native_search_paths:[{nativeSearchPaths}]");
                Console.WriteLine($"corehost_resolve_component_dependencies resource_search_paths:[{resourceSearcPaths}]");
            }
            else
            {
                Console.WriteLine($"corehost_resolve_component_dependencies:Fail[0x{rc.ToString("X8")}]");
            }
        }

        public static bool RunTest(string apiToTest, string[] args)
        {
            switch (apiToTest)
            {
                case nameof(hostpolicy.corehost_resolve_component_dependencies):
                    Test_corehost_resolve_component_dependencies(args);
                    break;
                default:
                    return false;
            }

            Utils.LogModulePath("hostpolicy");

            return true;
        }
    }
}