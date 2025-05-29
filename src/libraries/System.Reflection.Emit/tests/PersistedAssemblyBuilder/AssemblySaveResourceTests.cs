// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Resources;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveResourceTests
    {
        [Theory]
        [InlineData(new byte[] { 1 })]
        [InlineData(new byte[] { 1, 2 })] // Verify blob alignment padding by adding a byte.
        public void ManagedResourcesAndFieldData(byte[] byteValues)
        {
            PersistedAssemblyBuilder ab = new PersistedAssemblyBuilder(new AssemblyName("MyAssemblyWithResource"), typeof(object).Assembly);
            ab.DefineDynamicModule("MyModule");
            MetadataBuilder metadata = ab.GenerateMetadata(out BlobBuilder ilStream, out BlobBuilder fieldData);

            // We shouldn't have any field data.
            Assert.Equal(0, fieldData.Count);
            fieldData = new ();
            fieldData.WriteBytes(byteValues);

            using MemoryStream memoryStream = new MemoryStream();
            ResourceWriter myResourceWriter = new ResourceWriter(memoryStream);
            myResourceWriter.AddResource("StringResource", "Value");
            myResourceWriter.AddResource("ByteResource", byteValues);
            myResourceWriter.Close();

            byte[] data = memoryStream.ToArray();
            BlobBuilder resourceBlob = new BlobBuilder();
            resourceBlob.WriteInt32(data.Length);
            resourceBlob.WriteBytes(data);
            int resourceBlobSize = resourceBlob.Count;

            metadata.AddManifestResource(
                ManifestResourceAttributes.Public,
                metadata.GetOrAddString("MyResource.resources"),
                implementation: default,
                offset: 0);

            ManagedPEBuilder peBuilder = new ManagedPEBuilder(
                            header: PEHeaderBuilder.CreateLibraryHeader(),
                            metadataRootBuilder: new MetadataRootBuilder(metadata),
                            ilStream: ilStream,
                            mappedFieldData: fieldData,
                            managedResources: resourceBlob);

            BlobBuilder blob = new BlobBuilder();
            peBuilder.Serialize(blob);

            // Ensure the the blobs passed to Serialize() weren't modified due to alignment padding
            Assert.Equal(resourceBlobSize, resourceBlob.Count);
            Assert.Equal(byteValues.Length, fieldData.Count);

            // To verify the resources work with runtime APIs, load the assembly into the process instead of
            // the normal testing approach of using MetadataLoadContext.
            TestAssemblyLoadContext testAssemblyLoadContext = new();
            try
            {
                Assembly readAssembly = testAssemblyLoadContext.LoadFromStream(new MemoryStream(blob.ToArray()));

                // Use ResourceReader to read the resources.
                using Stream readStream = readAssembly.GetManifestResourceStream("MyResource.resources")!;
                using ResourceReader reader = new(readStream);
                Verify(reader.GetEnumerator());

                // Use ResourceManager to read the resources.
                ResourceManager rm = new ResourceManager("MyResource", readAssembly);
                ResourceSet resourceSet = rm.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false);
                Verify(resourceSet.GetEnumerator());
            }
            finally
            {
                testAssemblyLoadContext.Unload();
            }

            void Verify(IDictionaryEnumerator resources)
            {
                Assert.True(resources.MoveNext());
                DictionaryEntry resource = (DictionaryEntry)resources.Current;
                Assert.Equal("ByteResource", resource.Key);
                Assert.Equal(byteValues, resource.Value);

                Assert.True(resources.MoveNext());
                resource = (DictionaryEntry)resources.Current;
                Assert.Equal("StringResource", resource.Key);
                Assert.Equal("Value", resource.Value);

                Assert.False(resources.MoveNext());
            }
        }
    }
}

