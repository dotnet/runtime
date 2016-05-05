// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

namespace System.Reflection.PortableExecutable
{
    public class ManagedPEBuilder : PEBuilder
    {
        private const string TextSectionName = ".text";
        private const string ResourceSectionName = ".rsrc";
        private const string RelocationSectionName = ".reloc";

        private readonly PEDirectoriesBuilder _peDirectoriesBuilder;
        private readonly TypeSystemMetadataSerializer _metadataSerializer;
        private readonly BlobBuilder _ilStream;
        private readonly BlobBuilder _mappedFieldData;
        private readonly BlobBuilder _managedResourceData;
        private readonly Action<BlobBuilder, SectionLocation> _nativeResourceSectionSerializerOpt;
        private readonly int _strongNameSignatureSize;
        private readonly MethodDefinitionHandle _entryPoint;
        private readonly string _pdbPathOpt;
        private readonly ContentId _nativePdbContentId;
        private readonly ContentId _portablePdbContentId;
        private readonly CorFlags _corFlags;
       
        private int _lazyEntryPointAddress;
        private Blob _lazyStrongNameSignature;

        public ManagedPEBuilder(
            PEHeaderBuilder header,
            TypeSystemMetadataSerializer metadataSerializer,
            BlobBuilder ilStream,
            BlobBuilder mappedFieldData,
            BlobBuilder managedResourceData,
            Action<BlobBuilder, SectionLocation> nativeResourceSectionSerializer, // opt
            int strongNameSignatureSize,
            MethodDefinitionHandle entryPoint,
            string pdbPathOpt, // TODO: DebugTableBuilder
            ContentId nativePdbContentId, // TODO: DebugTableBuilder
            ContentId portablePdbContentId, // TODO: DebugTableBuilder
            CorFlags corFlags, 
            Func<IEnumerable<Blob>, ContentId> deterministicIdProvider = null)
            : base(header, deterministicIdProvider)
        {
            _metadataSerializer = metadataSerializer;
            _ilStream = ilStream;
            _mappedFieldData = mappedFieldData;
            _managedResourceData = managedResourceData;
            _nativeResourceSectionSerializerOpt = nativeResourceSectionSerializer;
            _strongNameSignatureSize = strongNameSignatureSize;
            _entryPoint = entryPoint;
            _pdbPathOpt = pdbPathOpt;
            _nativePdbContentId = nativePdbContentId;
            _portablePdbContentId = portablePdbContentId;
            _corFlags = corFlags;

            _peDirectoriesBuilder = new PEDirectoriesBuilder();
        }

        protected override ImmutableArray<Section> CreateSections()
        {
            var builder = ImmutableArray.CreateBuilder<Section>(3);
            builder.Add(new Section(TextSectionName, SectionCharacteristics.MemRead | SectionCharacteristics.MemExecute | SectionCharacteristics.ContainsCode));

            if (_nativeResourceSectionSerializerOpt != null)
            {
                builder.Add(new Section(ResourceSectionName, SectionCharacteristics.MemRead | SectionCharacteristics.ContainsInitializedData));
            }

            if (Header.Machine == Machine.I386 || Header.Machine == 0)
            {
                builder.Add(new Section(RelocationSectionName, SectionCharacteristics.MemRead | SectionCharacteristics.MemDiscardable | SectionCharacteristics.ContainsInitializedData));
            }

            return builder.ToImmutable();
        }

        protected override BlobBuilder SerializeSection(string name, SectionLocation location)
        {
            switch (name)
            {
                case TextSectionName:
                    return SerializeTextSection(location);

                case ResourceSectionName:
                    return SerializeResourceSection(location);

                case RelocationSectionName:
                    return SerializeRelocationSection(location);

                default:
                    throw new ArgumentException(SR.Format(SR.UnknownSectionName, name), nameof(name));
            }
        }

        private BlobBuilder SerializeTextSection(SectionLocation location)
        {
            var sectionBuilder = new BlobBuilder();
            var metadataBuilder = new BlobBuilder();

            var metadataSizes = _metadataSerializer.MetadataSizes;

            var textSection = new ManagedTextSection(
                metadataSizes.MetadataSize,
                ilStreamSize: _ilStream.Count,
                mappedFieldDataSize: _mappedFieldData.Count,
                resourceDataSize: _managedResourceData.Count,
                strongNameSignatureSize: _strongNameSignatureSize,
                imageCharacteristics: Header.ImageCharacteristics,
                machine: Header.Machine,
                pdbPathOpt: _pdbPathOpt,
                isDeterministic: IsDeterministic);

            int methodBodyStreamRva = location.RelativeVirtualAddress + textSection.OffsetToILStream;
            int mappedFieldDataStreamRva = location.RelativeVirtualAddress + textSection.CalculateOffsetToMappedFieldDataStream();
            _metadataSerializer.SerializeMetadata(metadataBuilder, methodBodyStreamRva, mappedFieldDataStreamRva);

            BlobBuilder debugTableBuilderOpt;
            if (_pdbPathOpt != null || IsDeterministic)
            {
                debugTableBuilderOpt = new BlobBuilder();
                textSection.WriteDebugTable(debugTableBuilderOpt, location, _nativePdbContentId, _portablePdbContentId);
            }
            else
            {
                debugTableBuilderOpt = null;
            }

            _lazyEntryPointAddress = textSection.GetEntryPointAddress(location.RelativeVirtualAddress);

            textSection.Serialize(
                sectionBuilder,
                location.RelativeVirtualAddress,
                _entryPoint.IsNil ? 0 : MetadataTokens.GetToken(_entryPoint),
                _corFlags,
                Header.ImageBase,
                metadataBuilder,
                _ilStream,
                _mappedFieldData,
                _managedResourceData,
                debugTableBuilderOpt,
                out _lazyStrongNameSignature);

            _peDirectoriesBuilder.AddressOfEntryPoint = _lazyEntryPointAddress;
            _peDirectoriesBuilder.DebugTable = textSection.GetDebugDirectoryEntry(location.RelativeVirtualAddress);
            _peDirectoriesBuilder.ImportAddressTable = textSection.GetImportAddressTableDirectoryEntry(location.RelativeVirtualAddress);
            _peDirectoriesBuilder.ImportTable = textSection.GetImportTableDirectoryEntry(location.RelativeVirtualAddress);
            _peDirectoriesBuilder.CorHeaderTable = textSection.GetCorHeaderDirectoryEntry(location.RelativeVirtualAddress);
            
            return sectionBuilder;
        }

        private BlobBuilder SerializeResourceSection(SectionLocation location)
        {
            var sectionBuilder = new BlobBuilder();
            _nativeResourceSectionSerializerOpt(sectionBuilder, location);

            _peDirectoriesBuilder.ResourceTable = new DirectoryEntry(location.RelativeVirtualAddress, sectionBuilder.Count);
            return sectionBuilder;
        }

        private BlobBuilder SerializeRelocationSection(SectionLocation location)
        {
            var sectionBuilder = new BlobBuilder();
            WriteRelocationSection(sectionBuilder, Header.Machine, _lazyEntryPointAddress);

            _peDirectoriesBuilder.BaseRelocationTable = new DirectoryEntry(location.RelativeVirtualAddress, sectionBuilder.Count);
            return sectionBuilder;
        }

        private static void WriteRelocationSection(BlobBuilder builder, Machine machine, int entryPointAddress)
        {
            Debug.Assert(builder.Count == 0);

            builder.WriteUInt32((((uint)entryPointAddress + 2) / 0x1000) * 0x1000);
            builder.WriteUInt32((machine == Machine.IA64) ? 14u : 12u);
            uint offsetWithinPage = ((uint)entryPointAddress + 2) % 0x1000;
            uint relocType = (machine == Machine.Amd64 || machine == Machine.IA64) ? 10u : 3u;
            ushort s = (ushort)((relocType << 12) | offsetWithinPage);
            builder.WriteUInt16(s);
            if (machine == Machine.IA64)
            {
                builder.WriteUInt32(relocType << 12);
            }

            builder.WriteUInt16(0); // next chunk's RVA
        }

        protected internal override PEDirectoriesBuilder GetDirectories()
        {
            return _peDirectoriesBuilder;
        }

        private IEnumerable<Blob> GetContentToSign(BlobBuilder peImage)
        {
            // Signed content includes 
            // - PE header without its alignment padding
            // - all sections including their alignment padding and excluding strong name signature blob

            int remainingHeader = Header.ComputeSizeOfPeHeaders(Sections.Length);
            foreach (var blob in peImage.GetBlobs())
            {
                if (remainingHeader > 0)
                {
                    int length = Math.Min(remainingHeader, blob.Length);
                    yield return new Blob(blob.Buffer, blob.Start, length);
                    remainingHeader -= length;
                }
                else if (blob.Buffer == _lazyStrongNameSignature.Buffer)
                {
                    yield return new Blob(blob.Buffer, blob.Start, _lazyStrongNameSignature.Start - blob.Start);
                    yield return new Blob(blob.Buffer, _lazyStrongNameSignature.Start + _lazyStrongNameSignature.Length, blob.Length - _lazyStrongNameSignature.Length);
                }
                else
                {
                    yield return new Blob(blob.Buffer, blob.Start, blob.Length);
                }
            }
        }

        public void Sign(BlobBuilder peImage, Func<IEnumerable<Blob>, byte[]> signatureProvider)
        {
            if (peImage == null)
            {
                throw new ArgumentNullException(nameof(peImage));
            }

            if (signatureProvider == null)
            {
                throw new ArgumentNullException(nameof(signatureProvider));
            }

            var content = GetContentToSign(peImage);
            byte[] signature = signatureProvider(content);

            // signature may be shorter (the rest of the reserved space is padding):
            if (signature == null || signature.Length > _lazyStrongNameSignature.Length)
            {
                throw new InvalidOperationException(SR.SignatureProviderReturnedInvalidSignature);
            }

            // TODO: Native csc also calculates and fills checksum in the PE header
            // Using MapFileAndCheckSum() from imagehlp.dll.

            var writer = new BlobWriter(_lazyStrongNameSignature);
            writer.WriteBytes(signature);
        }
    }
}
