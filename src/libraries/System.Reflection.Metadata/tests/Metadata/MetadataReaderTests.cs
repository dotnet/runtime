// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Xunit;

using StreamMemoryBlockProvider = System.Reflection.Internal.StreamMemoryBlockProvider;

namespace System.Reflection.Metadata.Tests
{
    public class MetadataReaderTests
    {
        #region Helpers

        // Table Value << 24
        private enum TableConstant : uint
        {
            mdtModule = 0,
            mdtTypeRef = 0x01000000,
            mdtTypeDef = 0x02000000,
            mdtFieldDef = 0x04000000,
            mdtMethodDef = 0x06000000,
            mdtParamDef = 0x08000000,
            mdtInterfaceImpl = 0x09000000,
            mdtMemberRef = 0x0A000000,
            mdtCustomAttribute = 0x0C000000,
            mdtDeclSecurity = 0x0E000000, // Permission
            mdtStandAloneSig = 0x11000000, // Signature
            mdtEvent = 0x14000000,
            mdtProperty = 0x17000000,
            mdtModuleRef = 0x1A000000,
            mdtTypeSpec = 0x1B000000,
            mdtAssembly = 0x20000000,
            mdtAssemblyRef = 0x23000000,
            mdtFile = 0x26000000,
            mdtExportedType = 0x27000000,
            mdtManifestResource = 0x28000000,
            mdtMethodSpec = 0x2B000000,
            mdtGenericParam = 0x2A000000,
            mdtGenericParamConstraint = 0x2C000000,

            // ===========================
            mdtConstant = 0x0B000000,
            mdtFieldMarshal = 0x0D000000,
            mdtClassLayout = 0x0F000000,
            mdtFieldLayout = 0x10000000,
            mdtEventMap = 0x12000000,
            mdtPropertyMap = 0x15000000,
            mdtMethodSemantics = 0x18000000,
            mdtMethodImpl = 0x19000000,
            mdtImplMap = 0x1C000000,
            mdtFieldRVA = 0x1D000000,
            mdtAssemblyProcessor = 0x21000000,
            mdtAssemblyOS = 0x22000000,
            mdtAssemblyRefProcessor = 0x24000000,
            mdtAssemblyRefOS = 0x25000000,
            mdtNestedClass = 0x29000000,
        }

        private static readonly Dictionary<byte[], GCHandle> s_peImages = new Dictionary<byte[], GCHandle>();

        internal static unsafe MetadataReader GetMetadataReader(byte[] peImage, bool isModule = false, MetadataReaderOptions options = MetadataReaderOptions.Default, MetadataStringDecoder decoder = null)
        {
            int _;
            return GetMetadataReader(peImage, out _, isModule, options, decoder);
        }

        internal static unsafe MetadataReader GetMetadataReader(byte[] peImage, out int metadataStartOffset, bool isModule = false, MetadataReaderOptions options = MetadataReaderOptions.Default, MetadataStringDecoder decoder = null)
        {
            GCHandle pinned = GetPinnedPEImage(peImage);
            var headers = new PEHeaders(new MemoryStream(peImage));
            metadataStartOffset = headers.MetadataStartOffset;
            return new MetadataReader((byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset, headers.MetadataSize, options, decoder);
        }

        internal static unsafe GCHandle GetPinnedPEImage(byte[] peImage)
        {
            lock (s_peImages)
            {
                GCHandle pinned;
                if (!s_peImages.TryGetValue(peImage, out pinned))
                {
                    s_peImages.Add(peImage, pinned = GCHandle.Alloc(peImage, GCHandleType.Pinned));
                }

                return pinned;
            }
        }

        internal static unsafe int IndexOf(byte[] peImage, byte[] toFind, int start)
        {
            for (int i = 0; i < peImage.Length - toFind.Length; i++)
            {
                if (toFind.SequenceEqual(peImage.Slice(i + start, i + start + toFind.Length)))
                {
                    return i;
                }
            }
            return -1;
        }

        #endregion

        [Fact]
        public unsafe void InvalidSignature()
        {
            byte* ptr = stackalloc byte[4];
            Assert.Throws<BadImageFormatException>(() => new MetadataReader(ptr, 16));
        }

        [Fact]
        public unsafe void InvalidFindMscorlibAssemblyRefNoProjection()
        {
            // start with a valid PE (cloned because we'll mutate it).
            byte[] peImage = (byte[])WinRT.Lib.Clone();

            GCHandle pinned = GetPinnedPEImage(peImage);
            PEHeaders headers = new PEHeaders(new MemoryStream(peImage));

            //find index for mscorlib
            int mscorlibIndex = IndexOf(peImage, "mscorlib"u8.ToArray(), headers.MetadataStartOffset);
            Assert.NotEqual(-1, mscorlibIndex);
            //mutate mscorlib
            peImage[mscorlibIndex + headers.MetadataStartOffset] = 0xFF;

            Assert.Throws<BadImageFormatException>(() => new MetadataReader((byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset, headers.MetadataSize));
        }

        [Fact]
        public unsafe void InvalidStreamHeaderLengths()
        {
            // start with a valid PE (cloned because we'll mutate it).
            byte[] peImage = (byte[])WinRT.Lib.Clone();

            GCHandle pinned = GetPinnedPEImage(peImage);
            PEHeaders headers = new PEHeaders(new MemoryStream(peImage));

            // mutate CLR to reach MetadataKind.WindowsMetadata
            // find CLR
            int clrIndex = IndexOf(peImage, "CLR"u8.ToArray(), headers.MetadataStartOffset);
            Assert.NotEqual(-1, clrIndex);
            //find 5, This is the streamcount and is the last thing that should be read befor the test.
            int fiveIndex = IndexOf(peImage, new byte[] {5}, headers.MetadataStartOffset + clrIndex);
            Assert.NotEqual(-1, fiveIndex);

            peImage[clrIndex + headers.MetadataStartOffset] = 0xFF;

            //Not enough space for VersionString
            Assert.Throws<BadImageFormatException>(() => new MetadataReader((byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset, fiveIndex + 2, MetadataReaderOptions.Default));
            //NotEnoughSpaceForStreamHeaderName for index of five + uint16 + COR20Constants.MinimumSizeofStreamHeader
            Assert.Throws<BadImageFormatException>(() => new MetadataReader((byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset, fiveIndex + clrIndex + COR20Constants.MinimumSizeofStreamHeader + 2, MetadataReaderOptions.Default));
            //SR.StreamHeaderTooSmall
            Assert.Throws<BadImageFormatException>(() => new MetadataReader((byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset, fiveIndex + clrIndex + COR20Constants.MinimumSizeofStreamHeader , MetadataReaderOptions.Default));

        }

        [Fact]
        public unsafe void InvalidSpaceForStreams()
        {
            // start with a valid PE (cloned because we'll mutate it).
            byte[] peImage = (byte[])NetModule.AppCS.Clone();

            GCHandle pinned = GetPinnedPEImage(peImage);
            PEHeaders headers = new PEHeaders(new MemoryStream(peImage));

            //find 5, This is the streamcount we'll change to one to leave out loops.
            int fiveIndex = IndexOf(peImage, new byte[] { 5 }, headers.MetadataStartOffset);
            Assert.NotEqual(-1, fiveIndex);
            Array.Copy(BitConverter.GetBytes((ushort)1), 0, peImage, fiveIndex + headers.MetadataStartOffset, BitConverter.GetBytes((ushort)1).Length);

            string[] streamNames= new string[]
            {
                COR20Constants.StringStreamName, COR20Constants.BlobStreamName, COR20Constants.GUIDStreamName,
                COR20Constants.UserStringStreamName, COR20Constants.CompressedMetadataTableStreamName,
                COR20Constants.UncompressedMetadataTableStreamName, COR20Constants.MinimalDeltaMetadataTableStreamName,
                COR20Constants.StandalonePdbStreamName, "#invalid"
            };

            foreach (string name in streamNames)
            {
                Array.Copy(Encoding.ASCII.GetBytes(name), 0, peImage, fiveIndex + 10 + headers.MetadataStartOffset, Encoding.ASCII.GetBytes(name).Length);
                peImage[fiveIndex + 10 + headers.MetadataStartOffset + name.Length] = (byte)0;
                Assert.Throws<BadImageFormatException>(() => new MetadataReader((byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset, fiveIndex + 15 + name.Length));
            }


            Array.Copy(Encoding.ASCII.GetBytes(COR20Constants.MinimalDeltaMetadataTableStreamName), 0, peImage, fiveIndex + 10 + headers.MetadataStartOffset, Encoding.ASCII.GetBytes(COR20Constants.MinimalDeltaMetadataTableStreamName).Length);
            peImage[fiveIndex + 10 + headers.MetadataStartOffset + COR20Constants.MinimalDeltaMetadataTableStreamName.Length] = (byte)0;
            Assert.Throws<BadImageFormatException>(() => new MetadataReader((byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset, headers.MetadataSize));

        }

        [Fact]
        public unsafe void InvalidExternalTableMask()
        {
            byte[] peImage = (byte[])PortablePdbs.DocumentsPdb.Clone();
            GCHandle pinned = GetPinnedPEImage(peImage);

            //0x900001447 is the external table mask from PortablePdbs.DocumentsPdb
            int externalTableMaskIndex = IndexOf(peImage, new byte[] { 0x47, 0x14, 0, 0, 9, 0, 0, 0 }, 0);
            Assert.NotEqual(-1, externalTableMaskIndex);

            Array.Copy(new byte[] { 0x48, 0x14, 0, 0, 9, 0, 0, 0 }, 0, peImage, externalTableMaskIndex, 8);
            Assert.Throws<BadImageFormatException>(() => new MetadataReader((byte*)pinned.AddrOfPinnedObject(), peImage.Length));
        }

        [Fact]
        public unsafe void IsMinimalDelta()
        {
            byte[] peImage = (byte[])PortablePdbs.DocumentsPdb.Clone();
            GCHandle pinned = GetPinnedPEImage(peImage);
            //Find COR20Constants.StringStreamName to be changed to COR20Constants.MinimalDeltaMetadataTableStreamName
            int stringIndex = IndexOf(peImage, Encoding.ASCII.GetBytes(COR20Constants.StringStreamName), 0);
            Assert.NotEqual(-1, stringIndex);
            //find remainingBytes to be increased because we are changing to uncompressed
            int remainingBytesIndex = IndexOf(peImage, BitConverter.GetBytes(180), 0);
            Assert.NotEqual(-1, remainingBytesIndex);
            //find compressed to change to uncompressed
            int compressedIndex = IndexOf(peImage, Encoding.ASCII.GetBytes(COR20Constants.CompressedMetadataTableStreamName), 0);
            Assert.NotEqual(-1, compressedIndex);

            Array.Copy(Encoding.ASCII.GetBytes(COR20Constants.MinimalDeltaMetadataTableStreamName), 0, peImage, stringIndex, Encoding.ASCII.GetBytes(COR20Constants.MinimalDeltaMetadataTableStreamName).Length);
            peImage[stringIndex + COR20Constants.MinimalDeltaMetadataTableStreamName.Length] = (byte)0;
            Array.Copy(BitConverter.GetBytes(250), 0, peImage, remainingBytesIndex, BitConverter.GetBytes(250).Length);
            Array.Copy(Encoding.ASCII.GetBytes(COR20Constants.UncompressedMetadataTableStreamName), 0, peImage, compressedIndex, Encoding.ASCII.GetBytes(COR20Constants.UncompressedMetadataTableStreamName).Length);

            MetadataReader minimalDeltaReader = new MetadataReader((byte*)pinned.AddrOfPinnedObject(), peImage.Length);
            Assert.True(minimalDeltaReader.IsMinimalDelta);
        }


        [Fact]
        public unsafe void InvalidMetaDataTableHeaders()
        {
            // start with a valid PE (cloned because we'll mutate it).
            byte[] peImage = (byte[])NetModule.AppCS.Clone();

            GCHandle pinned = GetPinnedPEImage(peImage);
            PEHeaders headers = new PEHeaders(new MemoryStream(peImage));

            //0x0570 is the remaining bytes from NetModule.AppCS
            int remainingBytesIndex = IndexOf(peImage, new byte[] { 0x70, 0x05, 0, 0 }, headers.MetadataStartOffset);
            Assert.NotEqual(-1, remainingBytesIndex);
            //0xcc90da21757 is the presentTables from NetModule.AppCS, must be after remainingBytesIndex
            int presentTablesIndex = IndexOf(peImage, new byte[] { 0x57, 0x17, 0xa2, 0x0d, 0xc9, 0x0c, 0, 0 }, headers.MetadataStartOffset + remainingBytesIndex);
            Assert.NotEqual(-1, presentTablesIndex);

            //Set this.ModuleTable.NumberOfRows to 0
            Array.Copy(new byte[] { 0, 0, 0, 0, 0, 0, 0, 0 }, 0, peImage, presentTablesIndex + remainingBytesIndex + headers.MetadataStartOffset + 16, 8);
            Assert.Throws<BadImageFormatException>(() => new MetadataReader((byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset, headers.MetadataSize));
            //set row counts greater than TokenTypeIds.RIDMask
            Array.Copy(new byte[] { 0, 0, 1, 0 }, 0, peImage, presentTablesIndex + remainingBytesIndex + headers.MetadataStartOffset + 16, 4);
            Assert.Throws<BadImageFormatException>(() => new MetadataReader((byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset, headers.MetadataSize));
            //set remaining bytes smaller than required for row counts.
            Array.Copy(new byte[] { 25, 0, 0, 0 }, 0, peImage, remainingBytesIndex + headers.MetadataStartOffset, 4);
            Assert.Throws<BadImageFormatException>(() => new MetadataReader((byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset, headers.MetadataSize));
            //0xcc90da21758 is a value to make (presentTables & ~validTables) != 0 but not (presentTables & (ulong)(TableMask.PtrTables | TableMask.EnCMap)) != 0
            Array.Copy(new byte[] { 0x58, 0x17, 0xa2, 0x0d, 0xc9, 0x0c, 0, 0 }, 0, peImage, presentTablesIndex + remainingBytesIndex + headers.MetadataStartOffset, 8);
            Assert.Throws<BadImageFormatException>(() => new MetadataReader((byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset, headers.MetadataSize));
            //0xccb0da21757 makes (presentTables & ~validTables) != 0 fail
            Array.Copy(new byte[] { 0x57, 0x17, 0xa2, 0x0d, 0xcb, 0x0c, 0, 0 }, 0, peImage, presentTablesIndex + remainingBytesIndex + headers.MetadataStartOffset, 8);
            Assert.Throws<BadImageFormatException>(() => new MetadataReader((byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset, headers.MetadataSize));
            //set remaining bytes smaller than MetadataStreamConstants.SizeOfMetadataTableHeader
            Array.Copy(new byte[] { 1, 0, 0, 0 }, 0, peImage, remainingBytesIndex + headers.MetadataStartOffset, 4);
            Assert.Throws<BadImageFormatException>(() => new MetadataReader((byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset, headers.MetadataSize));
        }

        [Fact]
        public unsafe void EmptyMetadata()
        {
            byte* ptr = stackalloc byte[1];

            Assert.Throws<ArgumentNullException>(() => new MetadataReader(null, 10));
            Assert.Throws<ArgumentOutOfRangeException>(() => new MetadataReader(ptr, -10));
            Assert.Throws<BadImageFormatException>(() => new MetadataReader(ptr, 0));
        }

        [Fact]
        public void StreamLengths()
        {
            var reader = GetMetadataReader(NetModule.AppCS);
            Assert.Equal(0x038, reader.GetHeapSize(HeapIndex.UserString));
            Assert.Equal(0x3c2, reader.GetHeapSize(HeapIndex.String));
            Assert.Equal(0x1cc, reader.GetHeapSize(HeapIndex.Blob));
            Assert.Equal(0x010, reader.GetHeapSize(HeapIndex.Guid));
        }

        [Fact]
        public unsafe void PointerAndLength()
        {
            GCHandle pinned = GetPinnedPEImage(NetModule.AppCS);
            var headers = new PEHeaders(new MemoryStream(NetModule.AppCS));
            byte* ptr = (byte*)pinned.AddrOfPinnedObject() + headers.MetadataStartOffset;
            var reader = new MetadataReader(ptr, headers.MetadataSize);

            Assert.True(ptr == reader.MetadataPointer);
            Assert.Equal(headers.MetadataSize, reader.MetadataLength);
        }

        [Fact]
        public void CannotInstantiateReaderWithNonUtf8Decoder()
        {
            var decoder = new MetadataStringDecoder(Encoding.ASCII);
            AssertExtensions.Throws<ArgumentException>("utf8Decoder", () => GetMetadataReader(Misc.Members, decoder: decoder));
        }

        [Fact]
        public void CanCustomizeReaderUtf8Fallback()
        {
            // start with a valid PE (cloned because we'll mutate it).
            byte[] peImage = (byte[])Namespace.NamespaceTests.Clone();

            // find a System string in its string heap.
            int metadataStartOffset;
            var reader = GetMetadataReader(peImage, out metadataStartOffset);

            List<string> strings = new List<string>();
            StringHandle handle;
            for (handle = MetadataTokens.StringHandle(1); !handle.IsNil; handle = reader.GetNextHandle(handle))
                if (reader.StringComparer.Equals(handle, "NSTests.WithNestedType"))
                    break;

            Assert.Equal("NSTests.WithNestedType", reader.GetString(handle));
            Assert.True(reader.StringComparer.Equals(handle.WithWinRTPrefix(), "<WinRT>NSTests.WithNestedType"));
            Assert.True(reader.StringComparer.StartsWith(handle.WithWinRTPrefix(), "<WinRT>N"));

            // now let's see how well the API we added to make small patches to binaries works
            // and use it to corrupt the NSTests.WithNestedType entry in the string heap.

            int stringHeapOffset = reader.GetHeapMetadataOffset(HeapIndex.String);
            int stringHandleOffset = reader.GetHeapOffset(handle);
            int totalPEOffset = metadataStartOffset + stringHeapOffset + stringHandleOffset;

            Assert.Equal((byte)'N', peImage[totalPEOffset]);
            peImage[metadataStartOffset + stringHeapOffset + stringHandleOffset] = 0xC0;

            // metadata reader is over pinned byte[]. Our changes are live. :)
            Assert.Equal("\uFFFDSTests.WithNestedType", reader.GetString(handle)); // default fallback
            Assert.Equal("<WinRT>\uFFFDSTests.WithNestedType", reader.GetString(handle.WithWinRTPrefix()));
            Assert.Equal("\uFFFDSTests", reader.GetString(handle.WithDotTermination()));

            Assert.True(reader.StringComparer.Equals(handle, "\uFFFDSTests.WithNestedType"));
            Assert.True(reader.StringComparer.Equals(handle.WithDotTermination(), "\uFFFDSTests"));

            // This one calls the decoder already because we don't bother optimizing uncommon winrt prefix case.
            Assert.True(reader.StringComparer.StartsWith(handle.WithWinRTPrefix(), "<WinRT>\uFFFDS"));
            Assert.True(reader.StringComparer.Equals(handle.WithWinRTPrefix(), "<WinRT>\uFFFDSTests.WithNestedType"));
            ///////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // recreate our reader with exception fallback decoder.
            reader = GetMetadataReader(
                peImage,
                decoder: new MetadataStringDecoder(
                            Encoding.GetEncoding(
                                "utf-8",
                                EncoderFallback.ExceptionFallback,
                                DecoderFallback.ExceptionFallback)));

            // since we're reading the same PE image our handles are still valid.
            Assert.Throws<DecoderFallbackException>(() => reader.GetString(handle)); // BOOM!
        }

        [Fact]
        public void GetToken_Projected()
        {
            var reader = GetMetadataReader(WinRT.Lib, options: MetadataReaderOptions.ApplyWindowsRuntimeProjections);
            int expectedToken = 0x23000001;
            foreach (var assemblyRefHandle in reader.AssemblyReferences)
            {
                Assert.Equal(expectedToken >= 0x23000004, assemblyRefHandle.IsVirtual);
                Assert.Equal(expectedToken, reader.GetToken(assemblyRefHandle));
                Assert.Equal(expectedToken, reader.GetToken((Handle)assemblyRefHandle));
                expectedToken++;
            }

            Assert.Equal(9, reader.AssemblyReferences.Count);
        }

        [Fact]
        public void GetToken_NotProjected()
        {
            var reader = GetMetadataReader(WinRT.Lib, options: MetadataReaderOptions.None);
            var expectedToken = 0x23000001;
            foreach (var assemblyRefHandle in reader.AssemblyReferences)
            {
                Assert.False(assemblyRefHandle.IsVirtual);
                Assert.Equal(expectedToken, reader.GetToken(assemblyRefHandle));
                Assert.Equal(expectedToken, reader.GetToken((Handle)assemblyRefHandle));
                expectedToken++;
            }

            Assert.Equal(3, reader.AssemblyReferences.Count);
        }

        [Fact]
        public void GetBlobReader_VirtualBlob()
        {
            var reader = GetMetadataReader(WinRT.Lib, options: MetadataReaderOptions.ApplyWindowsRuntimeProjections);
            var handle = reader.AssemblyReferences.Skip(3).First();
            Assert.True(handle.IsVirtual);

            var assemblyRef = reader.GetAssemblyReference(handle);
            Assert.Equal("System.Runtime", reader.GetString(assemblyRef.Name));

            AssertEx.Equal(
                new byte[] { 0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A },
                reader.GetBlobBytes(assemblyRef.PublicKeyOrToken));

            var blobReader = reader.GetBlobReader(assemblyRef.PublicKeyOrToken);
            Assert.Equal(new byte[] { 0xB0, 0x3F, 0x5F, 0x7F, 0x11, 0xD5, 0x0A, 0x3A }, blobReader.ReadBytes(8));
            Assert.Equal(0, blobReader.RemainingBytes);
        }

        [Fact]
        public void GetString_WinRTPrefixed_Projected()
        {
            var reader = GetMetadataReader(WinRT.Lib, options: MetadataReaderOptions.ApplyWindowsRuntimeProjections);

            // .class /*02000002*/ public auto ansi sealed beforefieldinit Lib.Class1
            var winrtDefHandle = MetadataTokens.TypeDefinitionHandle(2);
            var winrtDef = reader.GetTypeDefinition(winrtDefHandle);
            Assert.Equal(StringKind.Plain, winrtDef.Name.StringKind);
            Assert.Equal("Class1", reader.GetString(winrtDef.Name));
            Assert.Equal(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass |
                TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                winrtDef.Attributes);

            var strReader = reader.GetBlobReader(winrtDef.Name);
            Assert.Equal("Class1"u8.ToArray(), strReader.ReadBytes("Class1".Length));
            Assert.Equal(0, strReader.RemainingBytes);

            // .class /*02000003*/ private auto ansi import windowsruntime sealed beforefieldinit Lib.'<WinRT>Class1'
            var clrDefHandle = MetadataTokens.TypeDefinitionHandle(3);
            var clrDef = reader.GetTypeDefinition(clrDefHandle);
            Assert.Equal(StringKind.WinRTPrefixed, clrDef.Name.StringKind);
            Assert.Equal("<WinRT>Class1", reader.GetString(clrDef.Name));
            Assert.Equal(
                TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass |
                TypeAttributes.Import | TypeAttributes.WindowsRuntime | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                clrDef.Attributes);

            strReader = reader.GetBlobReader(clrDef.Name);
            Assert.Equal("<WinRT>Class1"u8.ToArray(), strReader.ReadBytes("<WinRT>Class1".Length));
            Assert.Equal(0, strReader.RemainingBytes);
        }

        [Fact]
        public void GetString_WinRTPrefixed_NotProjected()
        {
            var reader = GetMetadataReader(WinRT.Lib, options: MetadataReaderOptions.None);

            // .class /*02000002*/ private auto ansi sealed beforefieldinit specialname Lib.'<CLR>Class1'
            var winrtDefHandle = MetadataTokens.TypeDefinitionHandle(2);
            var winrtDef = reader.GetTypeDefinition(winrtDefHandle);
            Assert.Equal(StringKind.Plain, winrtDef.Name.StringKind);
            Assert.Equal("<CLR>Class1", reader.GetString(winrtDef.Name));
            Assert.Equal(
                TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass |
                TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit | TypeAttributes.SpecialName,
                winrtDef.Attributes);

            var strReader = reader.GetBlobReader(winrtDef.Name);
            Assert.Equal("<CLR>Class1"u8.ToArray(), strReader.ReadBytes("<CLR>Class1".Length));
            Assert.Equal(0, strReader.RemainingBytes);

            // .class /*02000003*/ public auto ansi windowsruntime sealed beforefieldinit Lib.Class1
            var clrDefHandle = MetadataTokens.TypeDefinitionHandle(3);
            var clrDef = reader.GetTypeDefinition(clrDefHandle);
            Assert.Equal(StringKind.Plain, clrDef.Name.StringKind);
            Assert.Equal("Class1", reader.GetString(clrDef.Name));
            Assert.Equal(
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.AutoLayout | TypeAttributes.AnsiClass |
                TypeAttributes.WindowsRuntime | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                clrDef.Attributes);

            strReader = reader.GetBlobReader(clrDef.Name);
            Assert.Equal("Class1"u8.ToArray(), strReader.ReadBytes("Class1".Length));
            Assert.Equal(0, strReader.RemainingBytes);
        }

        [Fact]
        public void GetString_DotTerminated()
        {
            var reader = GetMetadataReader(WinRT.Lib, options: MetadataReaderOptions.None);

            //  4: 0x23000001 (AssemblyRef)  'CompilationRelaxationsAttribute' (#1c3)  'System.Runtime.CompilerServices' (#31a)
            var typeRef = reader.GetTypeReference(MetadataTokens.TypeReferenceHandle(4));
            Assert.Equal("System.Runtime.CompilerServices", reader.GetString(typeRef.Namespace));

            var strReader = reader.GetBlobReader(typeRef.Namespace);
            Assert.Equal("System.Runtime.CompilerServices"u8.ToArray(), strReader.ReadBytes("System.Runtime.CompilerServices".Length));
            Assert.Equal(0, strReader.RemainingBytes);

            var dotTerminated = typeRef.Namespace.WithDotTermination();
            Assert.Equal("System", reader.GetString(dotTerminated));

            strReader = reader.GetBlobReader(dotTerminated);
            Assert.Equal("System"u8.ToArray(), strReader.ReadBytes("System".Length));
            Assert.Equal(0, strReader.RemainingBytes);
        }

        /// <summary>
        /// Assembly Table Columns:
        ///     Name (offset to #String)
        ///     Flags (4 byte uint) - none, noappdomain, noprocess, nomachine
        ///     MajorVersion, MinorVersion, BuildNumber, RevisionNumber (2 byte unsigned)
        ///     PublicKey (offset to #blob)
        ///     Locale (offset to #String)
        ///     HashAlgId (4 byte unsigned)
        /// </summary>
        [Fact]
        public void ValidateAssemblyTableExe()
        {
            var reader = GetMetadataReader(NetModule.AppCS);

            // only one
            Assert.Equal(1, reader.AssemblyTable.NumberOfRows);

            // 1 based
            AssemblyDefinition row = reader.GetAssemblyDefinition();
            Assert.Equal("AppCS", reader.GetString(row.Name));

            // Flags
            Assert.Equal((uint)0, (uint)row.Flags); // AssemblyFlags

            // Locale
            Assert.True(row.Culture.IsNil);

            // PublicKey
            Assert.True(row.PublicKey.IsNil);

            // Version
            Assert.Equal(new Version(1, 2, 3, 4), row.Version);

            // 32772
            Assert.Equal(AssemblyHashAlgorithm.Sha1, row.HashAlgorithm);
        }

        /// <summary>
        /// AssemblyRef Table Columns:
        ///     Name (offset to #String)
        ///     Flags (4 byte uint) - none, noappdomain, noprocess, nomachine
        ///     MajorVersion, MinorVersion, BuildNumber, RevisionNumber (2 byte ushort)
        ///     PublicKeyOrToken (offset to #blob)
        ///     Locale (offset to #String)
        ///     HashValue (offset to #blob)
        /// </summary>
        [Fact]
        public void ValidateAssemblyRefTable()
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

            var reader = GetMetadataReader(NetModule.AppCS);

            int i = 0;
            foreach (var assemblyRef in reader.AssemblyReferences)
            {
                var row = reader.GetAssemblyReference(assemblyRef);
                Assert.Equal(expRefs[i], reader.GetString(row.Name));
                Assert.Equal((uint)0, (uint)row.Flags);
                Assert.True(row.Culture.IsNil);

                var pubKey = reader.GetBlobBytes(row.PublicKeyOrToken);
                Assert.Equal(pubKey.Length, expKeys[i].Length);
                for (int j = 0; j < pubKey.Length; j++)
                {
                    Assert.Equal(pubKey[j], expKeys[i][j]);
                }

                // Data Validation
                Assert.Equal(expVers[i], row.Version);

                Assert.True(row.HashValue.IsNil);

                i++;
            }

            Assert.Equal(expRefs.Length, i);
        }

        /// <summary>
        /// Module Table Columns:
        ///     Name (offset to #String)
        ///     Mvid (offset to #Guid)
        ///     Generation (2-byte ushort) EnC
        ///     EncId, EncBaseId (offset to #Guid)
        /// </summary>
        [Fact]
        public void ValidateModuleTable()
        {
            var reader = GetMetadataReader(NetModule.AppCS);

            ModuleDefinition moduleDef = reader.GetModuleDefinition();

            // Validity Rules
            Assert.Equal("AppCS.exe", reader.GetString(moduleDef.Name));

            // Data Validation
            Assert.Equal(@"F130D514-7D4E-4B7E-9767-58808BC06A7E", reader.GetGuid(moduleDef.Mvid).ToString().ToUpperInvariant());
        }

        [Fact]
        public void ValidateModuleTableMod()
        {
            var reader = GetMetadataReader(NetModule.ModuleVB01, true);
            ModuleDefinition moduleDef = reader.GetModuleDefinition();

            // Validity Rules
            Assert.Equal("ModuleVB01.mod", reader.GetString(moduleDef.Name));

            // Data Validation
            Assert.Equal(@"A7C4B488-9378-4750-801B-9E78BCF98995", reader.GetGuid(moduleDef.Mvid).ToString().ToUpperInvariant());
        }

        /// <summary>
        /// ModuleRef Table Columns:
        ///     Name (offset to #String)
        /// -----------------------------
        /// File Table Columns:
        ///     Name (offset to #String)
        ///     Flags (4 byte uint)
        ///     HashValue (offset to #blob)
        /// </summary>
        [Fact]
        public void ValidateModuleRefTableAndFileTableManaged()
        {
            // AppCS has 2 modules
            var expMods = new string[] { "ModuleCS01.mod", "ModuleVB01.mod" };
            var expHashes = new byte[][]
            {
                // ModuleCS01.mod - 2B 56 10 8B 34 A1 DC CD CC B5 CF 66 5E 43 94 5E 09 9F 34 A3
                new byte[] { 0x2B, 0x56, 0x10, 0x8B, 0x34, 0xA1, 0xDC, 0xCD, 0xCC, 0xB5, 0xCF, 0x66, 0x5E, 0x43, 0x94, 0x5E, 0x09, 0x9F, 0x34, 0xA3 },

                // ModuleVB01.mod - A7 F0 25 28 0F 3C 29 2E 83 90 F0 FA A7 13 8E E4 54 16 D7 A0
                new byte[] { 0xA7, 0xF0, 0x25, 0x28, 0x0F, 0x3C, 0x29, 0x2E, 0x83, 0x90, 0xF0, 0xFA, 0xA7, 0x13, 0x8E, 0xE4, 0x54, 0x16, 0xD7, 0xA0 }
            };

            var reader = GetMetadataReader(NetModule.AppCS);

            Assert.Equal(expMods.Length, reader.GetTableRowCount(TableIndex.ModuleRef));
            int m = 0;
            foreach (var moduleRefName in reader.GetReferencedModuleNames())
            {
                Assert.Equal(expMods[m], reader.GetString(moduleRefName));
                m++;
            }

            // -- FileTable --

            // # of mod == # of file
            Assert.Equal(reader.GetTableRowCount(TableIndex.ModuleRef), reader.GetTableRowCount(TableIndex.File));
            Assert.Equal(expMods.Length, reader.GetTableRowCount(TableIndex.File));

            int i = 0;
            foreach (var fileHandle in reader.AssemblyFiles)
            {
                var file = reader.GetAssemblyFile(fileHandle);

                Assert.Equal(expMods[i], reader.GetString(file.Name));
                Assert.True(file.ContainsMetadata);

                var hv = reader.GetBlobBytes(file.HashValue);
                Assert.Equal(hv.Length, expHashes[i].Length);
                for (int j = 0; j < hv.Length; j++)
                {
                    Assert.Equal(hv[j], expHashes[i][j]);
                }

                i++;
            }
        }

        /// <summary>
        /// ModuleRef Table Columns:
        ///     Name (offset to #String)
        /// -----------------------------
        /// File Table Columns:
        ///     Name (offset to #String)
        ///     Flags (4 byte uint)
        ///     HashValue (offset to #blob)
        /// </summary>
        [Fact]
        public void ValidateModuleRefTableMod()
        {
            // AppCS has 2 modules
            var expMods = new string[] { "ModuleCS00.mod" };
            var expHashes = new byte[][]
            {
                // ModuleCS00.mod
                // new byte [] { 0xd4, 0x6b, 0xec, 0x25, 0x47, 0x01, 0x20, 0x30, 0x05, 0x42, 0x34, 0x4b, 0x31, 0x22, 0x44, 0xd8, 0x1c, 0x87, 0xd0, 0x98 },
            };

            // ModuleVB01
            var reader = GetMetadataReader(NetModule.ModuleVB01, true);

            Assert.Equal(expMods.Length, reader.ModuleRefTable.NumberOfRows);
            int m = 0;
            foreach (var moduleRefName in reader.GetReferencedModuleNames())
            {
                Assert.Equal(expMods[m], reader.GetString(moduleRefName));
                m++;
            }

            // ==================================================
            // ModuleCS01
            reader = GetMetadataReader(NetModule.ModuleCS01, true);

            Assert.Equal(expMods.Length, reader.ModuleRefTable.NumberOfRows);
            m = 0;
            foreach (var moduleRefName in reader.GetReferencedModuleNames())
            {
                Assert.Equal(expMods[m], reader.GetString(moduleRefName));
            }
        }

        /// <summary>
        /// ExportType Table Columns:
        ///     TypeName, TypeNamespace (offset to #String)
        ///     Flags (4 byte uint)
        ///     TypeDefId (4 byte uint)
        ///     Implementation (coded token)
        /// </summary>
        [Fact]
        public void ValidateExportedTypeTable()
        {
            // 14
            var expTypes = new string[]
            {
                "ModChainA", "ModChainB", "ModChainC", "Extension", "GenDele`1",
                "ModIGen2`2", "ModClassImplImp`1", "ModStructImplExp", "ModVBClass", "ModVBStruct",
                "ModVBInnerEnum", "ModVBInnerStruct", "ModVBDele", "ModVBInnerIFoo",
            };

            var expNamespaces = new string[]
            {
                "NS.Module.CS01", "NS.Module.CS01", "NS.Module.CS01", "NS.Module.CS01", "NS.Module.CS01",
                "NS.Module.CS01.CS02", "NS.Module.CS01.CS02", "NS.Module.CS01.CS02", string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, string.Empty,
            };

            var expFlags = new int[]
            {
                0x00100081, 0x00100001, 0x00100101, 0x00100181, 0x00000101,
                0x000000a1, 0x00100001, 0x00100109, 0x00000001, 0x00100109,
                0x00000102, 0x0000010a, 0x00000102, 0x000000a2
            };

            var expTDefTokens = new int[]
            {
                0x02000002, 0x02000003, 0x02000004, 0x02000005, 0x02000006, 0x02000007, 0x02000008,
                0x02000009, 0x02000002, 0x02000003, 0x02000004, 0x02000005, 0x02000006, 0x02000007
            };

            var expImplTokens = new int[]
            {
                0x26000001, 0x26000001, 0x26000001, 0x26000001, 0x26000001, 0x26000001, 0x26000001,
                0x26000001, 0x26000002, 0x26000002, 0x27000009, 0x2700000a, 0x2700000a, 0x2700000c
            };

            var reader = GetMetadataReader(NetModule.AppCS);

            Assert.Equal(expTypes.Length, reader.ExportedTypeTable.NumberOfRows);
            for (int i = 0; i < reader.ExportedTypeTable.NumberOfRows; i++)
            {
                int rid = i + 1;
                Assert.Equal(expTypes[i], reader.GetString(reader.ExportedTypeTable.GetTypeName(rid)));
                Assert.Equal(expNamespaces[i], reader.GetString(reader.ExportedTypeTable.GetTypeNamespace(rid)));
                Assert.Equal(expFlags[i], (int)reader.ExportedTypeTable.GetFlags(rid));
                Assert.Equal(expTDefTokens[i], reader.ExportedTypeTable.GetTypeDefId(rid));
                Assert.Equal(expImplTokens[i], reader.ExportedTypeTable.GetImplementation(rid).Token);
            }
        }

        /// <summary>
        /// TypeRef Table Columns:
        ///     Name, Namespace (offset to #String)
        ///     ResolutionScope (token to Assembly)
        /// </summary>
        [Fact]
        public void ValidateTypeRefTable()
        {
            // 24
            var expNames = new string[]
            {
                "RuntimeCompatibilityAttribute", "ExtensionAttribute", "Object", "ModChainB", "ModIGen2`2",
                "Expression", "ModChainA", "ModClassImplImp`1", "ModVBClass", "ModVBInnerEnum",
                "ModVBStruct", "ModVBInnerStruct", "ModVBInnerIFoo", "AssemblyTitleAttribute", "AssemblyVersionAttribute",
                "AssemblyCultureAttribute", "CompilationRelaxationsAttribute", "ModStructImplExp", "EventLog", "EventArgs",
                "Extension", "DefaultMemberAttribute", "Action`1", "Activator",
            };
            var expNamespaces = new string[]
            {
                "System.Runtime.CompilerServices", "System.Runtime.CompilerServices", "System", "NS.Module.CS01", "NS.Module.CS01.CS02",
                "System.Linq.Expressions", "NS.Module.CS01", "NS.Module.CS01.CS02", string.Empty, string.Empty,
                string.Empty, string.Empty, string.Empty, "System.Reflection", "System.Reflection",
                "System.Reflection", "System.Runtime.CompilerServices", "NS.Module.CS01.CS02", "System.Diagnostics", "System",
                "NS.Module.CS01", "System.Reflection", "System", "System",
            };
            var expAsmTokens = new int[]
            {
                0x23000001, 0x23000002, 0x23000001, 0x1a000001, 0x1a000001,
                0x23000002, 0x1a000001, 0x1a000001, 0x1a000002, 0x01000009,
                0x1a000002, 0x0100000b, 0x0100000c, 0x23000001, 0x23000001,
                0x23000001, 0x23000001, 0x1a000001, 0x23000003, 0x23000001,
                0x1a000001, 0x23000001, 0x23000001, 0x23000001
            };

            var reader = GetMetadataReader(NetModule.AppCS);

            Assert.Equal(expNames.Length, reader.TypeReferences.Count);
            int i = 0;
            foreach (var typeRef in reader.TypeReferences)
            {
                TypeReference row = reader.GetTypeReference(typeRef);

                // var n = reader.GetString(row.Name);
                // var ns = reader.GetString(row.Namespace);
                Assert.Equal(expNames[i], reader.GetString(row.Name));
                Assert.Equal(expNamespaces[i], reader.GetString(row.Namespace));
                Assert.Equal(expAsmTokens[i], row.ResolutionScope.Token);

                i++;
            }
        }

        /// <summary>
        /// TypeRef Table Columns:
        ///     Name, Namespace (offset to #String)
        ///     ResolutionScope (token to Assembly)
        /// </summary>
        [Fact]
        public void ValidateTypeRefTableMod()
        {
            // ModuleCS01 - 0x16
            var expNames = new string[]
            {
                "Object", "MulticastDelegate", "Action`1", "ValueType", "Expression",
                "EventLog", "EventArgs", "ModStruct", "ModIDerive", "IsVolatile",
                "IAsyncResult", "AsyncCallback", "Func`3", "OutAttribute", "RuntimeCompatibilityAttribute",
                "AssemblyAttributesGoHere", "ExtensionAttribute", "CompilerGeneratedAttribute",
                "DefaultMemberAttribute", "Delegate", "StructLayoutAttribute", "LayoutKind",
            };
            var expNamespaces = new string[]
            {
                "System", "System", "System", "System", "System.Linq.Expressions",
                "System.Diagnostics", "System", "NS.Module", "NS.Module", "System.Runtime.CompilerServices",
                "System", "System", "System", "System.Runtime.InteropServices", "System.Runtime.CompilerServices",
                "System.Runtime.CompilerServices", "System.Runtime.CompilerServices",  "System.Runtime.CompilerServices", "System.Reflection", "System",
                "System.Runtime.InteropServices", "System.Runtime.InteropServices",
            };
            var expAsmTokens = new int[]
            {
                0x23000001, 0x23000001, 0x23000001, 0x23000001, 0x23000002,
                0x23000003, 0x23000001, 0x1a000001, 0x1a000001, 0x23000001,
                0x23000001, 0x23000001, 0x23000001, 0x23000001, 0x23000001,
                0x23000001, 0x23000002, 0x23000001, 0x23000001, 0x23000001, 0x23000001, 0x23000001,
            };

            var reader = GetMetadataReader(NetModule.ModuleCS01, true);
            Assert.Equal(expNames.Length, reader.TypeReferences.Count);

            int i = 0;
            foreach (var typeRef in reader.TypeReferences)
            {
                TypeReference row = reader.GetTypeReference(typeRef);
                Assert.Equal(expNames[i], reader.GetString(row.Name));
                Assert.Equal(expNamespaces[i], reader.GetString(row.Namespace));
                Assert.Equal(expAsmTokens[i], row.ResolutionScope.Token);

                i++;
            }
        }

        /// <summary>
        /// TypeDef Table Columns:
        ///     Name, Namespace (offset to #String)
        ///     Flags (4 byte unsigned)
        ///     Extends (token)
        ///     FieldList, MethodList (RID)
        /// </summary>
        /// <TODO> flag 0x00040000 set -> DeclSecurity record or SuppressUnmanagedCodeSecurityAttribute
        /// </TODO>
        [Fact]
        public void ValidateTypeDefTable()
        {
            var expNames = new string[]
            {
                "<Module>", "App", "UseModule", "IContraVar`1", "ICoVar`1", "INormal`1",
                "ContraInClass`1", "CoOutClass`1", "NormalClass`1", "Animal", "Tiger", "Test"
            };
            var expNamespaces = new string[]
            {
                string.Empty, "AppCS", "AppCS", "AppCS", "AppCS", "AppCS",
                "AppCS", "AppCS", "AppCS", "AppCS", "AppCS", "AppCS",
            };
            var expFlags = new uint[]
            {
                0, 0x00100001, 0x00100001, 0x000000a1, 0x000000a1, 0x000000a0,
                0x00100000, 0x00100000, 0x00100000, 0x00100000, 0x00100000, 0x00100001
            };
            var expExtends = new uint[]
            {
                0x02000000, 0x01000003, 0x01000003, 0x02000000, 0x02000000, 0x02000000,
                0x01000003, 0x01000003, 0x01000003, 0x01000003, 0x0200000a, 0x01000003,
            };
            var expNest = new bool[] { false, false, false, false, false, false, false, false, false, false, false, false };

            var reader = GetMetadataReader(NetModule.AppCS);
            Assert.Equal(expNames.Length, reader.TypeDefinitions.Count);

            uint prevFieldStart = 0;
            uint prevMethodStart = 0;

            int i = 0;
            foreach (var typeDefHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeDefHandle);
                uint fieldStart = (uint)reader.TypeDefTable.GetFieldStart(typeDefHandle.RowId);
                uint methodStart = (uint)reader.TypeDefTable.GetMethodStart(typeDefHandle.RowId);

                Assert.Equal(expNames[i], reader.GetString(typeDef.Name));
                Assert.Equal(expNamespaces[i], reader.GetString(typeDef.Namespace));

                // Assert.Equal((TypeDefFlags)expFlags[i], (TypeDefFlags)row.Flags);
                Assert.Equal(expFlags[i], (uint)typeDef.Attributes);
                Assert.Equal(expExtends[i], (uint)typeDef.BaseType.Token);
                Assert.Equal(expNest[i], typeDef.Attributes.IsNested());

                // validate previous row's member as it needs current row's member other to calc how many
                if (i > 0)
                {
                    ValidateFieldDef(reader, prevFieldStart, fieldStart - prevFieldStart);
                    ValidateMethodDef(reader, prevMethodStart, methodStart - prevMethodStart);
                    // ValidatePropertyByType(i + 1, expMemberCount[i * 4 + 2]);
                    // ValidateEventByType(i + 1, expMemberCount[i * 4 + 3]);
                }

                // Last
                if (i == reader.TypeDefinitions.Count - 1)
                {
                    ValidateFieldDef(reader, prevFieldStart, 0xF0000000);
                    ValidateMethodDef(reader, prevMethodStart, 0xF0000000);
                    // ValidatePropertyByType(i + 1, expMemberCount[i * 4 + 2]);
                    // ValidateEventByType(i + 1, expMemberCount[i * 4 + 3]);
                }

                prevFieldStart = fieldStart;
                prevMethodStart = methodStart;
                i++;
            }
        }

        /// <summary>
        /// TypeDef Table Columns:
        ///     Name, Namespace (offset to #String)
        ///     Flags (4 byte unsigned)
        ///     Extends (token)
        ///     FieldList, MethodList (RID)
        /// </summary>
        /// <TODO> flag 0x00040000 set -> DeclSecurity record or SuppressUnmanagedCodeSecurityAttribute
        /// </TODO>
        [Fact]
        public void ValidateTypeDefTableMod()
        {
            // 7
            var expNames = new string[]
            {
                "<Module>", "ModVBClass", "ModVBStruct", "ModVBInnerEnum", "ModVBInnerStruct", "ModVBDele", "ModVBInnerIFoo",
            };
            var expNamespaces = new string[]
            {
                string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
            };
            var expFlags = new TypeAttributes[]
            {
                /* 0 */
                        TypeAttributes.NotPublic, /* 1 */ TypeAttributes.Public,
                /* 0x00100109 */
                                 TypeAttributes.BeforeFieldInit | TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.Public,
                /* 0x0102 */
                             TypeAttributes.Sealed | TypeAttributes.NestedPublic,
                /* 0x010a */
                             TypeAttributes.Sealed | TypeAttributes.SequentialLayout | TypeAttributes.NestedPublic,
                /* 0x0102 */
                             TypeAttributes.Sealed | TypeAttributes.NestedPublic,
                /* 0x00a2 */
                             TypeAttributes.Abstract | TypeAttributes.Interface | TypeAttributes.NestedPublic,
            };
            var expExtends = new uint[] { 0x02000000, 0x01000001, 0x01000007, 0x01000009, 0x01000007, 0x0100000a, 0x02000000 };

            // var expNest = new bool[] { false, false, false, true, true, true, true };
            // count is calc-ed by the smaller of last row of table OR next row in EventMap table
            // TODO: check with DEV - too much work to figure out, hard code for now - property, event
            var expMemberCount = new uint[]
            {
                /*<Module>*/0, 0, /*ModVBClass*/ 2, 0, /*ModVBStruct*/ 0, 1,
                /*ModVBInnerEnum*/ 0, 0, /*ModVBInnerStruct*/ 0, 0, /*ModVBDele*/0, 0, /*ModVBInnerIFoo*/0, 0,
            };

            var reader = GetMetadataReader(NetModule.ModuleVB01, true);
            Assert.Equal(expNames.Length, reader.TypeDefinitions.Count);

            bool first = true;

            // uint prevPid = 0, prevEid = 0, currPid = 0, currEid = 0;
            uint prevFieldStart = 0;
            uint prevMethodStart = 0;

            int i = 0;
            foreach (var typeDefHandle in reader.TypeDefinitions)
            {
                var typeDef = reader.GetTypeDefinition(typeDefHandle);
                uint fieldStart = (uint)reader.TypeDefTable.GetFieldStart(typeDefHandle.RowId);
                uint methodStart = (uint)reader.TypeDefTable.GetMethodStart(typeDefHandle.RowId);

                Assert.Equal(expNames[i], reader.GetString(typeDef.Name));
                Assert.Equal(expNamespaces[i], reader.GetString(typeDef.Namespace));
                Assert.Equal(expFlags[i], typeDef.Attributes);
                Assert.Equal(expExtends[i], (uint)typeDef.BaseType.Token);

                // Assert.Equal(expNest[i], row.IsNested);
                if (typeDef.Attributes.IsNested())
                {
                    ValidateNestedClass(reader, typeDefHandle, true);
                }

                // == Property
                var rid = reader.PropertyMapTable.FindPropertyMapRowIdFor(typeDefHandle);

                // other == 0 means no property for this type
                if (0 != rid)
                {
                    ValidateProperty(reader, ((uint)TableConstant.mdtTypeDef | (uint)typeDefHandle.RowId),
                        (uint)reader.PropertyMapTable.GetPropertyListStartFor(rid), expMemberCount[i * 2], true);
                }

                // == Event
                rid = reader.EventMapTable.FindEventMapRowIdFor(typeDefHandle);
                if (0 != rid)
                {
                    ValidateEvent(reader, ((uint)TableConstant.mdtTypeDef | (uint)typeDefHandle.RowId),
                        (uint)reader.EventMapTable.GetEventListStartFor(rid), expMemberCount[i * 2 + 1], true);
                }

                // == Field & Method: validate previous row's member as it needs current row's member other to calc how many
                if (!first)
                {
                    ValidateFieldDef(reader, prevFieldStart, fieldStart - prevFieldStart, true);
                    ValidateMethodDef(reader, prevMethodStart, methodStart - prevMethodStart, true);
                    // ValidateProperty(reader, rowId, prevPid, currPid - prevPid, true);
                    // ValidateEvent(reader, rowId, prevEid, currEid - prevEid, true);
                }

                // Last
                if (i + 1 == reader.TypeDefinitions.Count)
                {
                    ValidateFieldDef(reader, prevFieldStart, 0xF0000000, true);
                    ValidateMethodDef(reader, prevMethodStart, 0xF0000000, true);
                }

                prevFieldStart = fieldStart;
                prevMethodStart = methodStart;
                i++;
            }
        }

        /// <summary>
        /// Helper method that will validate that a NamespaceDefinition (and all NamespaceDefinitions considered children
        /// of it) report correct values for their child namespaces, types, etc. All namespaces in the module are expected
        /// to be listed in the allNamespaces array. Additionally, the global namespace is expected to have type definitions
        /// for GlobalClassA, GlobalClassB, and Module. No type forwarder declarations are expected.
        ///
        /// All namespaces that aren't the global NS are expected to have type definitions equal to the array
        /// @namespaceName.Split('.')
        /// So, ns1.Ns2.NS3 is expected to have type definitions
        /// {"ns1", "Ns2", "NS3"}.
        ///
        /// definitionExceptions and forwarderExceptions may be used to override the default expectations. Pass in
        /// namespace (key) and what is expected (list of strings) for each exception.
        /// </summary>
        private void ValidateNamespaceChildren(
            MetadataReader reader,
            NamespaceDefinitionHandle initHandle,
            string[] allNamespaces,
            IReadOnlyDictionary<string, IList<string>> definitionExceptions = null,
            IReadOnlyDictionary<string, IList<string>> forwarderExceptions = null)
        {
            // Don't want to have to deal with null.
            if (definitionExceptions == null)
            {
                definitionExceptions = new Dictionary<string, IList<string>>();
            }

            if (forwarderExceptions == null)
            {
                forwarderExceptions = new Dictionary<string, IList<string>>();
            }

            var rootNamespaceDefinition = reader.GetNamespaceDefinition(initHandle);
            string rootNamespaceName = reader.GetString(initHandle);

            // We need to be in the table of all namespaces...
            Assert.Contains(rootNamespaceName, allNamespaces);

            // Cool. Now check to make sure that .Name only returns the last bit of our namespace name.
            var expNamespaceNameSegment = rootNamespaceName.Split('.').Last();
            var rootNamespaceNameSegment = reader.GetString(rootNamespaceDefinition.Name);
            Assert.Equal(expNamespaceNameSegment, rootNamespaceNameSegment);

            bool isGlobalNamespace = rootNamespaceName.Length == 0;
            string[] expTypeDefinitions = null;
            // Special case: Global NS has GlobalClassA, GlobalClassB. Others just have autogenerated classes.
            if (definitionExceptions.ContainsKey(rootNamespaceName))
            {
                expTypeDefinitions = definitionExceptions[rootNamespaceName].ToArray();
            }
            else if (isGlobalNamespace)
            {
                expTypeDefinitions = new[] { "GlobalClassA", "GlobalClassB", "<Module>" };
            }
            else
            {
                expTypeDefinitions = rootNamespaceName.Split('.');
            }

            // Validating type definitions inside the namespace...
            int numberOfTypeDefinitions = 0;
            foreach (var definitionHandle in rootNamespaceDefinition.TypeDefinitions)
            {
                var definition = reader.GetTypeDefinition(definitionHandle);
                var definitionName = reader.GetString(definition.Name);
                var definitionFullNamespaceName = reader.GetString(definition.Namespace);
                Assert.Equal(rootNamespaceName, definitionFullNamespaceName);
                Assert.Contains(definitionName, expTypeDefinitions);
                numberOfTypeDefinitions += 1;
            }

            // Guarantee that there are no extra unexpected members...
            Assert.Equal(numberOfTypeDefinitions, expTypeDefinitions.Length);

            string[] expTypeForwarders = null;
            if (forwarderExceptions.ContainsKey(rootNamespaceName))
            {
                expTypeForwarders = forwarderExceptions[rootNamespaceName].ToArray();
            }
            else
            {
                expTypeForwarders = new string[] { };
            }

            int numberOfTypeForwarders = 0;
            foreach (var forwarderHandle in rootNamespaceDefinition.ExportedTypes)
            {
                var forwarder = reader.GetExportedType(forwarderHandle);
                Assert.True(reader.StringComparer.Equals(forwarder.Namespace, rootNamespaceName));
                var forwarderName = reader.GetString(forwarder.Name);
                Assert.Contains(forwarderName, expTypeForwarders);
                numberOfTypeForwarders += 1;
            }
            Assert.Equal(expTypeForwarders.Length, numberOfTypeForwarders);

            // Validate sub-namespaces

            // If the last index of '.' in a namespace name is == the current name's length, then
            // that ns is a direct child of the current one!
            IList<string> expChildren = null;

            // Special case: Global NS's children won't have .s in them.
            if (isGlobalNamespace)
            {
                expChildren = allNamespaces.Where(ns => !String.IsNullOrEmpty(ns) && !ns.Contains('.')).ToList();
            }
            else
            {
                expChildren = allNamespaces
                    .Where(ns => ns.StartsWith(rootNamespaceName) && ns.LastIndexOf('.') == rootNamespaceName.Length)
                    .ToList();
            }

            int numberOfSubNamespaces = 0;
            foreach (var subNamespaceHandle in rootNamespaceDefinition.NamespaceDefinitions)
            {
                Assert.False(subNamespaceHandle.IsNil);
                string subNamespaceFullName = reader.GetString(subNamespaceHandle);
                NamespaceDefinition subNamespace = reader.GetNamespaceDefinition(subNamespaceHandle);

                string subNamespaceName = subNamespaceFullName.Split('.').Last();
                Assert.Equal(subNamespaceName, reader.GetString(subNamespace.Name));
                Assert.True(reader.StringComparer.Equals(subNamespace.Name, subNamespaceName));
                Assert.True(reader.StringComparer.StartsWith(subNamespace.Name, subNamespaceName));
                Assert.True(reader.StringComparer.StartsWith(subNamespace.Name, subNamespaceName.Substring(0, subNamespaceName.Length - 1)));

                Assert.Equal(subNamespace.Parent, initHandle);
                Assert.Contains(subNamespaceFullName, expChildren);
                ValidateNamespaceChildren(reader, subNamespaceHandle, allNamespaces, definitionExceptions, forwarderExceptions);
                numberOfSubNamespaces += 1;
            }
            // Guarantee no extra unexpected namespaces...
            Assert.Equal(expChildren.Count, numberOfSubNamespaces);
        }

        /// <summary>
        /// Validates that NamespaceDefinition type properly provides the functionality it is intended to
        /// </summary>
        [Fact]
        public void ValidateNamespaceFunctionality()
        {
            var expNamespaces = new[]
            {
                "", // Global NS
                "Microsoft",
                "Microsoft.CSharp",
                "FxResources",
                "FxResources.Microsoft",
                "FxResources.Microsoft.CSharp",
                "NSTests",
                "NSTests.WithNestedType",
                "NSTests.Nested",
                "NSTests.Nested.AndAgain",
                "NSTests.Nested.Multiple",
                "Nstests",
                "Nstests.Nested",
                "NSTests.nested",
                "SkipFirst",
                "SkipFirst.Namespace",
                "SkipFirst.AndSecond",
                "SkipFirst.AndSecond.Namespace",
                "SkipFirstOnce",
                "SkipFirstOnce.Namespace",
                "Forwarder",
                "Forwarder.NoDefs"
            };

            var uniqueForwarders = new Dictionary<string, IList<string>>();
            uniqueForwarders.Add("Forwarder", new[] { "FwdType" });
            uniqueForwarders.Add("Forwarder.NoDefs", new[] { "FwdType" });
            var uniqueDefinitions = new Dictionary<string, IList<string>>();
            uniqueDefinitions.Add("Microsoft", new string[] { });
            uniqueDefinitions.Add("FxResources", new string[] { });
            uniqueDefinitions.Add("FxResources.Microsoft", new string[] { });
            uniqueDefinitions.Add("Forwarder.NoDefs", new string[] { });
            uniqueDefinitions.Add("SkipFirst", new string[] { });
            uniqueDefinitions.Add("SkipFirst.AndSecond", new string[] { });
            uniqueDefinitions.Add("SkipFirstOnce", new string[] { });

            var reader = GetMetadataReader(Namespace.NamespaceTests);

            NamespaceDefinitionHandle globalHandle = NamespaceDefinitionHandle.FromFullNameOffset(0);
            ValidateNamespaceChildren(reader, globalHandle, expNamespaces, uniqueDefinitions, uniqueForwarders);
        }

        /// <summary>
        /// Validates that the namespace cache is lazy and will not automatically be created with simple name lookups.
        /// </summary>
        [Fact]
        public void ValidateNamespaceCacheLaziness()
        {
            var reader = GetMetadataReader(Namespace.NamespaceTests);

            Assert.False(reader.NamespaceCache.CacheIsRealized);

            var namespaceSet = new HashSet<NamespaceDefinitionHandle>();

            foreach (var typeHandle in reader.TypeDefinitions)
            {
                namespaceSet.Add(reader.GetTypeDefinition(typeHandle).NamespaceDefinition);
            }

            Assert.False(reader.NamespaceCache.CacheIsRealized);

            foreach (var typeForwarderHandle in reader.ExportedTypes)
            {
                namespaceSet.Add(reader.GetExportedType(typeForwarderHandle).NamespaceDefinition);
            }

            Assert.False(reader.NamespaceCache.CacheIsRealized);

            foreach (var namespaceHandle in namespaceSet)
            {
                Assert.False(reader.NamespaceCache.CacheIsRealized);
                Assert.True(namespaceHandle.HasFullName);
                var fullyQualifiedName = reader.GetString(namespaceHandle);
                var expFullyQualifiedName = reader.GetString(namespaceHandle);
                Assert.NotNull(fullyQualifiedName);
                Assert.Equal(fullyQualifiedName, expFullyQualifiedName);
                Assert.False(reader.NamespaceCache.CacheIsRealized);
                var comparisonResult = reader.StringComparer.Equals(namespaceHandle, fullyQualifiedName);
                Assert.True(comparisonResult);
                Assert.False(reader.NamespaceCache.CacheIsRealized);
            }
        }

        /// <summary>
        /// TypeSpec Table Columns: (vector, array, generic, unmanaged pointer, function pointer)
        ///     Signature (offset to #blob)
        /// </summary>
        [Fact]
        public void ValidateTypeSpecTable()
        {
            var expSigs = new byte[][]
            {
                // GenericInst Class AppCS.IContraVar`1< Var!0>
                new byte[] { 0x15, 0x12, 0x10, 0x01, 0x13, 00 },

                // GenericInst Class AppCS.ICoVar`1< Var!0>
                new byte[] { 0x15, 0x12, 0x14, 0x01, 0x13, 00 },

                // GenericInst Class AppCS.INormal`1< Var!0>
                new byte[] { 0x15, 0x12, 0x18, 0x01, 0x13, 00 },

                // GenericInst Class NS.Module.CS01.CS02.ModClassImplImp`1< Class ModVBInnerIFoo>
                new byte[] { 0x15, 0x12, 0x21, 0x01, 0x12, 0x35 },

                // Var!0
                new byte[] { 0x13, 0x00 },

                // GenericInst Class AppCS.ContraInClass`1< Class AppCS.Animal>
                new byte[] { 0x15, 0x12, 0x1c, 0x01, 0x12, 0x28 },

                // GenericInst Class AppCS.CoOutClass`1< Class AppCS.Tiger>
                new byte[] { 0x15, 0x12, 0x20, 0x01, 0x12, 0x2c },

                // GenericInst Class AppCS.NormalClass`1< Class AppCS.Animal>
                new byte[] { 0x15, 0x12, 0x24, 0x01, 0x12, 0x28 },

                // GenericInst Class AppCS.NormalClass`1< Class AppCS.Tiger>
                new byte[] { 0x15, 0x12, 0x24, 0x01, 0x12, 0x2c },

                // GenericInst Class AppCS.INormal`1< Class AppCS.Animal>
                new byte[] { 0x15, 0x12, 0x18, 0x01, 0x12, 0x28 },
            };

            var reader = GetMetadataReader(NetModule.AppCS);
            var table = reader.TypeSpecTable;

            // Validity Rules
            Assert.Equal(expSigs.Length, table.NumberOfRows);
            for (int i = 0; i < table.NumberOfRows; i++)
            {
                var sig = reader.GetBlobBytes(table.GetSignature(TypeSpecificationHandle.FromRowId(i + 1)));
                for (int j = 0; j < expSigs[i].Length; j++)
                {
                    Assert.Equal(expSigs[i][j], sig[j]);
                }
            }
        }

        [Fact]
        public void ValidateTypeSpecTableMod()
        {
            var expSigs = new byte[][]
            {
                // MDArray ValueClass ModVBInnerStruct 2 0 2 0 0
                new byte[] { 0x14, 0x11, 0x14, 0x02, 0x00, 0x02, 0x00, 0x00 },
            };

            var reader = GetMetadataReader(NetModule.ModuleVB01, true);
            var table = reader.TypeSpecTable;

            // Validity Rules
            Assert.Equal(expSigs.Length, table.NumberOfRows);
            for (int i = 0; i < table.NumberOfRows; i++)
            {
                var sig = reader.GetBlobBytes(table.GetSignature(TypeSpecificationHandle.FromRowId(i + 1)));
                for (int j = 0; j < expSigs[i].Length; j++)
                {
                    Assert.Equal(expSigs[i][j], sig[j]);
                }
            }
        }

        /// <summary>
        /// Field Table Columns:
        ///     Name (offset to #String)
        ///     Flags (2 byte unsigned)
        ///     Signature (offset to #blob)
        /// </summary>
        private void ValidateFieldDef(MetadataReader reader, uint startIndex, uint count, bool isMod = false)
        {
            if (count == 0)
            {
                return;
            }

            // APPCS
            var expNames = new string[] { "AppField01", "AppField02" };
            var expFlags = new FieldAttributes[]
            {
                /*0x11*/
                         FieldAttributes.Private | FieldAttributes.Static,
                /*0x01*/ FieldAttributes.Private,
                };
            var expSigs = new byte[][] { new byte[] { 0x06, 0x12, 0x11 }, new byte[] { 0x06, 0x12, 0x25 }, };

            // =====================================================================================================
            // VB Module - 8
            var modNames = new string[] { "ConstString", "ArrayField", "AnEventEvent", "value__", "None", "Red", "Yellow", "Blue", };
            var modFlags = new FieldAttributes[]
            {
                /* 0x8053 */
                             FieldAttributes.HasDefault | FieldAttributes.Literal | FieldAttributes.Static | FieldAttributes.FamANDAssem | FieldAttributes.Private,
                /* 0x0016 */ FieldAttributes.Static | FieldAttributes.Family | FieldAttributes.FamANDAssem,
                /* 0x0001 */ FieldAttributes.Private,
                /* 0x0606 */ FieldAttributes.RTSpecialName | FieldAttributes.SpecialName | FieldAttributes.Family | FieldAttributes.FamANDAssem,
                /* 0x8056 */ FieldAttributes.HasDefault | FieldAttributes.Literal | FieldAttributes.Static | FieldAttributes.Family | FieldAttributes.FamANDAssem,
                /* 0x8056 */ FieldAttributes.HasDefault | FieldAttributes.Literal | FieldAttributes.Static | FieldAttributes.Family | FieldAttributes.FamANDAssem,
                /* 0x8056 */ FieldAttributes.HasDefault | FieldAttributes.Literal | FieldAttributes.Static | FieldAttributes.Family | FieldAttributes.FamANDAssem,
                /* 0x8056 */ FieldAttributes.HasDefault | FieldAttributes.Literal | FieldAttributes.Static | FieldAttributes.Family | FieldAttributes.FamANDAssem,
                };
            var modSigs = new byte[][]
            {
                new byte[] { 0x06, 0x0e }, new byte[] { 0x06, 0x14, 0x11, 0x14, 02, 00, 02, 00, 00 },
                new byte[] { 0x06, 0x12, 0x18 }, new byte[] { 0x06, 0x08 },
                new byte[] { 0x06, 0x11, 0x10 }, new byte[] { 0x06, 0x11, 0x10 },
                new byte[] { 0x06, 0x11, 0x10 }, new byte[] { 0x06, 0x11, 0x10 },
            };

            if (startIndex > reader.FieldTable.NumberOfRows)
            {
                return;
            }

            uint zeroBased = startIndex - 1;
            uint delta = count;

            // Last one
            if (0xF0000000 == count)
            {
                delta = (uint)reader.FieldTable.NumberOfRows - zeroBased;
                if (0 == delta)
                {
                    return;
                }
            }

            Assert.InRange((uint)reader.FieldTable.NumberOfRows, zeroBased + delta, uint.MaxValue); // 1 based
            for (uint i = zeroBased; i < zeroBased + delta; i++)
            {
                var handle = FieldDefinitionHandle.FromRowId((int)(i + 1));
                var row = reader.GetFieldDefinition(handle);

                if (isMod)
                {
                    Assert.Equal(modNames[i], reader.GetString(row.Name));
                    Assert.Equal(modFlags[i], row.Attributes);
                }
                else
                {
                    Assert.Equal(expNames[i], reader.GetString(row.Name));
                    Assert.Equal(expFlags[i], row.Attributes);
                }

                var sig = reader.GetBlobBytes(row.Signature);

                // calling convention, always 6 for field
                Assert.Equal(6, sig[0]);
                int len = 0;
                if (isMod)
                {
                    len = modSigs[i].Length;
                }
                else
                {
                    len = expSigs[i].Length;
                }

                for (int j = 1; j < len; j++)
                {
                    if (isMod)
                    {
                        Assert.Equal(modSigs[i][j], sig[j]);
                    }
                    else
                    {
                        Assert.Equal(expSigs[i][j], sig[j]);
                    }
                }
            }
        }

        /// <summary>
        /// Method Table Columns:
        ///     Name (offset to #String)
        ///     Flags, ImplFlags (2 byte unsigned)
        ///     Signature (offset to #blob)
        ///     ParamList (RID to Param)
        ///     RVA (4-byte unsigned) -> body
        /// </summary>
        private void ValidateMethodDef(MetadataReader reader, uint startIndex, uint count, bool isMod = false)
        {
            if (0 == count)
            {
                return;
            }

            var expNames = new string[]
            {
                "get_AppProp", ".ctor", "AppMethod", ".cctor", "get_Item",
                "Use", ".ctor", "set_ContraFooProp", "CoFooMethod", "NormalFoo",
                "set_ContraFooProp", ".ctor", "CoFooMethod", ".ctor", "NormalFoo",
                ".ctor", ".ctor", ".ctor", "Main", ".ctor",
            };
            var expFlags = new ushort[]
            {
                0x0883, 0x1886, 0x0084, 0x1891, 0x0881,   0x0086, 0x1886, 0x0dc6, 0x05c6, 0x05c6,
                    0x09e6, 0x1886, 0x01e6, 0x1886, 0x01e6, 0x1886, 0x1886, 0x1886, 0x0096, 0x1886
                };
            var expRVAs = new uint[]
            {
                0x2050, 0x2070, 0x20a4, 0x20b7, 0x20c0, 0x20d4, 0x2105, 0, 0, 0,
                0x2115, 0x2118, 0x2120, 0x2152, 0x215c, 0x2177, 0x217f, 0x2187, 0x2190, 0x21d5
            };

            var expSigs = new byte[][]
            {
                new byte[] { 0x20, 0x00, 0x15, 0x12, 0x15, 0x02, 0x12, 0x19, 0x1c }, new byte[] { 0x20, 01, 01, 0x10, 0x12, 0x1d },
                new byte[] { 0x30, 01, 01, 0x15, 0x12, 0x21, 0x01, 0x1e, 00, 0x1e, 00 }, new byte[] { 0x00, 0x00, 0x01 },
                new byte[] { 0x20, 01, 0x11, 0x29, 0x11, 0x29 }, new byte[] { 0x20, 00, 0x15, 0x12, 0x21, 0x01, 0x12, 0x35 },
                new byte[] { 0x20, 00, 01 }, new byte[] { 0x20, 01, 01, 0x13, 00 },
                new byte[] { 0x20, 00, 0x13, 00 }, new byte[] { 0x20, 01, 0x13, 00, 0x13, 00 },
                new byte[] { 0x20, 01, 01, 0x13, 00 }, new byte[] { 0x20, 00, 01 },
                new byte[] { 0x20, 00, 0x13, 00 }, new byte[] { 0x20, 00, 01 },
                new byte[] { 0x20, 01, 0x13, 00, 0x13, 00 }, new byte[] { 0x20, 00, 01 },
                new byte[] { 0x20, 00, 01 }, new byte[] { 0x20, 00, 01 }, new byte[] { 00, 00, 08 }, new byte[] { 0x20, 00, 01 },
            };

            var modNames = new string[]
            {
                ".ctor", "get_ModVBDefaultProp", "set_ModVBDefaultProp", "get_ModVBProp", "BCSub01",
                "BCFunc02", ".cctor", "add_AnEvent", "remove_AnEvent", "EventHandler1",
                "BSFunc01", ".ctor", "BeginInvoke", "EndInvoke", "Invoke",
            };
            var modFlags = new ushort[]
            {
                0x1806, 0x0803, 0x0803, 0x0806, 0x0006,
                0x0006, 0x1811, 0x0806, 0x0806, 0x0006, 0x0006, 0x1806, 0x0346, 0x0346, 0x0346,
                };
            var modImpls = new ushort[]
            {
                0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 3, 3, 3, 3,
            };
            var modRVAs = new uint[]
            {
                0x2050, 0x2058, 0x2070, 0x2074, 0x2090,
                    0x20a8, 0x20bc, 0x20cc, 0x20e8, 0x2104,  0x2108, 0, 0, 0, 0
            };
            var modSigs = new byte[][]
            {
                new byte[] { 0x20, 0x00, 0x01 }, new byte[] { 0x20, 01, 0x0e, 0x08 },
                new byte[] { 0x20, 02, 01, 0x08, 0x0e }, new byte[] { 0x20, 00, 0x11, 0x09 },
                new byte[] { 0x20, 02, 01, 0x11, 0x0d, 0x12, 0x11 }, new byte[] { 0x20, 01, 0x12, 0x15, 0x10, 0x12, 0x19 },
                new byte[] { 0x00, 00, 01 }, new byte[] { 0x20, 01, 01, 0x12, 0x18 },
                new byte[] { 0x20, 01, 01, 0x12, 0x18 }, new byte[] { 0x20, 02, 01, 0x1c, 0x12, 0x21 },
                new byte[] { 0x20, 01, 0x1c, 0x10, 0x11, 0x14 }, new byte[] { 0x20, 02, 01, 0x1c, 0x18 },
                new byte[] { 0x20, 04, 0x12, 0x2d, 0x1c, 0x12, 0x21, 0x12, 0x31, 0x1c }, new byte[] { 0x20, 01, 01, 0x12, 0x2d },
                new byte[] { 0x20, 02, 01, 0x1c, 0x12, 0x21 },
            };

            if (startIndex > reader.MethodDefTable.NumberOfRows)
            {
                return;
            }

            uint zeroBased = startIndex - 1;
            uint delta = count;

            // Last one
            if (0xF0000000 == count)
            {
                delta = (uint)reader.MethodDefTable.NumberOfRows - zeroBased;
                if (0 == delta)
                {
                    return;
                }
            }

            Assert.InRange((uint)reader.MethodDefTable.NumberOfRows, zeroBased + delta, uint.MaxValue); // 1 based
            bool first = true;
            uint prevParamStart = 0;
            for (uint i = zeroBased; i < zeroBased + delta; i++)
            {
                var handle = MethodDefinitionHandle.FromRowId((int)i + 1);
                var flags = reader.MethodDefTable.GetFlags(handle);
                var implFlags = reader.MethodDefTable.GetImplFlags(handle);
                var rva = reader.MethodDefTable.GetRva(handle);
                var name = reader.MethodDefTable.GetName(handle);
                var signature = reader.MethodDefTable.GetSignature(handle);
                var paramStart = (uint)reader.MethodDefTable.GetParamStart((int)i + 1);

                if (isMod)
                {
                    // Console.WriteLine("M: {0}", reader.GetString(row.Name));
                    Assert.Equal(modNames[i], reader.GetString(name));

                    Assert.Equal(modFlags[i], (ushort)flags);
                    Assert.Equal(modImpls[i], (ushort)implFlags);
                    Assert.Equal(modRVAs[i], (uint)rva);
                }
                else
                {
                    // Console.WriteLine("M: {0}", reader.GetString(row.Name));
                    Assert.Equal(expNames[i], reader.GetString(name));

                    Assert.Equal(expFlags[i], (ushort)flags);
                    Assert.Equal((ushort)0, (ushort)implFlags);
                    Assert.Equal(expRVAs[i], (uint)rva);
                }

                var sig = reader.GetBlobBytes(signature);
                int len = 0;
                if (isMod)
                {
                    len = modSigs[i].Length;
                }
                else
                {
                    len = expSigs[i].Length;
                }

                for (int j = 0; j < len; j++)
                {
                    if (isMod)
                    {
                        Assert.Equal(modSigs[i][j], sig[j]);
                    }
                    else
                    {
                        Assert.Equal(expSigs[i][j], sig[j]);
                    }
                }

                // validate previous row's param as it needs current row's other to calc how many
                if (!first)
                {
                    ValidateParam(reader, prevParamStart, paramStart - prevParamStart, isMod);
                }

                // Last
                if (i + 1 == reader.MethodDefTable.NumberOfRows)
                {
                    ValidateParam(reader, paramStart, 0xF0000000, isMod);
                }

                prevParamStart = paramStart;
                first = false;
            }
        }

        /// <summary>
        /// Param Table Columns:
        ///     Name (offset to #String)
        ///     Flags, Sequence (2 byte unsigned)
        /// </summary>
        private void ValidateParam(MetadataReader reader, uint startIndex, uint count, bool isMod = false)
        {
            if (count == 0)
            {
                return;
            }

            // AppCS - 7
            var expNames = new string[] { "p", "t", "p", "value", "t", "value", "t" };

            // =================
            // ModuleVB01 - 20
            var modNames = new string[]
            {
                "index", "index", "value", "em", "cls",    "del", "obj", "obj", "o", "e",
                "p", "TargetObject", "TargetMethod", "o", "e",
                "DelegateCallback", "DelegateAsyncState", "DelegateAsyncResult", "o", "e",
            };
            var modFlags = new ushort[] { 0, 0, 0, 0, 0x1010, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, };
            var modSeqs = new ushort[] { 1, 1, 2, 1, 2, 1, 1, 1, 1, 2, 1, 1, 2, 1, 2, 3, 4, 1, 1, 2, };

            if (startIndex > reader.ParamTable.NumberOfRows)
            {
                return;
            }

            uint zeroBased = startIndex - 1;
            uint delta = count;

            // Last one
            if (0xF0000000 == count)
            {
                delta = (uint)reader.ParamTable.NumberOfRows - zeroBased;
                if (0 == delta)
                {
                    return;
                }
            }

            Assert.InRange((uint)reader.ParamTable.NumberOfRows, zeroBased + delta, uint.MaxValue); // 1 based
            for (uint i = zeroBased; i < zeroBased + delta; i++)
            {
                var handle = ParameterHandle.FromRowId((int)i + 1);
                var row = reader.GetParameter(handle);

                // Console.WriteLine("P: {0}", GetStringData(row.Name));
                if (isMod)
                {
                    Assert.Equal(modNames[i], reader.GetString(row.Name));
                    Assert.Equal(modFlags[i], (ushort)row.Attributes);
                    Assert.Equal(modSeqs[i], row.SequenceNumber);
                }
                else
                {
                    Assert.Equal(expNames[i], reader.GetString(row.Name));
                    Assert.Equal((ushort)0, (ushort)row.Attributes);
                    Assert.Equal((ushort)1, row.SequenceNumber);
                }
            } // for
        }

        /// <summary>
        /// PropertyMap Table Columns:
        ///     Parent (RID to TypeDef)
        ///     PropertyList (RID to EventTable)
        /// ===========================================
        /// Property Table Columns:
        ///     Name (offset to #String)
        ///     PropFlags (2 byte unsigned)
        ///     Type (offset to #blob - Signature)
        /// </summary>
        private void ValidateProperty(MetadataReader reader, uint rowId, uint startIndex, uint count, bool isVBMod = false)
        {
            if (0 == count)
            {
                return;
            }

            // ModuleCS01
            var expNames = new string[]
            {
                "AppProp", "P01", "Item", "P01", "Item",
                "CS1IGoo<System.Linq.Expressions.Expression,System.Object>.P01",
                "CS1IGoo<System.Linq.Expressions.Expression,System.Object>.Item",
            };
            var expSigs = new byte[][]
            {
                new byte[] { 0x28, 00, 0x15, 0x12, 0x21, 0x02, 0x12, 0x19, 0x1c }, new byte[] { 0x28, 00, 0x13, 00 },
                new byte[] { 0x28, 01, 0x1c, 0x13, 00 }, new byte[] { 0x28, 0x00, 0x13, 00 }, new byte[] { 0x28, 0x01, 0x1c, 0x13, 00 },
                new byte[] { 0x28, 00, 0x12, 0x19 }, new byte[] { 0x28, 01, 0x1c, 0x12, 0x19 }
            };

            // ModuleVB01
            // Prop: 0:0000, 1:string#13c, 2:blob#70 | 0:0000, 1:string#14d, 2:blob#75
            var modNames = new string[]
            {
                "ModVBDefaultProp", "ModVBProp",
            };
            var modSigs = new byte[][]
            {
                new byte[] { 0x28, 01, 0x0e, 0x08 }, new byte[] { 0x28, 00, 0x11, 0x09 },
            };

            // Validity Rules
            uint zeroBased = startIndex - 1;
            uint delta = count;

            // Last one
            if (0xF0000000 == count)
            {
                delta = (uint)reader.PropertyTable.NumberOfRows - zeroBased;
                if (0 == delta)
                {
                    return;
                }
            }

            Assert.InRange((uint)reader.PropertyTable.NumberOfRows, zeroBased + count, uint.MaxValue);
            for (uint i = zeroBased; i < zeroBased + count; i++)
            {
                var handle = PropertyDefinitionHandle.FromRowId((int)i + 1);
                var row = reader.GetPropertyDefinition(handle);

                // Name
                if (isVBMod)
                {
                    Assert.Equal(modNames[i], reader.GetString(row.Name));
                }
                else
                {
                    Assert.Equal(expNames[i], reader.GetString(row.Name));
                }

                Assert.Equal(0, (ushort)row.Attributes);

                var sig = reader.GetBlobBytes(row.Signature);
                Assert.Equal(40, sig[0]);
                byte[] exp;
                if (isVBMod)
                {
                    exp = modSigs[i];
                }
                else
                {
                    exp = expSigs[i];
                }

                for (int j = 0; j < exp.Length; j++)
                {
                    Assert.Equal(exp[j], sig[j]);
                }
            } // for
        }

        /// <summary>
        /// EventMap Table Columns:
        ///     Parent (RID to TypeDef)
        ///     EventList (RID to EventTable)
        /// ===========================================
        /// Event Table Columns:
        ///     Name (offset to #String)
        ///     Flags (2 byte unsigned)
        ///     EventType (token to TypeDefOrRef)
        /// </summary>
        private void ValidateEvent(MetadataReader reader, uint rowId, uint startIdx, uint count, bool isVBMod = false)
        {
            if (0 == count)
            {
                return;
            }

            var expNames = new string[] { "E01", "E01", "CS1IGoo<System.Linq.Expressions.Expression,System.Object>.E01" };

            // ModuleVB01
            // Map: 0:TypeDef[2000003], 1:Event[14000001]
            // Event: 0:0000, 1:string#1c6, 2:TypeDefOrRef[02000006]
            var modNames = new string[] { "AnEvent" };
            var modTokens = new uint[] { 0x02000006 };

            uint zeroBased = startIdx - 1;
            uint delta = count;

            // Last one
            if (0xF0000000 == count)
            {
                delta = (uint)reader.EventTable.NumberOfRows - zeroBased;
                if (0 == delta)
                {
                    return;
                }
            }

            // Validity Rules
            Assert.InRange((uint)reader.EventTable.NumberOfRows, zeroBased + count, uint.MaxValue);
            for (uint i = zeroBased; i < zeroBased + count; i++)
            {
                var handle = EventDefinitionHandle.FromRowId((int)i + 1);
                var evnt = reader.GetEventDefinition(handle);

                // Name
                if (isVBMod)
                {
                    Assert.Equal(modNames[i], reader.GetString(evnt.Name));
                }
                else
                {
                    Assert.Equal(expNames[i], reader.GetString(evnt.Name));
                }

                Assert.Equal(0, (ushort)evnt.Attributes);
                if (isVBMod)
                {
                    Assert.Equal(modTokens[i], (uint)evnt.Type.Token);
                }
                else
                {
                    Assert.Equal((int)rowId, evnt.Type.RowId); // could be TypeSpec Id if it's in generic
                }
            }
        }

        /// <summary>
        /// NestedClass Table Columns:
        ///     NestedClass (RID to TypeDef) nestee
        ///     EnclosingClass (RID to TypeDef)
        /// </summary>
        private void ValidateNestedClass(MetadataReader reader, TypeDefinitionHandle typeDef, bool isMod = false)
        {
            // var expNC = new uint[] { 4, 5, 6 };
            var expClasses = new int[][] { new int[] { 4, 5, 6 }, new int[] { 3, 3, 5 } };
            var modClasses = new int[][] { new int[] { 4, 5, 6, 7 }, new int[] { 2, 3, 3, 5 } };

            int rid = reader.NestedClassTable.FindEnclosingType(typeDef).RowId;
            int[][] classes = expClasses;

            if (isMod)
            {
                classes = modClasses;
            }

            Assert.Equal(rid, classes[1].Where((x, index) => classes[0][index] == typeDef.RowId).First());
        }

        /// <summary>
        /// MemberRef Table Columns:
        ///     Name (offset to #string)
        ///     Class (4-bytes unsigned) - Parent
        ///     Signature (offset to #blob)
        /// </summary>
        [Fact]
        public void ValidateMemberRefTableMod()
        {
            // 6
            var expNames = new string[]
            {
                ".ctor", "CM", ".ctor", ".ctor", "Combine", "Remove",
            };

            var expClass = new int[]
            {
                0x01000001, 0x01000004, 0x0100000f, 0x1b000001, 0x01000010, 0x01000010,
            };

            var expSigs = new byte[][]
            {
                new byte[] { 0x20, 00, 01 },
                new byte[] { 0x20, 03, 0x12, 0x39, 0x08, 0x09, 0x0a },
                new byte[] { 0x20, 01, 01, 0x0e },
                new byte[] { 0x20, 02, 01, 08, 08 },
                new byte[] { 00, 02, 0x12, 0x41, 0x12, 0x41, 0x12, 0x41 },
                new byte[] { 00, 02, 0x12, 0x41, 0x12, 0x41, 0x12, 0x41 },
            };

            var reader = GetMetadataReader(NetModule.ModuleVB01, true);

            // Validity Rules
            Assert.Equal(expNames.Length, reader.MemberReferences.Count);
            int i = 0;
            foreach (var memberRef in reader.MemberReferences)
            {
                var row = reader.GetMemberReference(memberRef);
                Assert.Equal(reader.GetString(row.Name), expNames[i]);
                Assert.Equal(row.Parent.Token, expClass[i]);

                var sig = reader.GetBlobBytes(row.Signature);
                for (int j = 0; j < expSigs[i].Length; j++)
                {
                    Assert.Equal(expSigs[i][j], sig[j]);
                }

                i++;
            }
        }

        /// <summary>
        /// InterfaceImpl Table Columns:
        ///     Class (RID to TypeDef)
        ///     Interface (token to typeDefOrRef)
        /// </summary>
        [Fact]
        public void ValidateInterfaceImplTableMod()
        {
            // CSModule1
            var expTDef = new int[] { 0x02000007, 0x2000008 }; // class other who implements the interface
            var expIfs = new int[] { 0x1b000001, 0x1b000002 }; // TypeSpec table

            var reader = GetMetadataReader(NetModule.ModuleCS01, true);
            Assert.Equal(2, reader.InterfaceImplTable.NumberOfRows);

            // uint ct = 0;
            // var xxx = table.FindInterfaceImplForType(7, out ct); // 0x01
            // var yyy = table.FindInterfaceImplForType(8, out ct); // 0x02
            for (int i = 0; i < reader.InterfaceImplTable.NumberOfRows; i++)
            {
                var ioffset = reader.InterfaceImplTable.GetInterface(i + 1).Token; // 0x1b000001|2
                Assert.Equal(ioffset, expIfs[i]);
            }
        }

        /// <summary>
        /// GenericParam Columns:
        ///     Name (offset to #string)
        ///     Flags, Number (2 byte unsigned)
        ///     Owner (token to TypeDefOrRef)
        /// </summary>
        [Fact]
        public void ValidateGenericParamTable()
        {
            // AppCS - 7
            var expNames = new string[] { "V", "CT", "CO", "T", "CT1", "CO1", "T1" };
            var expFlags = new GenericParameterAttributes[]
            {
                /* 4 */
                        GenericParameterAttributes.ReferenceTypeConstraint,
                /* 6 */ GenericParameterAttributes.ReferenceTypeConstraint | GenericParameterAttributes.Contravariant,
                /* 1 */ GenericParameterAttributes.Covariant,
                /* 0 */ GenericParameterAttributes.None,
                /* 4 */ GenericParameterAttributes.ReferenceTypeConstraint,
                /* 0x10 */ GenericParameterAttributes.DefaultConstructorConstraint, // Mask 001C
                /* 0 */ GenericParameterAttributes.None
            };
            var expNumber = new ushort[] { 0, 0, 0, 0, 0, 0, 0 };
            var expTypeTokens = new int[] { 0x06000003, 0x02000004, 0x02000005, 0x02000006, 0x02000007, 0x02000008, 0x02000009, };

            // ---------------------------------------------------
            // ModuleCS01 - 5
            var modNames = new string[] { "T", "T", "R", "T", "X" };
            var modFlags = new GenericParameterAttributes[]
            {
                /* 0 */
                        GenericParameterAttributes.None,
                /* 4 */
                        GenericParameterAttributes.ReferenceTypeConstraint,
                /* 4 */ GenericParameterAttributes.ReferenceTypeConstraint,
                /* 4 */ GenericParameterAttributes.ReferenceTypeConstraint,
                /* 0 */ GenericParameterAttributes.None
            };

            var modNumber = new ushort[] { 0, 0, 1, 0, 0 };
            var modTypeTokens = new int[] { 0x02000006, 0x02000007, 0x02000007, 0x02000008, 0x06000025, };

            var reader = GetMetadataReader(NetModule.AppCS);

            // Validity Rules
            Assert.Equal(expNames.Length, reader.GenericParamTable.NumberOfRows);

            for (int i = 0; i < reader.GenericParamTable.NumberOfRows; i++)
            {
                var handle = GenericParameterHandle.FromRowId(i + 1);
                Assert.Equal(expNames[i], reader.GetString(reader.GenericParamTable.GetName(handle)));
                Assert.Equal(expFlags[i], reader.GenericParamTable.GetFlags(handle));
                Assert.Equal(expNumber[i], reader.GenericParamTable.GetNumber(handle));
                Assert.Equal(expTypeTokens[i], reader.GenericParamTable.GetOwner(handle).Token);
            }

            // =======================================

            reader = GetMetadataReader(NetModule.ModuleCS01, true);

            // Validity Rules
            Assert.Equal(modNames.Length, reader.GenericParamTable.NumberOfRows);

            for (int i = 0; i < reader.GenericParamTable.NumberOfRows; i++)
            {
                var handle = GenericParameterHandle.FromRowId(i + 1);
                Assert.Equal(modNames[i], reader.GetString(reader.GenericParamTable.GetName(handle)));
                Assert.Equal(modFlags[i], reader.GenericParamTable.GetFlags(handle));
                Assert.Equal(modNumber[i], reader.GenericParamTable.GetNumber(handle));
                Assert.Equal(modTypeTokens[i], reader.GenericParamTable.GetOwner(handle).Token);
            }
        }

        /// <summary>
        /// Class Layout Table Columns:
        ///     PackingSize (2 byte unsigned)
        ///     ClassSize (4 byte unsigned)
        ///     Parent (RID to TypeDef)
        /// </summary>
        [Fact]
        public void ValidateClassLayoutTable()
        {
            // Interop
            var comTypeRids = new int[] { 4 };
            var comPkSize = new ushort[] { 0x10 };
            var comCsSize = new uint[] { 8 };

            // VBModule
            var modTypeRids = new int[] { 5 };
            var modPkSize = new ushort[] { 0 };
            var modCsSize = new uint[] { 1 };

            var reader = GetMetadataReader(Interop.Interop_Mock01);

            for (uint i = 0; i < reader.ClassLayoutTable.NumberOfRows; i++)
            {
                var row = reader.GetTypeLayout(TypeDefinitionHandle.FromRowId(comTypeRids[i]));
                Assert.Equal(comPkSize[i], row.PackingSize);
                Assert.Equal(comCsSize[i], row.ClassSize);
            }

            // =============================================
            reader = GetMetadataReader(NetModule.ModuleVB01, true);
            for (uint i = 0; i < reader.ClassLayoutTable.NumberOfRows; i++)
            {
                var row = reader.GetTypeLayout(TypeDefinitionHandle.FromRowId(modTypeRids[i]));
                Assert.Equal(modPkSize[i], row.PackingSize);
                Assert.Equal(modCsSize[i], row.ClassSize);
            }
        }

        /// <summary>
        /// FieldLayout Table Columns:
        ///     Offset (4 byte unsigned)
        ///     Field (token to Field Table)
        /// </summary>
        [Fact]
        public void ValidateFieldLayoutTable()
        {
            // Interop
            var comFieldRids = new int[] { 7, 8, 9, 10 };
            var comOffset = new int[] { 0, 0, 0, 0 };

            var reader = GetMetadataReader(Interop.Interop_Mock01);

            for (int i = 0; i < comFieldRids.Length; i++)
            {
                var field = reader.GetFieldDefinition(FieldDefinitionHandle.FromRowId(comFieldRids[i]));
                Assert.Equal(comOffset[i], field.GetOffset());
            }
        }

        /// <summary>
        /// FieldMarshal Table Columns:
        ///     Parent (token to Field/Param table)
        ///     Native Type (offset to #blob)
        /// </summary>
        [Fact]
        public void ValidateFieldMarshal()
        {
            var comParents = new int[]
            {
                0x08000001, 0x08000002, 0x08000005, 0x04000007, 0x04000008, 0x04000009,
                0x0400000a, 0x0400000d, 0x0800000e, 0x0800000f, 0x08000032, 0x08000033
            };

            var comNatives = new byte[][]
            {
                new byte[] { 0x08 }, new byte[] { 0x1b },
                new byte[] { 0x13 }, new byte[] { 0x03 },
                new byte[] { 0x06 }, new byte[] { 0x07 },
                new byte[] { 0x0a }, new byte[] { 0x1b },
                new byte[] { 0x1a }, new byte[] { 0x1a },
                new byte[] { 0x2a, 0x50 }, new byte[] { 0x2a, 0x50 },
            };

            var reader = GetMetadataReader(Interop.Interop_Mock01);

            for (int i = 0; i < reader.FieldMarshalTable.NumberOfRows; i++)
            {
                int rid = i + 1;
                Assert.Equal(comParents[i], reader.FieldMarshalTable.GetParent(rid).Token);

                var blob = reader.GetBlobBytes(reader.FieldMarshalTable.GetNativeType(rid));
                for (int j = 0; j < comNatives[i].Length; j++)
                {
                    Assert.Equal(comNatives[i][j], blob[j]);
                }
            }
        }

        /// <summary>
        /// InterfaceImpl Table Columns:
        ///     Class (RID to TypeDef)
        ///     Interface (token to typeDefOrRef)
        /// </summary>
        [Fact]
        public void ValidateInterfaceImplTable()
        {
            // class other who implements the interface
            // InteropImpl
            var comClassRids = new int[] { 2, 3, 4 }; // , 0x02000002, 0x2000003, 0x2000004, };
            // TypeDef/Ref/Spec table
            var comInterface = new int[] { 0x01000002, 0x01000004, 0x01000005, };

            // CSModule1
            var modClassRids = new int[] { 8, 9 }; // 0x02000008, 0x2000009 };
            var modInterface = new int[] { 0x1b000001, 0x1b000002 };

            var reader = GetMetadataReader(Interop.Interop_Mock01_Impl);

            for (int i = 0; i < comClassRids.Length; i++)
            {
                var impls = reader.GetTypeDefinition(TypeDefinitionHandle.FromRowId(comClassRids[i])).GetInterfaceImplementations();
                Assert.Equal(comInterface[i], reader.GetInterfaceImplementation(impls.Single()).Interface.Token);
            }

            reader = GetMetadataReader(NetModule.ModuleCS01, true);
            for (int i = 0; i < modClassRids.Length; i++)
            {
                var impls = reader.GetTypeDefinition(TypeDefinitionHandle.FromRowId(modClassRids[i])).GetInterfaceImplementations();
                Assert.Equal(modInterface[i], reader.GetInterfaceImplementation(impls.Single()).Interface.Token);
            }
        }

        /// <summary>
        /// MethodImpl Table Columns:
        ///     Class  (RID to TypeDef)
        ///     MethodBody (token to MethodDefOrRef)
        ///     MethodDecl (token to MethodDefOrRef)
        /// </summary>
        [Fact]
        public void ValidateMethodImplTable()
        {
            // 6
            var comClassRids = new int[] { /*0x2000002*/ 2, 2, 2, 2, 2, 2, };
            var comMthBody = new int[] { 0x06000001, 0x06000002, 0x06000003, 0x06000004, 0x06000005, 0x06000006, };
            var comMthDecl = new int[] { 0x0a000001, 0x0a000002, 0x0a000003, 0x0a000004, 0x0a000005, 0x0a000006, };

            var reader = GetMetadataReader(Interop.Interop_Mock01_Impl);

            // Validity Rules
            Assert.Equal(comClassRids.Length, reader.MethodImplTable.NumberOfRows);
            for (int i = 0; i < reader.MethodImplTable.NumberOfRows; i++)
            {
                var handle = MethodImplementationHandle.FromRowId(i + 1);
                var row = reader.GetMethodImplementation(handle);
                Assert.Equal(comClassRids[i], row.Type.RowId);
                Assert.Equal(comMthBody[i], row.MethodBody.Token);
                Assert.Equal(comMthDecl[i], row.MethodDeclaration.Token);
            }
        }

        /// <summary>
        /// Field Table Columns:
        ///     Parent (token of type HasCustomAttribute)
        ///     Types (token of type CustomAttributeType)
        ///     Value (offset to #blob)
        /// </summary>
        [Fact]
        public void ValidateCustomAttribute()
        {
            // Interop - 0x37 (55) - check every 5th
            var comTypes = new int[]
            {
                0x0a000002, 0x0a00000e, 0x0a000013, 0x0a00001b, 0x0a000002,
                0x0a000021, 0x0a00001a, 0x0a00001a, 0x0a00001b, 0x0a00001b, 0x0a000006
            };

            var comParents = new int[]
            {
                0x08000001, 0x20000001, 0x20000001, 0x06000002, 0x02000004,
                0x06000007, 0x04000008, 0x04000009, 0x0400000b, 0x0400000d, 0x0800000f
            };

            var comValues = new byte[][]
            {
                new byte[] { 01, 00, 00, 00 },
                new byte[]
                {
                    0x01, 00, 00, 01, 00, 0x53, 0x02, 0x15, 0x54, 0x68, 0x72, 0x6f, 0x77, 0x4f, 0x6e,
                    0x55, 0x6e, 0x6d, 0x61, 0x70, 0x70, 0x61, 0x62, 0x6c, 0x65, 0x43, 0x68, 0x61, 0x72, 0x01
                },
                new byte[] { 01, 00, 01, 00, 00, 00, 00, 00, 00, 00, 00, 00 },
                new byte[] { 01, 00, 0xf3, 03, 00, 00, 00, 00 },
                new byte[] { 01, 00, 00, 00 },
                new byte[] { 01, 00, 00, 00 },
                new byte[] { 01, 00, 04, 00, 00, 00, 00, 00 },
                new byte[] { 01, 00, 04, 00, 00, 00, 00, 00 },
                new byte[] { 01, 00, 01, 00, 00, 00, 00, 00 },
                new byte[] { 01, 00, 03, 00, 00, 00, 00, 00 },
                new byte[] { 01, 00, 00, 00 },
              };

            // ModuleVb01
            var modTypes = new int[] { 0x0a000003 };
            var modParents = new int[] { 0x02000002 };
            var modValues = new byte[][]
            {
                new byte[] { 0x01, 00, 0x10, 0x4d, 0x6f, 0x64, 0x56, 0x42, 0x44, 0x65, 0x66, 0x61, 0x75, 0x6c, 0x74, 0x50, 0x72, 0x6f, 0x70, 00, 00 }
            };

            var reader = GetMetadataReader(Interop.Interop_Mock01);

            int i = 0;
            foreach (var caHandle in reader.CustomAttributes)
            {
                if (i % 5 == 0)
                {
                    var row = reader.GetCustomAttribute(caHandle);
                    Assert.Equal(comTypes[i / 5], row.Constructor.Token);
                    Assert.Equal(comParents[i / 5], row.Parent.Token);

                    var sig = reader.GetBlobBytes(row.Value);
                    var blob = comValues[i / 5];
                    for (int j = 0; j < blob.Length; j++)
                    {
                        Assert.Equal(blob[j], sig[j]);
                    }
                }

                i++;
            }

            Assert.Equal(0x37, i);

            // ====================================================
            reader = GetMetadataReader(NetModule.ModuleVB01, true);

            i = 0;
            foreach (var caHandle in reader.CustomAttributes)
            {
                var row = reader.GetCustomAttribute(caHandle);
                Assert.Equal(modTypes[i], row.Constructor.Token);
                Assert.Equal(modParents[i], row.Parent.Token);

                var sig = reader.GetBlobBytes(row.Value);
                var blob = modValues[i];
                for (int j = 0; j < blob.Length; j++)
                {
                    Assert.Equal(blob[j], sig[j]);
                }

                i++;
            }
        }

        [Fact]
        public void GetCustomAttributes()
        {
            var reader = GetMetadataReader(Interop.Interop_Mock01);

            var attributes1 = reader.GetCustomAttributes(MetadataTokens.EntityHandle(0x02000006));
            AssertEx.Equal(new[] { 0x16, 0x17, 0x18, 0x19 }, attributes1.Select(a => a.RowId));
            Assert.Equal(4, attributes1.Count);

            var attributes2 = reader.GetCustomAttributes(MetadataTokens.EntityHandle(0x02000000));
            AssertEx.Equal(new int[0], attributes2.Select(a => a.RowId));
            Assert.Equal(0, attributes2.Count);
        }

        /// <summary>
        /// MethodSemantics Table
        ///     Semantic (2-byte unsigned)
        ///     Method (RID to method table)
        ///     Association (Token)
        /// </summary>
        [Fact]
        public void ValidateMethodSemanticsTable()
        {
            // ModuleCS01 0x17 - chkec every 5
            var expSems = new ushort[] { 0x10, 0x08, 0x02, 0x10, 0x01, };

            // MethodTable always 0x06000000
            var expMets = new int[] { /*0x6000018*/ 0x19, 0x1a, 0x017, 0x2c, 0x28, };
            var expAsso = new int[] { 0x14000001, 0x14000002, 0x17000003, 0x14000005, 0x17000006, };

            var reader = GetMetadataReader(NetModule.ModuleCS01, true);

            // Validity Rules
            // Assert.Equal((uint)expSems.Length, table1.NumberOfRows);
            for (int i = 0; i < reader.GetTableRowCount(TableIndex.MethodSemantics); i += 5)
            {
                int rid = i + 1;
                Assert.Equal(expSems[i / 5], (uint)reader.MethodSemanticsTable.GetSemantics(rid));
                Assert.Equal(expMets[i / 5], reader.MethodSemanticsTable.GetMethod(rid).RowId);
                Assert.Equal(expAsso[i / 5], reader.MethodSemanticsTable.GetAssociation(rid).Token);
            }
        }

        /// <summary>
        /// StandAloneSigTable Columns: (Prop or return type)
        ///     Name (offset to #String)
        ///     Flags, Sequence (2 byte unsigned)
        ///     Signature (offset to #blob)
        /// </summary>
        [Fact]
        public void ValidateSignature()
        {
            // InteropImpl - 8
            var expSigs = new byte[][]
            {
                new byte[] { 0x07, 01, 0x11, 0x19 }, new byte[] { 0x07, 02, 0x11, 0x1d, 0x11, 0x1d },
                new byte[] { 0x07, 01, 0x0e },  new byte[] { 0x07, 01, 0x1c },
                new byte[] { 0x07, 04, 0x12, 0x31, 0x12, 0x31, 0x12, 0x31, 0x02 },
                new byte[] { 0x07, 04, 0x12, 0x35, 0x12, 0x35, 0x12, 0x35, 0x02 },
                new byte[] { 0x07, 04, 0x12, 0x39, 0x12, 0x39, 0x12, 0x39, 0x02 }, new byte[] { 0x07, 01, 02 }
            };

            // ModuleVB01 - 5
            var modSigs = new byte[][]
            {
                new byte[] { 0x07, 01, 0x0e },
                new byte[] { 0x07, 0x02, 0x11, 0x09, 0x11, 0x09 },
                new byte[] { 0x07, 01, 0x12, 0x35 },
                new byte[] { 0x07, 01, 0x12, 0x15 },
                new byte[] { 0x07, 01, 0x1c }
            };

            var reader = GetMetadataReader(Interop.Interop_Mock01_Impl);

            // Validity Rules
            Assert.Equal(expSigs.Length, reader.StandAloneSigTable.NumberOfRows);
            for (int i = 0; i < reader.GetTableRowCount(TableIndex.StandAloneSig); i++)
            {
                var signature = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(i + 1)).Signature;
                var sig = reader.GetBlobBytes(signature);
                var exp = expSigs[i];
                for (int j = 0; j < exp.Length; j++)
                {
                    Assert.Equal(exp[j], sig[j]);
                }
            }

            // ==============================================================
            reader = GetMetadataReader(NetModule.ModuleVB01, true);

            // Validity Rules
            Assert.Equal(modSigs.Length, reader.StandAloneSigTable.NumberOfRows);
            for (int i = 0; i < reader.GetTableRowCount(TableIndex.StandAloneSig); i++)
            {
                var signature = reader.GetStandaloneSignature(MetadataTokens.StandaloneSignatureHandle(i + 1)).Signature;
                var sig = reader.GetBlobBytes(signature);
                var exp = modSigs[i];
                for (int j = 0; j < exp.Length; j++)
                {
                    Assert.Equal(exp[j], sig[j]);
                }
            }
        }

        /// <summary>
        /// ConstantTable Columns:
        ///     Type (1 byte unsigned)
        ///     Parent (token)
        ///     Value (offset to #blob)
        /// </summary>
        [Fact]
        public void ValidateConstantTable()
        {
            // ModuleCS01 - 9
            var expTypes = new byte[] { 0x12, 0x0b, 0x12, 0x0e, 0x12, 0x0b, 0x09, 0x12, 0x0e };

            var expParent = new int[]
            {
                0x08000001, 0x08000003, 0x08000005, 0x08000006, 0x08000008, 0x0800000a, 0x0800000d, 0x0800000f, 0x08000010,
            };

            var expSigs = new byte[][]
            {
                new byte[] { 0x00, 00, 00, 00 },
                new byte[] { 0xbc, 01, 00, 00, 00, 00, 00, 00 },
                new byte[] { 0x00, 00, 00, 00 },
                new byte[]
                {
                    0x4f, 00, 0x70, 00, 0x74, 00, 0x69, 00, 0x6f, 00, 0x6e, 00, 0x61, 00, 0x6c, 00,
                        0x2e, 00, 0x53, 00, 0x74, 00, 0x72, 00, 0x69, 00, 0x6e, 00, 0x67, 00, 0x2e, 00,
                        0x43, 00, 0x6f, 00, 0x6e, 00, 0x73, 00, 0x74, 00, 0x61, 00, 0x6e, 00, 0x74, 00
                },
                new byte[] { 0x00, 00, 00, 00 },
                new byte[] { 00, 00, 00, 00, 00, 00, 00, 00 },
                new byte[] { 0x00, 00, 00, 00 },
                new byte[] { 0x00, 00, 00, 00 },
                new byte[]
                {
                    0x4f, 00, 0x76, 00, 0x65, 00, 0x72, 00, 0x72, 00, 0x69, 00, 0x64, 00, 0x65, 00,
                    0x20, 00, 0x4f, 00, 0x70, 00, 0x74, 00, 0x69, 00, 0x6f, 00, 0x6e, 00, 0x61, 00,
                    0x6c, 00, 0x2e, 00, 0x53, 00, 0x74, 00, 0x72, 00, 0x69, 00, 0x6e, 00, 0x67, 00
                }
            };

            // ---------------------------------------------------
            // ModuleVB01 - 6
            var modTypes = new byte[] { 0x0e, 0x08, 0x12, 0x08, 0x08, 0x08, };
            var modParent = new int[] { 0x04000001, 0x04000005, 0x08000005, 0x04000006, 0x04000007, 0x04000008, };
            var modSigs = new byte[][]
            {
                new byte[]
                {
                    0x56, 0x00, 0x42, 0x00, 0x20, 0x00, 0x43, 0x00, 0x6f, 0x00, 0x6e, 0x00, 0x73, 0x00, 0x74, 0x00,
                    0x61, 0x00, 0x6e, 0x00, 0x74, 0x00, 0x20, 0x00, 0x53, 0x00, 0x74, 0x00, 0x72, 0x00, 0x69, 0x00,
                    0x6e, 0x00, 0x67, 0x00, 0x20, 0x00, 0x46, 0x00, 0x69, 0x00, 0x65, 0x00, 0x6c, 0x00, 0x64, 0x00
                },
                new byte[] { 0, 0, 0, 0 },
                new byte[] { 0, 0, 0, 0 },
                new byte[] { 1, 0, 0, 0 },
                new byte[] { 2, 0, 0, 0 },
                new byte[] { 3, 0, 0, 0 },
            };

            var reader = GetMetadataReader(NetModule.ModuleCS01, true);

            // Validity Rules
            Assert.Equal(expTypes.Length, reader.GetTableRowCount(TableIndex.Constant));
            int i = 0;
            foreach (var handle in reader.GetConstants())
            {
                Constant constant = reader.GetConstant(handle);

                Assert.Equal(expTypes[i], (byte)constant.TypeCode);
                Assert.Equal(expParent[i], constant.Parent.Token);

                var sig = reader.GetBlobBytes(constant.Value);
                for (int j = 0; j < expSigs[i].Length; j++)
                {
                    Assert.Equal(expSigs[i][j], sig[j]);
                }

                i++;
            }

            // =======================================
            reader = GetMetadataReader(NetModule.ModuleVB01, true);

            // Validity Rules
            Assert.Equal(modTypes.Length, reader.GetTableRowCount(TableIndex.Constant));
            i = 0;
            foreach (var handle in reader.GetConstants())
            {
                Constant constant = reader.GetConstant(handle);

                Assert.Equal(modTypes[i], (byte)constant.TypeCode);
                Assert.Equal(modParent[i], constant.Parent.Token);

                var sig = reader.GetBlobBytes(constant.Value);
                for (int j = 0; j < modSigs[i].Length; j++)
                {
                    Assert.Equal(modSigs[i][j], sig[j]);
                }

                i++;
            }
        }

        /// <summary>
        /// User defined string#:
        ///     Offset
        /// </summary>
        [Fact]
        public void ValidateUserStringStream()
        {
            // AppCS
            var expOffset = new int[] { 1 };
            var expCount = new int[] { 25 };
            var expValues = new string[] { "String Parameter Constant" };

            // ModuleCS01
            var modOffset = new int[] { 1, 0x2f };
            var modCount = new int[] { 22, 24 };
            var modValues = new string[] { "Static String Constant", "Readonly-String_Constant" };

            var reader = GetMetadataReader(NetModule.AppCS);

            for (uint i = 0; i < expOffset.Length; i++)
            {
                var data = reader.GetUserString(UserStringHandle.FromOffset(expOffset[i]));
                Assert.Equal(expCount[i], data.Length);
                Assert.Equal(expValues[i], data);
            }

            // =============================================
            reader = GetMetadataReader(NetModule.ModuleCS01, true);

            for (uint i = 0; i < modOffset.Length; i++)
            {
                var data = reader.GetUserString(UserStringHandle.FromOffset(modOffset[i]));
                Assert.Equal(modCount[i], data.Length);
                Assert.Equal(modValues[i], data);
            }
        }

        [Fact]
        public void EmptyType()
        {
            var reader = GetMetadataReader(Misc.EmptyType);
            var typeDef = reader.GetTypeDefinition(reader.TypeDefinitions.Skip(2).First());

            Assert.Equal("C", reader.GetString(typeDef.Name));

            Assert.Equal(3, reader.TypeDefTable.NumberOfRows);
            Assert.Equal(0, reader.TypeDefTable.GetMethodStart(3));

            var methods = typeDef.GetMethods();
            Assert.Equal(0, methods.Count);
            var e = methods.GetEnumerator();
            Assert.True(e.Current.IsNil);
            Assert.False(e.MoveNext());
            Assert.True(e.Current.IsNil);
        }

        /// <summary>
        /// Import .OBJ file as a netmodule.
        /// </summary>
        [Fact]
        public void Bug17109()
        {
            var reader = GetMetadataReader(Misc.CPPClassLibrary2);

            var typeDef = reader.GetTypeDefinition(reader.TypeDefinitions.First());
            string name = reader.GetString(typeDef.Name);
            var genericParams = typeDef.GetGenericParameters();

            Assert.Equal("<Module>", name);
            Assert.Equal(0, genericParams.Count);

            typeDef = reader.GetTypeDefinition(reader.TypeDefinitions.Skip(1).First());
            name = reader.GetString(typeDef.Name);
            genericParams = typeDef.GetGenericParameters();

            Assert.Equal("Class1", name);
            Assert.Equal(0, genericParams.Count);
        }

        [Fact]
        public void OtherAccessors()
        {
            var reader = GetMetadataReader(Interop.OtherAccessors);
            var typeDef = reader.GetTypeDefinition(reader.TypeDefinitions.First());
            Assert.Equal("<Module>", reader.GetString(typeDef.Name));

            typeDef = reader.GetTypeDefinition(reader.TypeDefinitions.Skip(1).First());
            Assert.Equal("IContainerObject", reader.GetString(typeDef.Name));

            var propertyDef = reader.GetPropertyDefinition(typeDef.GetProperties().First());
            var propertyAccessors = propertyDef.GetAccessors();

            Assert.Equal("get_Value", reader.GetString(reader.GetMethodDefinition(propertyAccessors.Getter).Name));
            Assert.Equal("set_Value", reader.GetString(reader.GetMethodDefinition(propertyAccessors.Setter).Name));
            Assert.Equal("let_Value", reader.GetString(reader.GetMethodDefinition(propertyAccessors.Others.Single()).Name));

            typeDef = reader.GetTypeDefinition(reader.TypeDefinitions.Skip(2).First());
            Assert.Equal("IEventSource", reader.GetString(typeDef.Name));

            var eventDef = reader.GetEventDefinition(typeDef.GetEvents().First());
            var eventAccessors = eventDef.GetAccessors();
            var otherAccessorNames = (from methodHandle in eventAccessors.Others
                                      select reader.GetString(reader.GetMethodDefinition(methodHandle).Name)).ToArray();

            Assert.Equal("add_Notification", reader.GetString(reader.GetMethodDefinition(eventAccessors.Adder).Name));
            Assert.Equal("remove_Notification", reader.GetString(reader.GetMethodDefinition(eventAccessors.Remover).Name));
            Assert.True(eventAccessors.Raiser.IsNil);

            // Note that ilasm doesn't retain the order in which other accessors were specified in IL,
            // so if the DLL resource is rebuilt from IL this test may need to be adjusted.
            Assert.Equal(new[] { "resume_Notification", "other_Notification", "suspend_Notification" }, otherAccessorNames);
        }

        [Fact]
        public void DebugMetadataHeader()
        {
            var pdbBlob = PortablePdbs.DocumentsPdb;
            using (var provider = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(pdbBlob)))
            {
                var reader = provider.GetMetadataReader();

                Assert.Equal(default, reader.DebugMetadataHeader.EntryPoint);
                AssertEx.Equal(new byte[] { 0x89, 0x03, 0x86, 0xAD, 0xFF, 0x27, 0x56, 0x46, 0x9F, 0x3F, 0xE2, 0x18, 0x4B, 0xEF, 0xFC, 0xC0, 0xBE, 0x0C, 0x52, 0xA0 }, reader.DebugMetadataHeader.Id);
                Assert.Equal(0x7c, reader.DebugMetadataHeader.IdStartOffset);

                var slice = pdbBlob.AsSpan(reader.DebugMetadataHeader.IdStartOffset, reader.DebugMetadataHeader.Id.Length);
                AssertEx.Equal(reader.DebugMetadataHeader.Id, slice.ToArray());
            }
        }

        [Fact]
        public void GetMethodDebugInformation_InvalidHandle()
        {
            using (var provider = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(PortablePdbs.DocumentsPdb)))
            {
                var reader = provider.GetMetadataReader();
                var mdi = reader.GetMethodDebugInformation(default(MethodDebugInformationHandle));

                Assert.Throws<BadImageFormatException>(() => mdi.SequencePointsBlob);
                Assert.Throws<BadImageFormatException>(() => mdi.Document);
                Assert.Throws<BadImageFormatException>(() => mdi.LocalSignature);
                Assert.Throws<BadImageFormatException>(() => mdi.GetSequencePoints());
            }
        }

        [Fact]
        public void GetCustomDebugInformation()
        {
            using (var provider = MetadataReaderProvider.FromPortablePdbStream(new MemoryStream(PortablePdbs.DocumentsPdb)))
            {
                var reader = provider.GetMetadataReader();
                var cdi1 = reader.GetCustomAttributes(MetadataTokens.EntityHandle(0x30000001));
                AssertEx.Equal(new int[0], cdi1.Select(a => a.RowId));
                Assert.Equal(0, cdi1.Count);

                var cdi2 = reader.GetCustomAttributes(MetadataTokens.EntityHandle(0x03000000));
                AssertEx.Equal(new int[0], cdi2.Select(a => a.RowId));
                Assert.Equal(0, cdi2.Count);
            }
        }

        [Fact]
        public void MemberCollections_AllMembers()
        {
            var reader = GetMetadataReader(Misc.Members);
            var methodNames = (from m in reader.MethodDefinitions
                               select reader.GetString(reader.GetMethodDefinition(m).Name)).ToArray();

            var fieldNames = (from f in reader.FieldDefinitions
                              select reader.GetString(reader.GetFieldDefinition(f).Name)).ToArray();

            var eventNames = (from e in reader.EventDefinitions
                              select reader.GetString(reader.GetEventDefinition(e).Name)).ToArray();

            var propertyNames = (from p in reader.PropertyDefinitions
                                 select reader.GetString(reader.GetPropertyDefinition(p).Name)).ToArray();

            Assert.Equal(new[] {
                    "MC1",
                    "MC2",
                    "add_EC1",
                    "remove_EC1",
                    "add_EC2",
                    "remove_EC2",
                    "add_EC3",
                    "remove_EC3",
                    ".ctor",
                    "MD1",
                    "get_PE1",
                    "set_PE1",
                    "add_ED1",
                    "remove_ED1",
                    ".ctor",
                    "get_PE1",
                    "set_PE1",
                    "get_PE2",
                    "set_PE2",
                    ".ctor"
            }, methodNames);

            Assert.Equal(new[] {
                    "EC1",
                    "EC2",
                    "EC3",
                    "FD1",
                    "ED1",
                    "FE1",
                    "FE2",
                    "FE3",
                    "FE4",
                }, fieldNames);

            Assert.Equal(new[] {
                    "PE1",
                    "PE1",
                    "PE2",
                }, propertyNames);

            Assert.Equal(new[] {
                    "EC1",
                    "EC2",
                    "EC3",
                    "ED1",
                }, eventNames);
        }

        [Fact]
        public void MemberCollections_TypeMembers_FirstTypeDef()
        {
            var reader = GetMetadataReader(Misc.Members);
            var typeModule = reader.GetTypeDefinition(reader.TypeDefinitions.First());
            Assert.Equal("<Module>", reader.GetString(typeModule.Name));

            var methodNames = (from m in typeModule.GetMethods()
                               select reader.GetString(reader.GetMethodDefinition(m).Name)).ToArray();

            var fieldNames = (from f in typeModule.GetFields()
                              select reader.GetString(reader.GetFieldDefinition(f).Name)).ToArray();

            var eventNames = (from e in typeModule.GetEvents()
                              select reader.GetString(reader.GetEventDefinition(e).Name)).ToArray();

            var propertyNames = (from p in typeModule.GetProperties()
                                 select reader.GetString(reader.GetPropertyDefinition(p).Name)).ToArray();

            Assert.Equal(new string[0], methodNames);
            Assert.Equal(new string[0], fieldNames);
            Assert.Equal(new string[0], propertyNames);
            Assert.Equal(new string[0], eventNames);
        }

        [Fact]
        public void MemberCollections_TypeMembers_MiddleTypeDef()
        {
            var reader = GetMetadataReader(Misc.Members);
            var typeC = reader.GetTypeDefinition(reader.TypeDefinitions.Where(t => reader.GetString(reader.GetTypeDefinition(t).Name) == "C").Single());

            var methodNames = (from m in typeC.GetMethods()
                               select reader.GetString(reader.GetMethodDefinition(m).Name)).ToArray();

            var fieldNames = (from f in typeC.GetFields()
                              select reader.GetString(reader.GetFieldDefinition(f).Name)).ToArray();

            var eventNames = (from e in typeC.GetEvents()
                              select reader.GetString(reader.GetEventDefinition(e).Name)).ToArray();

            var propertyNames = (from p in typeC.GetProperties()
                                 select reader.GetString(reader.GetPropertyDefinition(p).Name)).ToArray();

            Assert.Equal(new[] {
                    "MC1",
                    "MC2",
                    "add_EC1",
                    "remove_EC1",
                    "add_EC2",
                    "remove_EC2",
                    "add_EC3",
                    "remove_EC3",
                    ".ctor",
                }, methodNames);

            Assert.Equal(new[] {
                    "EC1",
                    "EC2",
                    "EC3",
                }, fieldNames);

            Assert.Equal(new string[0], propertyNames);

            Assert.Equal(new[] {
                    "EC1",
                    "EC2",
                    "EC3"
                }, eventNames);
        }

        [Fact]
        public void MemberCollections_TypeMembers_LastTypeDef()
        {
            var reader = GetMetadataReader(Misc.Members);
            var typeE = reader.GetTypeDefinition(reader.TypeDefinitions.Last());
            Assert.Equal("E", reader.GetString(typeE.Name));

            var methodNames = (from m in typeE.GetMethods()
                               select reader.GetString(reader.GetMethodDefinition(m).Name)).ToArray();

            var fieldNames = (from f in typeE.GetFields()
                              select reader.GetString(reader.GetFieldDefinition(f).Name)).ToArray();

            var eventNames = (from e in typeE.GetEvents()
                              select reader.GetString(reader.GetEventDefinition(e).Name)).ToArray();

            var propertyNames = (from p in typeE.GetProperties()
                                 select reader.GetString(reader.GetPropertyDefinition(p).Name)).ToArray();

            Assert.Equal(new[] {
                    "get_PE1",
                    "set_PE1",
                    "get_PE2",
                    "set_PE2",
                    ".ctor"
                }, methodNames);

            Assert.Equal(new[] {
                    "FE1",
                    "FE2",
                    "FE3",
                    "FE4",
                }, fieldNames);

            Assert.Equal(new[] {
                    "PE1",
                    "PE2",
                }, propertyNames);

            Assert.Equal(new string[0], eventNames);
        }

        [Fact]
        public void Handles()
        {
            var assemblyRef = AssemblyReferenceHandle.FromVirtualIndex(0);
            Handle handle = (Handle)assemblyRef;

            Assert.False(assemblyRef.IsNil);
            Assert.False(handle.IsNil);
            Assert.Equal(handle.RowId, assemblyRef.RowId);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsThreadingSupported))]
        public void CanReadFromSameMemoryMappedPEReaderInParallel()
        {
            // See http://roslyn.codeplex.com/workitem/299
            //
            // This simulates the use case where something is holding on
            // to a PEReader and prepared to produce a MetadataReader
            // on demand for callers on different threads.
            //
            using (var stream = GetTemporaryAssemblyLargeEnoughToBeMemoryMapped())
            {
                Assert.InRange(stream.Length, StreamMemoryBlockProvider.MemoryMapThreshold + 1, int.MaxValue);

                for (int i = 0; i < 1000; i++)
                {
                    stream.Position = 0;

                    using (var peReader = new PEReader(stream, PEStreamOptions.LeaveOpen))
                    {
                        Parallel.For(0, 4, _ => { peReader.GetMetadataReader(); });
                    }
                }
            }
        }

        private static FileStream GetTemporaryAssemblyLargeEnoughToBeMemoryMapped()
        {
            var stream = new FileStream(
                Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.Read,
                4096,
                FileOptions.DeleteOnClose);

            using (var testData = new MemoryStream(Misc.Members))
            {
                while (stream.Length <= StreamMemoryBlockProvider.MemoryMapThreshold)
                {
                    testData.CopyTo(stream);
                    testData.Position = 0;
                }
            }

            stream.Position = 0;
            return stream;
        }

        [Fact]
        public unsafe void ExtraDataObfuscation()
        {
            byte[] obfuscated = ObfuscateWithExtraData(Misc.Members);
            fixed (byte* ptr = obfuscated)
            {
                using (var peReader = new PEReader(ptr, obfuscated.Length))
                {
                    MetadataReader mdReader = peReader.GetMetadataReader();
                    ModuleDefinition module = mdReader.GetModuleDefinition();

                    Assert.Equal(0, module.Generation);
                    Assert.Equal("Members.dll", mdReader.GetString(module.Name));
                    Assert.Equal(mdReader.GetGuid(MetadataTokens.GuidHandle(1)), mdReader.GetGuid(module.Mvid));
                }
            }
        }

        [Fact]
        public unsafe void ExtraDataObfuscationWithoutCorrespondingFlag()
        {
            // if, unlike above, we leave the ExtraData heap size flag out but do the rest of the obfuscation,
            // we should fail as we did before we understood the flag.
            byte[] obfuscated = ObfuscateWithExtraData(Misc.Members, setFlag: false);

            fixed (byte* ptr = obfuscated)
            {
                using (var peReader = new PEReader(ptr, obfuscated.Length))
                {
                    MetadataReader mdReader = peReader.GetMetadataReader();
                    ModuleDefinition module = mdReader.GetModuleDefinition();

                    Assert.Equal(0x0000CCCC, module.Generation);
                    Assert.Throws<BadImageFormatException>(() => mdReader.GetString(module.Name));
                    Assert.True(module.Mvid.IsNil);
                }
            }
        }

        private struct StreamHeaderInfo
        {
            public int OffsetToOffset; // offset from PE start to offset field in stream header
            public int Offset; // offset from metadata start to the stream
            public int OffsetToSize; // offset from PE start to size field in stream header
            public int Size; // size of stream
        }

        // Mimic what at least one version of at least one obfuscator has done to use the undocumented/non-standard extra-data flag.
        // If setFlag is false, do everything but setting the flag.
        private static unsafe byte[] ObfuscateWithExtraData(byte[] unobfuscated, bool setFlag = true)
        {
            int offsetToMetadata;
            int offsetToModuleTable;
            int offsetToMetadataSize;
            int tableStreamIndex = -1;
            StreamHeaderInfo[] streamHeaders;

            fixed (byte* ptr = unobfuscated)
            {
                using (var peReader = new PEReader(ptr, unobfuscated.Length))
                {
                    PEMemoryBlock metadata = peReader.GetMetadata();
                    offsetToMetadata = peReader.PEHeaders.MetadataStartOffset;
                    offsetToMetadataSize = peReader.PEHeaders.CorHeaderStartOffset + 12;
                    offsetToModuleTable = offsetToMetadata + peReader.GetMetadataReader().GetTableMetadataOffset(TableIndex.Module);

                    // skip root header
                    BlobReader blobReader = metadata.GetReader();
                    blobReader.ReadUInt32(); // signature
                    blobReader.ReadUInt16(); // major version
                    blobReader.ReadUInt16(); // minor version
                    blobReader.ReadUInt32(); // reserved
                    int versionStringSize = blobReader.ReadInt32();
                    blobReader.Offset += versionStringSize;

                    // read stream headers to collect offsets and sizes to adjust later
                    blobReader.ReadUInt16(); // reserved
                    int streamCount = blobReader.ReadInt16();
                    streamHeaders = new StreamHeaderInfo[streamCount];

                    for (int i = 0; i < streamCount; i++)
                    {
                        streamHeaders[i].OffsetToOffset = offsetToMetadata + blobReader.Offset;
                        streamHeaders[i].Offset = blobReader.ReadInt32();
                        streamHeaders[i].OffsetToSize = offsetToMetadata + blobReader.Offset;
                        streamHeaders[i].Size = blobReader.ReadInt32();

                        string name = blobReader.ReadUtf8NullTerminated();
                        if (name == "#~")
                        {
                            tableStreamIndex = i;
                        }

                        blobReader.Align(4);
                    }
                }
            }

            const int sizeOfExtraData = 4;
            int offsetToTableStream = offsetToMetadata + streamHeaders[tableStreamIndex].Offset;
            int offsetToHeapSizeFlags = offsetToTableStream + 6;

            // copy unobfuscated to obfuscated, leaving room for 4 bytes of data right before the module table.
            byte[] obfuscated = new byte[unobfuscated.Length + sizeOfExtraData];
            Array.Copy(unobfuscated, obfuscated, offsetToModuleTable);
            Array.Copy(unobfuscated, offsetToModuleTable, obfuscated, offsetToModuleTable + sizeOfExtraData, unobfuscated.Length - offsetToModuleTable);

            // increase size of metadata
            Span<byte> MetadataSizeSpan = new Span<byte>(obfuscated, offsetToMetadataSize, 4);
            uint MetadataSize = BinaryPrimitives.ReadUInt32LittleEndian(MetadataSizeSpan);
            BinaryPrimitives.WriteUInt32LittleEndian(MetadataSizeSpan, MetadataSize + sizeOfExtraData);

            // increase size of table stream
            Span<byte> TableStreamSpan = new Span<byte>(obfuscated, streamHeaders[tableStreamIndex].OffsetToSize, 4);
            uint TableStreamSize = BinaryPrimitives.ReadUInt32LittleEndian(TableStreamSpan);
            BinaryPrimitives.WriteUInt32LittleEndian(TableStreamSpan, TableStreamSize + sizeOfExtraData);

            // adjust offset of any streams that follow it
            for (int i = 0; i < streamHeaders.Length; i++)
            {
                if (streamHeaders[i].Offset > streamHeaders[tableStreamIndex].Offset)
                {
                    Span<byte> OffsetSpan = new Span<byte>(obfuscated, streamHeaders[i].OffsetToOffset, 4);
                    uint Offset = BinaryPrimitives.ReadUInt32LittleEndian(OffsetSpan);
                    BinaryPrimitives.WriteUInt32LittleEndian(OffsetSpan, Offset + sizeOfExtraData);
                }
            }

            // write non-zero "extra data" to make sure so that our assertion of leading Module.Generation == 0
            // cannot succeed if extra data is interpreted as the start of the module table.
            for (int i = 0; i < sizeOfExtraData; i++)
            {
                obfuscated[offsetToModuleTable + i] = 0xCC;
            }

            if (setFlag)
            {
                // set the non-standard ExtraData flag indicating that these 4 bytes are present
                obfuscated[offsetToHeapSizeFlags] |= (byte)HeapSizes.ExtraData;
            }

            return obfuscated;
        }

        [Fact]
        public void GetAssemblyName()
        {
            AssertExtensions.Throws<ArgumentNullException>("assemblyFile", () => MetadataReader.GetAssemblyName(null));
            AssertExtensions.Throws<ArgumentException>("path", null, () => MetadataReader.GetAssemblyName(string.Empty));
            Assert.Throws<FileNotFoundException>(() => MetadataReader.GetAssemblyName("IDontExist"));

            using (var tempFile = new TempFile(Path.GetTempFileName(), 0)) // Zero-size file
            {
                Assert.Throws<BadImageFormatException>(() => MetadataReader.GetAssemblyName(tempFile.Path));
            }

            using (var tempFile = new TempFile(Path.GetTempFileName(), 42))
            {
                Assert.Throws<BadImageFormatException>(() => MetadataReader.GetAssemblyName(tempFile.Path));
            }

            if (PlatformDetection.HasAssemblyFiles)
            {
                Assembly a = typeof(MetadataReaderTests).Assembly;
                Assert.Equal(new AssemblyName(a.FullName).ToString(), MetadataReader.GetAssemblyName(AssemblyPathHelper.GetAssemblyLocation(a)).ToString());
            }
        }
    }
}
