// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace Microsoft.NET.WebAssembly.Webcil;


public sealed partial class WebcilReader
{

    // Helpers to call into System.Reflection.Metadata internals
    internal static class Reflection
    {
        private static readonly Lazy<MethodInfo> s_readUtf8NullTerminated = new Lazy<MethodInfo>(() =>
        {
            var mi = typeof(BlobReader).GetMethod("ReadUtf8NullTerminated", BindingFlags.NonPublic | BindingFlags.Instance);
            if (mi == null)
            {
                throw new InvalidOperationException("Could not find BlobReader.ReadUtf8NullTerminated");
            }
            return mi;
        });

        internal static string? ReadUtf8NullTerminated(ref BlobReader reader)
        {
            object boxedReader = reader;
            string? result = (string?)s_readUtf8NullTerminated.Value.Invoke(boxedReader, null);
            reader = (BlobReader) boxedReader; // the call modifies the struct state, make sure to copy it back.
            return result;
        }

        private static readonly Lazy<ConstructorInfo> s_codeViewDebugDirectoryDataCtor = new Lazy<ConstructorInfo>(() =>
        {
            var types = new Type[] { typeof(Guid), typeof(int), typeof(string) };
            var mi = typeof(CodeViewDebugDirectoryData).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);
            if (mi == null)
            {
                throw new InvalidOperationException("Could not find CodeViewDebugDirectoryData constructor");
            }
            return mi;
        });

        internal static CodeViewDebugDirectoryData MakeCodeViewDebugDirectoryData(Guid guid, int age, string path) => (CodeViewDebugDirectoryData)s_codeViewDebugDirectoryDataCtor.Value.Invoke(new object[] { guid, age, path });

        private static readonly Lazy<ConstructorInfo> s_pdbChecksumDebugDirectoryDataCtor = new Lazy<ConstructorInfo>(() =>
        {
            var types = new Type[] { typeof(string), typeof(ImmutableArray<byte>) };
            var mi = typeof(PdbChecksumDebugDirectoryData).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null, types, null);
            if (mi == null)
            {
                throw new InvalidOperationException("Could not find PdbChecksumDebugDirectoryData constructor");
            }
            return mi;
        });
        internal static PdbChecksumDebugDirectoryData MakePdbChecksumDebugDirectoryData(string algorithmName, ImmutableArray<byte> checksum) => (PdbChecksumDebugDirectoryData)s_pdbChecksumDebugDirectoryDataCtor.Value.Invoke(new object[] { algorithmName, checksum });
    }
}
