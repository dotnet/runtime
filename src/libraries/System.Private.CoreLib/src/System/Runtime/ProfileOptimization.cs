// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Loader;

namespace System.Runtime
{
    public static class ProfileOptimization
    {
        public static void SetProfileRoot(string directoryPath)
        {
            AssemblyLoadContext.Default.SetProfileOptimizationRoot(directoryPath);
        }

        public static void StartProfile(string? profile)
        {
            AssemblyLoadContext.Default.StartProfileOptimization(profile);
        }
    }
}
