// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Immutable;
using System.IO;

namespace System.Reflection.Metadata.Tests
{
    internal static class Interop
    {
        public static readonly byte[] IndexerWithByRefParam = ResourceHelper.GetResource("Interop.IndexerWithByRefParam.dll");
        public static readonly byte[] Interop_Mock01 = ResourceHelper.GetResource("Interop.Interop.Mock01.dll");
        public static readonly byte[] Interop_Mock01_Impl = ResourceHelper.GetResource("Interop.Interop.Mock01.Impl.dll");
    }

    internal static class Misc
    {
        public static readonly byte[] CPPClassLibrary2 = ResourceHelper.GetResource("Misc.CPPClassLibrary2.obj");
        public static readonly byte[] EmptyType = ResourceHelper.GetResource("Misc.EmptyType.dll");
        public static readonly byte[] Members = ResourceHelper.GetResource("Misc.Members.dll");
        public static readonly byte[] Deterministic = ResourceHelper.GetResource("Misc.Deterministic.dll");
        public static readonly byte[] Debug = ResourceHelper.GetResource("Misc.Debug.dll");
        public static readonly byte[] KeyPair = ResourceHelper.GetResource("Misc.KeyPair.snk");

        public static readonly byte[] KeyPair_PublicKey = new byte[] 
        {
            0x00, 0x24, 0x00, 0x00, 0x04, 0x80, 0x00, 0x00, 0x94, 0x00, 0x00, 0x00, 0x06, 0x02, 0x00, 0x00,
            0x00, 0x24, 0x00, 0x00, 0x52, 0x53, 0x41, 0x31, 0x00, 0x04, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00,
            0x33, 0x61, 0x19, 0xca, 0x32, 0xc4, 0x2b, 0xc8, 0x1e, 0x80, 0x48, 0xc1, 0xa9, 0xb2, 0x75, 0xa8,
            0xdf, 0x83, 0x1b, 0xb1, 0xeb, 0x4c, 0xf4, 0xdf, 0xdf, 0x99, 0xec, 0x35, 0x15, 0x35, 0x80, 0x0e,
            0x26, 0x85, 0x15, 0x73, 0x19, 0xba, 0xdc, 0xff, 0xb7, 0x0c, 0x96, 0x3e, 0xa0, 0x9b, 0x0a, 0x62,
            0x01, 0x17, 0x4b, 0x45, 0xa0, 0x76, 0x0a, 0xa8, 0xdb, 0x08, 0xbe, 0x16, 0x56, 0xa3, 0x20, 0x53,
            0xef, 0xf2, 0x12, 0x25, 0x85, 0xe7, 0x40, 0x74, 0x8e, 0x0a, 0xb8, 0x3e, 0xd7, 0xbf, 0xad, 0x13,
            0x1a, 0xa9, 0x81, 0x22, 0x86, 0xc9, 0x5f, 0xa5, 0x27, 0xde, 0x70, 0x40, 0x8b, 0xd0, 0xf4, 0x6a,
            0xfb, 0x48, 0x23, 0x8a, 0x27, 0x00, 0xe1, 0x80, 0xad, 0xd4, 0x08, 0xd4, 0x43, 0xf0, 0xcd, 0xd8,
            0x57, 0x1d, 0x5b, 0xa1, 0x5f, 0x96, 0x72, 0x58, 0xd7, 0x4a, 0xcc, 0xa7, 0x82, 0x00, 0x11, 0xcf
        };
    }

    internal static class NetModule
    {
        public static readonly byte[] ModuleCS01 = ResourceHelper.GetResource("NetModule.ModuleCS01.mod");
        public static readonly byte[] ModuleVB01 = ResourceHelper.GetResource("NetModule.ModuleVB01.mod");
        public static readonly byte[] AppCS = ResourceHelper.GetResource("NetModule.AppCS.exe");
    }

    internal static class Namespace
    {
        public static readonly byte[] NamespaceTests = ResourceHelper.GetResource("Namespace.NamespaceTests.dll");
    }

    internal static class WinRT
    {
        public static readonly byte[] Lib = ResourceHelper.GetResource("WinRT.Lib.winmd");
    }

    internal static class PortablePdbs
    {
        public static readonly byte[] DocumentsDll = ResourceHelper.GetResource("PortablePdbs.Documents.dll");
        public static readonly byte[] DocumentsPdb = ResourceHelper.GetResource("PortablePdbs.Documents.pdb");
    }

    internal static class ResourceHelper
    {
        public static byte[] GetResource(string name)
        {
            string fullName = "System.Reflection.Metadata.Tests.Resources." + name;
            using (var stream = typeof(ResourceHelper).GetTypeInfo().Assembly.GetManifestResourceStream(fullName))
            {
                var bytes = new byte[stream.Length];
                using (var memoryStream = new MemoryStream(bytes))
                {
                    stream.CopyTo(memoryStream);
                }
                return bytes;
            }
        }
    }
}
