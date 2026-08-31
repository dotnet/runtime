// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

namespace System
{
    public class AssemblyLoadEventArgs : EventArgs
    {
        public AssemblyLoadEventArgs(Assembly loadedAssembly)
        {
            LoadedAssembly = loadedAssembly;
        }

        public Assembly LoadedAssembly { get; }
    }
}
