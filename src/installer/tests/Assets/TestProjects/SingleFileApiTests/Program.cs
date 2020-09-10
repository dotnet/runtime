using System;

namespace SingleFileApiTests
{
    public class Program
    {
        public static void Main(string[] args)
        {
            switch (args[0])
            {
                case "fullyqualifiedname":
                    var module = typeof(object).Assembly.GetModules()[0];
                    Console.WriteLine("FullyQualifiedName: " + module.FullyQualifiedName);
                    Console.WriteLine("Name: " + module.Name);
                    return;

                case "cmdlineargs":
                    Console.WriteLine(Environment.GetCommandLineArgs()[0]);
                    return;

                case "codebase":
                    try
                    {
                        #pragma warning disable SYSLIB0012
                        _ = typeof(Program).Assembly.CodeBase;
                        #pragma warning restore SYSLIB0012
                    }
                    catch (NotSupportedException)
                    {
                        Console.WriteLine("CodeBase NotSupported");
                        return;
                    }
                    break;

                case "appcontext":
                    var deps_files = AppContext.GetData("APP_CONTEXT_DEPS_FILES");
                    Console.WriteLine("APP_CONTEXT_DEPS_FILES: " + deps_files);
                    return;
            }

            Console.WriteLine("test failure");
        }
    }
}
