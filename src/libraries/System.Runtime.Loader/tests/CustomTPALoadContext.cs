// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection;

namespace System.Runtime.Loader.Tests
{
    public class CustomTPALoadContext : AssemblyLoadContext
    {
        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = Path.Combine(Path.GetDirectoryName(typeof(string).Assembly.Location), assemblyName.Name + ".dll");
            return LoadFromAssemblyPath(assemblyPath);
        }
    }
}
