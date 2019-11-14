using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

internal class StartupHook
{
    public static void Initialize()
    {
        AssemblyLoadContext.Default.Resolving += SharedHostPolicy.SharedAssemblyResolver.Resolve;
    }
}

namespace SharedHostPolicy
{
    public class SharedAssemblyResolver
    {
        public static Assembly Resolve(AssemblyLoadContext context, AssemblyName assemblyName)
        {
            if (assemblyName.Name == "SharedLibrary")
            {
                string startupHookDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                string sharedLibrary = Path.GetFullPath(Path.Combine(startupHookDirectory, "SharedLibrary.dll"));
                return AssemblyLoadContext.Default.LoadFromAssemblyPath(sharedLibrary);
            }
            return null;
        }
    }
}
