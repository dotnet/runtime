// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using System.Threading;
using System.Threading.Tasks;

namespace System.Runtime.InteropServices.JavaScript
{
    internal static partial class JSHostImplementation
    {
        public static Task BindAssemblyExports(string? assemblyName)
        {
            Interop.Runtime.BindAssemblyExports(Marshal.StringToCoTaskMemUTF8(assemblyName));
            return Task.CompletedTask;
        }
    }
}
