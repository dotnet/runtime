// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.PortableExecutable.Tests
{
    public static class TestBuilders
    {
        public static byte[] BuildPEWithDebugDirectory(DebugDirectoryBuilder debugDirectoryBuilder)
        {
            var peStream = new MemoryStream();
            var ilBuilder = new BlobBuilder();
            var metadataBuilder = new MetadataBuilder();

            var peBuilder = new ManagedPEBuilder(
                PEHeaderBuilder.CreateLibraryHeader(),
                new MetadataRootBuilder(metadataBuilder),
                ilBuilder,
                debugDirectoryBuilder: debugDirectoryBuilder);

            var peImageBuilder = new BlobBuilder();
            peBuilder.Serialize(peImageBuilder);
            return peImageBuilder.ToArray();
        }
    }
}
