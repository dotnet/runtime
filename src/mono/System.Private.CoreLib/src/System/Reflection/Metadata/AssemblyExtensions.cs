// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata
{
    public static class AssemblyExtensions
    {
        [CLSCompliant(false)]
        public static unsafe bool TryGetRawMetadata(this Assembly assembly, out byte* blob, out int length)
        {
            ArgumentNullException.ThrowIfNull(assembly);

            if (assembly is RuntimeAssembly runtimeAssembly)
            {
                return runtimeAssembly.TryGetRawMetadata(out blob, out length);
            }

            blob = null;
            length = 0;
            return false;
        }
    }
}
