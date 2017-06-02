using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.DependencyModel;
using NuGet.Common;
using NuGet.ProjectModel;
using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Microsoft.DotNet.Build.Tasks
{
    public partial class ProcessSharedFrameworkDeps
    {
        partial void EnsureInitialized(string buildToolsTaskDir)
        {
            // ensure the build tools AssemblyResolver is enabled, so we get the correct assembly unification
            // even if the build tools assembly hasn't been loaded yet.
            string buildTasksPath = Path.Combine(buildToolsTaskDir, "Microsoft.DotNet.Build.Tasks.dll");
            Assembly buildTasksAssembly = Assembly.Load(AssemblyName.GetAssemblyName(buildTasksPath));
            Type assemblyResolver = buildTasksAssembly.GetType("Microsoft.DotNet.Build.Common.Desktop.AssemblyResolver");
            assemblyResolver.GetMethod("Enable").Invoke(null, new object[] { });
        }
    }
}
