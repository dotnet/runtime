using System;
using System.IO;
using System.Reflection;

namespace SingleFileApiTests
{
    public class Program
    {
        public static int Main(string[] args)
        {
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i];
                switch (arg)
                {
                    case "appcontext":
                        var deps_files = (string)AppContext.GetData("APP_CONTEXT_DEPS_FILES");
                        Console.WriteLine("APP_CONTEXT_DEPS_FILES: " + deps_files);
                        foreach (string deps_file_path in deps_files.Split(";", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                        {
                            if (!File.Exists(deps_file_path))
                            {
                                Console.WriteLine($"APP_CONTEXT_DEPS_FILES contains path which doesn't exist: '{deps_file_path}'");
                                return -1;
                            }
                        }
                        break;

                    case "executing_assembly_location":
                        Console.WriteLine("ExecutingAssembly.Location: " + Assembly.GetExecutingAssembly().Location);
                        break;

                    case "assembly_location":
                        string assemblyName = args[++i];
                        Console.WriteLine(assemblyName + " location: " + Assembly.Load(assemblyName).Location);
                        break;

                    case "cmdlineargs":
                        Console.WriteLine("Command line args: " + Environment.GetCommandLineArgs()[0]);
                        break;

                    case "codebase":
                        try
                        {
                            #pragma warning disable SYSLIB0012
                            var codeBase = typeof(Program).Assembly.CodeBase;
                            #pragma warning restore SYSLIB0012
                            Console.WriteLine("CodeBase " + codeBase);
                        }
                        catch (NotSupportedException)
                        {
                            Console.WriteLine("CodeBase NotSupported");
                        }
                        break;

                    case "fullyqualifiedname":
                        var module = typeof(object).Assembly.GetModules()[0];
                        Console.WriteLine("FullyQualifiedName: " + module.FullyQualifiedName);
                        Console.WriteLine("Name: " + module.Name);
                        break;

                    case "trusted_platform_assemblies":
                        Console.WriteLine("TRUSTED_PLATFORM_ASSEMBLIES:");
                        foreach (var assemblyPath in ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")).Split(Path.PathSeparator))
                        {
                            Console.WriteLine(assemblyPath);
                        }
                        break;

                    case "basedirectory":
                        Console.WriteLine("AppContext.BaseDirectory: " + AppContext.BaseDirectory);
                        break;

                    case "native_search_dirs":
                        var native_search_dirs = AppContext.GetData("NATIVE_DLL_SEARCH_DIRECTORIES");
                        Console.WriteLine("NATIVE_DLL_SEARCH_DIRECTORIES: " + native_search_dirs);
                        break;

                    default:
                        Console.WriteLine("test failure");
                        return -1;
                }
            }

            return 0;
        }
    }
}
