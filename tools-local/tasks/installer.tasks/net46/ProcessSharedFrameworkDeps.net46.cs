// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;

namespace Microsoft.DotNet.Build.Tasks
{
    public partial class ProcessSharedFrameworkDeps
    {
        partial void EnsureInitialized(string buildTasksAssemblyPath)
        {
            // Ensure the Arcade AssemblyResolver is enabled, so we get the correct assembly
            // unification even if an Arcade assembly hasn't been loaded yet.
            Assembly buildTasksAssembly = Assembly.Load(AssemblyName.GetAssemblyName(buildTasksAssemblyPath));
            Type assemblyResolver = buildTasksAssembly.GetType("Microsoft.DotNet.Build.Common.Desktop.AssemblyResolver");
            assemblyResolver.GetMethod("Enable").Invoke(null, new object[] { });
        }
    }
}
