// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class AssemblyDefinitionTests
    {
        [Fact]
        public void ValidateAssemblyNameWithCultureSet()
        {
            var assembly = new { Assembly = Misc.Satellite, Name = "SatelliteAssembly.resources", Version = new Version(1, 0, 0, 0), ContentType = AssemblyContentType.Default, Culture = "de-DE" };

            var reader = MetadataReaderTests.GetMetadataReader(assembly.Assembly, options: MetadataReaderOptions.ApplyWindowsRuntimeProjections);

            foreach (var assemblyRefHandle in reader.AssemblyReferences)
            {
                var assemblyDef = reader.GetAssemblyDefinition();
                var assemblyRef = reader.GetAssemblyReference(assemblyRefHandle);
                var assemblyName = assemblyDef.GetAssemblyName();

                // Validate against input assembly
                Assert.Equal(assembly.Version, assemblyName.Version);
                Assert.Equal(assembly.Name, assemblyName.Name);
                Assert.Equal(assembly.ContentType, assemblyName.ContentType);
                Assert.Equal(assembly.Culture, assemblyName.CultureName);
                Assert.Equal(Configuration.Assemblies.AssemblyHashAlgorithm.SHA1, assemblyName.HashAlgorithm);
                Assert.Null(assemblyName.GetPublicKey());
                Assert.Empty(assemblyName.GetPublicKeyToken());
                Assert.Equal(AssemblyNameFlags.None, assemblyName.Flags);

                // Validate against AssemblyDefinition
                ValidateDefinitionAssemblyNameAgainst(assemblyName, reader, assemblyDef);

                // Validate against AssemblyReference
                ValidateDefinitionAssemblyNameAgainst(assemblyName, reader, assemblyRef);
            }
        }

        [Fact]
        public void ValidateAssemblyNameWithPublicKey()
        {
            var assembly = new { Assembly = Misc.Signed, Name = "Signed", Version = new Version(0, 0, 0, 0), ContentType = AssemblyContentType.Default, Flags = AssemblyNameFlags.PublicKey };

            var reader = MetadataReaderTests.GetMetadataReader(assembly.Assembly, options: MetadataReaderOptions.ApplyWindowsRuntimeProjections);

            foreach (var assemblyRefHandle in reader.AssemblyReferences)
            {
                var assemblyDef = reader.GetAssemblyDefinition();
                var assemblyRef = reader.GetAssemblyReference(assemblyRefHandle);
                var assemblyName = assemblyDef.GetAssemblyName();

                // Validate against input assembly
                Assert.Equal(assembly.Version, assemblyName.Version);
                Assert.Equal(assembly.Name, assemblyName.Name);
                Assert.Equal(assembly.ContentType, assemblyName.ContentType);
                Assert.Equal("", assemblyName.CultureName);
                Assert.Equal(Configuration.Assemblies.AssemblyHashAlgorithm.SHA1, assemblyName.HashAlgorithm);
                Assert.Equal(assembly.Flags, assemblyName.Flags);
                Assert.NotNull(assemblyName.GetPublicKeyToken());
                Assert.NotNull(assemblyName.GetPublicKey());

                // Validate against AssemblyDefinition
                ValidateDefinitionAssemblyNameAgainst(assemblyName, reader, assemblyDef);

                // Validate against AssemblyReference
                ValidateDefinitionAssemblyNameAgainst(assemblyName, reader, assemblyRef);
            }
        }

        [Fact]
        public void ValidateAssemblyNameForAssemblyDefinition()
        {
            var assemblyItems = new[]
            {
                new { Assembly = WinRT.Lib, Name = "Lib", Version = new Version(1, 0, 0, 0), ContentType = AssemblyContentType.WindowsRuntime },
                new { Assembly = NetModule.AppCS, Name = "AppCS", Version = new Version(1, 2, 3, 4), ContentType = AssemblyContentType.Default },
                new { Assembly = Namespace.NamespaceTests, Name = "NamespaceTests", Version = new Version(0, 0, 0, 0), ContentType = AssemblyContentType.Default },
                new { Assembly = PortablePdbs.DocumentsDll, Name = "Documents", Version = new Version(0, 0, 0, 0), ContentType = AssemblyContentType.Default }
            };

            foreach (var item in assemblyItems)
            {
                var reader = MetadataReaderTests.GetMetadataReader(item.Assembly, options: MetadataReaderOptions.ApplyWindowsRuntimeProjections);

                foreach (var assemblyRefHandle in reader.AssemblyReferences)
                {
                    var assemblyDef = reader.GetAssemblyDefinition();
                    var assemblyRef = reader.GetAssemblyReference(assemblyRefHandle);
                    var assemblyName = assemblyDef.GetAssemblyName();

                    // Validate against input assembly
                    Assert.Equal(item.Version, assemblyName.Version);
                    Assert.Equal(item.Name, assemblyName.Name);
                    Assert.Equal(item.ContentType, assemblyName.ContentType);
                    Assert.Equal("", assemblyName.CultureName);
                    Assert.Equal(Configuration.Assemblies.AssemblyHashAlgorithm.SHA1, assemblyName.HashAlgorithm);
                    Assert.Null(assemblyName.GetPublicKey());
                    Assert.Empty(assemblyName.GetPublicKeyToken());
                    Assert.Equal(AssemblyNameFlags.None, assemblyName.Flags);

                    // Validate against AssemblyDefinition
                    ValidateDefinitionAssemblyNameAgainst(assemblyName, reader, assemblyDef);

                    // Validate against AssemblyReference
                    ValidateDefinitionAssemblyNameAgainst(assemblyName, reader, assemblyRef);
                }
            }
        }

        private void ValidateDefinitionAssemblyNameAgainst(AssemblyName assemblyName, MetadataReader reader, AssemblyDefinition assemblyDef)
        {
            Assert.Equal(reader.GetString(assemblyDef.Name), assemblyName.Name);
            Assert.Equal(assemblyDef.Version, assemblyName.Version);
            Assert.Equal((Configuration.Assemblies.AssemblyHashAlgorithm)assemblyDef.HashAlgorithm, assemblyName.HashAlgorithm);
            Assert.Equal((AssemblyContentType)(((int)assemblyDef.Flags & (int)AssemblyFlags.ContentTypeMask) >> 9), assemblyName.ContentType);
        }

        private void ValidateDefinitionAssemblyNameAgainst(AssemblyName assemblyName, MetadataReader reader, AssemblyReference assemblyRef)
        {
            Assert.NotEqual(reader.GetString(assemblyRef.Name), assemblyName.Name);
        }
    }
}
