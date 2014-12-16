// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Reflection;

namespace TestResources
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
