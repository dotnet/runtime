// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class AssemblyReferenceTests
    {
        [Fact]
        public void ValidateAssemblyNameForSingleAssemblyReference()
        {
            var reader = MetadataReaderTests.GetMetadataReader(WinRT.Lib, options: MetadataReaderOptions.ApplyWindowsRuntimeProjections);
            var handle = reader.AssemblyReferences.Skip(3).First();
            var assemblyRef = reader.GetAssemblyReference(handle);
            var assemblyDef = reader.GetAssemblyDefinition();
            var assemblyName = assemblyRef.GetAssemblyName();

            // Validate against input assembly
            Assert.Equal("System.Runtime", assemblyName.Name);
            Assert.Equal(new Version(4, 0, 0, 0), assemblyName.Version);
            Assert.Equal(new byte[] { 0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A }, assemblyName.GetPublicKeyToken());
            Assert.Equal("",assemblyName.CultureName);
            Assert.Equal(Configuration.Assemblies.AssemblyHashAlgorithm.None, assemblyName.HashAlgorithm);
            Assert.Null(assemblyName.GetPublicKey());
            Assert.Equal(AssemblyNameFlags.None, assemblyName.Flags);
            Assert.Equal(AssemblyContentType.Default, assemblyName.ContentType);

            // Validate against AssemblyReference
            ValidateReferenceAssemblyNameAgainst(assemblyName, reader, assemblyRef);

            // Validate against AssemblyDefinition
            ValidateReferenceAssemblyNameAgainst(assemblyName, reader, assemblyDef);
        }

        [Fact]
        public void ValidateAssemblyNameForMultipleAssemblyReferences()
        {
            var expRefs = new string[] { "mscorlib", "System.Core", "System", "Microsoft.VisualBasic" };

            byte[][] expKeys = new byte[][]
            {
                new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 },
                new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 },
                new byte[] { 0xb7, 0x7a, 0x5c, 0x56, 0x19, 0x34, 0xe0, 0x89 },

                // VB: B0 3F 5F 7F 11 D5 0A 3A
                new byte[] { 0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a }
            };

            var expVers = new Version[]
            {
                new Version(4, 0, 0, 0),
                new Version(4, 0, 0, 0),
                new Version(4, 0, 0, 0),
                new Version(/*VB*/10, 0, 0, 0),
            };

            var reader = MetadataReaderTests.GetMetadataReader(NetModule.AppCS);

            int i = 0;
            foreach (var assemblyRefHandle in reader.AssemblyReferences)
            {
                var assemblyRef = reader.GetAssemblyReference(assemblyRefHandle);
                var assemblyDef = reader.GetAssemblyDefinition();
                var assemblyName = assemblyRef.GetAssemblyName();

                // Validate against input assembly
                Assert.Equal(expRefs[i], assemblyName.Name);
                Assert.Equal(expVers[i], assemblyName.Version);
                Assert.Equal(expKeys[i], assemblyName.GetPublicKeyToken());
                Assert.Equal("",assemblyName.CultureName);
                Assert.Equal(Configuration.Assemblies.AssemblyHashAlgorithm.None, assemblyName.HashAlgorithm);
                Assert.Null(assemblyName.GetPublicKey());
                Assert.Equal(AssemblyNameFlags.None, assemblyName.Flags);
                Assert.Equal(AssemblyContentType.Default, assemblyName.ContentType);

                // Validate against AssemblyReference
                ValidateReferenceAssemblyNameAgainst(assemblyName, reader, assemblyRef);

                // Validate against AssemblyDefinition
                ValidateReferenceAssemblyNameAgainst(assemblyName, reader, assemblyDef);

                i++;
            }
        }

        private void ValidateReferenceAssemblyNameAgainst(AssemblyName assemblyName, MetadataReader reader, AssemblyReference assemblyRef)
        {
            Assert.Equal(reader.GetString(assemblyRef.Name), assemblyName.Name);
            Assert.Equal(assemblyRef.Version, assemblyName.Version);
            Assert.Equal(reader.GetBlobBytes(assemblyRef.PublicKeyOrToken), assemblyName.GetPublicKeyToken());
        }

        private void ValidateReferenceAssemblyNameAgainst(AssemblyName assemblyName, MetadataReader reader, AssemblyDefinition assemblyDef)
        {
            Assert.NotEqual(reader.GetString(assemblyDef.Name), assemblyName.Name);
            Assert.NotEqual(assemblyDef.Version, assemblyName.Version);
        }
    }
}
