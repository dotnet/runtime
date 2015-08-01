// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Reflection;

namespace System.Runtime.Loader.Tests
{
    public class CustomTPALoadContext : AssemblyLoadContext
    {
        protected override Assembly Load(AssemblyName assemblyName)
        {
            string assemblyPath = Path.Combine(Directory.GetCurrentDirectory(), assemblyName.Name + ".dll");
            return LoadFromAssemblyPath(assemblyPath);
        }
    }
}
