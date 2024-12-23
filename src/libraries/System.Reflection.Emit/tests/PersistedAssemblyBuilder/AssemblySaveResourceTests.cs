// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Resources;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsNotBrowser))]
    public class AssemblySaveResourceTests
    {
        [ConditionalTheory(typeof(RemoteExecutor), nameof(RemoteExecutor.IsSupported))]
        [InlineData(new byte[] { 1 }, "01")]
        [InlineData(new byte[] { 1, 2 }, "01-02")] // Verify blob padding by adding a byte.
        public void ManagedResources(byte[] byteValue, string byteValueExpected)
        {
            PersistedAssemblyBuilder ab = new PersistedAssemblyBuilder(new AssemblyName("MyAssemblyWithResource"), typeof(object).Assembly);
            ab.DefineDynamicModule("MyModule");
            MetadataBuilder metadata = ab.GenerateMetadata(out BlobBuilder ilStream, out _);

            using MemoryStream memoryStream = new MemoryStream();
            ResourceWriter myResourceWriter = new ResourceWriter(memoryStream);
            myResourceWriter.AddResource("StringResource", "Value");
            myResourceWriter.AddResource("ByteResource", byteValue);
            myResourceWriter.Close();

            byte[] data = memoryStream.ToArray();
            BlobBuilder resourceBlob = new BlobBuilder();
            resourceBlob.WriteInt32(data.Length);
            resourceBlob.WriteBytes(data);

            metadata.AddManifestResource(
                ManifestResourceAttributes.Public,
                metadata.GetOrAddString("MyResource.resources"),
                implementation: default,
                offset: 0);

            ManagedPEBuilder peBuilder = new ManagedPEBuilder(
                            header: PEHeaderBuilder.CreateLibraryHeader(),
                            metadataRootBuilder: new MetadataRootBuilder(metadata),
                            ilStream: ilStream,
                            managedResources: resourceBlob);

            BlobBuilder blob = new BlobBuilder();
            peBuilder.Serialize(blob);

            // Create a temporary assembly.
            string tempFilePath = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".dll");
            using (FileStream fileStream = new FileStream(tempFilePath, FileMode.Create, FileAccess.Write))
            {
                blob.WriteContentTo(fileStream);
            }

            // In order to verify the resources work with ResourceManager, we need to load the assembly.
            using (RemoteInvokeHandle remoteHandle = RemoteExecutor.Invoke(static (tempFilePath, byteValue, byteValueExpected) =>
            {
                Assembly readAssembly = Assembly.LoadFile(tempFilePath);

                // Use ResourceReader to read the resources.
                using Stream readStream = readAssembly.GetManifestResourceStream("MyResource.resources")!;
                using ResourceReader reader = new(readStream);
                Verify(reader.GetEnumerator());

                // Use ResourceManager to read the resources.
                ResourceManager rm = new ResourceManager("MyResource", readAssembly);
                ResourceSet resourceSet = rm.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false);
                Verify(resourceSet.GetEnumerator());

                void Verify(IDictionaryEnumerator resources)
                {
                    Assert.True(resources.MoveNext());
                    DictionaryEntry resource = (DictionaryEntry)resources.Current;
                    Assert.Equal("ByteResource", resource.Key);
                    Assert.Equal(byteValueExpected, byteValue);

                    Assert.True(resources.MoveNext());
                    resource = (DictionaryEntry)resources.Current;
                    Assert.Equal("StringResource", resource.Key);
                    Assert.Equal("Value", resource.Value);

                    Assert.False(resources.MoveNext());
                }
            }, tempFilePath, BitConverter.ToString(byteValue), byteValueExpected)) { }

            File.Delete(tempFilePath);
        }
    }
}
