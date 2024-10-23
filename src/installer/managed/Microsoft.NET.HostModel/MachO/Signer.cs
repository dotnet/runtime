// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.NET.HostModel.MachO.CodeSign.Blobs;
using Microsoft.NET.HostModel.MachO.Streams;

namespace Microsoft.NET.HostModel.MachO.CodeSign
{
    public static class Signer
    {
        /// <summary>
        /// Determine ideal set of hash types necessary for code signing.
        /// </summary>
        /// <remarks>
        /// macOS 10.11.4 and iOS/tvOS 11 have support for SHA-256. If the minimum platform
        /// version specified in the executable is equal or higher than those we only
        /// generate SHA-256. Otherwise we fallback to generating both SHA-1 and SHA-256.
        /// </remarks>
        private static HashType[] GetHashTypesForObjectFile(MachObjectFile objectFile)
        {
            var buildVersion = objectFile.LoadCommands.OfType<MachBuildVersionBase>().FirstOrDefault();
            if (buildVersion != null)
            {
                switch (buildVersion.Platform)
                {
                    case MachPlatform.MacOS:
                        if (buildVersion.MinimumPlatformVersion >= new Version(10, 11, 4))
                            return new[] { HashType.SHA256 };
                        break;
                    case MachPlatform.IOS:
                    case MachPlatform.TvOS:
                        if (buildVersion.MinimumPlatformVersion.Major >= 11)
                            return new[] { HashType.SHA256 };
                        break;
                }
            }
            return new[] { HashType.SHA1, HashType.SHA256 };
        }

        public static long AdHocSignMachO(Stream machStream, string bundleId)
        {
            ExecutableSegmentFlags executableSegmentFlags = 0;
            var objectFile = MachReader.Read(machStream).Single();
            var codeSignAllocate = new CodeSignAllocate(objectFile);
            var hashTypesPerArch = new Dictionary<(MachCpuType cpuType, uint cpuSubType), HashType[]>();
            var cdBuildersPerArch = new Dictionary<(MachCpuType cpuType, uint cpuSubType), CodeDirectoryBuilder[]>();
            var hashTypes = GetHashTypesForObjectFile(objectFile);
            var cdBuilders = new CodeDirectoryBuilder[hashTypes.Length];

            hashTypesPerArch.Add((objectFile.CpuType, objectFile.CpuSubType), hashTypes);
            cdBuildersPerArch.Add((objectFile.CpuType, objectFile.CpuSubType), cdBuilders);

            long signatureSizeEstimate = 0x1000;
            for (int i = 0; i < hashTypes.Length; i++)
            {
                cdBuilders[i] = new CodeDirectoryBuilder(objectFile, bundleId)
                {
                    HashType = hashTypes[i],
                };

                var requirementsData = new byte[12];
                new EmptyRequirementsBlob().WriteBigEndian(requirementsData, out _);
                cdBuilders[i].SetSpecialSlotData(CodeDirectorySpecialSlot.Requirements, requirementsData);

                cdBuilders[i].ExecutableSegmentFlags |= executableSegmentFlags;
                cdBuilders[i].Flags |= CodeDirectoryFlags.Adhoc;

                signatureSizeEstimate += cdBuilders[i].Size(CodeDirectoryVersion.HighestVersion);
            }

            var codeSignatureCommand = objectFile.LoadCommands.OfType<MachCodeSignature>().FirstOrDefault();
            if (codeSignatureCommand == null ||
                codeSignatureCommand.FileSize < signatureSizeEstimate)
            {
                codeSignatureCommand = codeSignAllocate.SetCodeSignatureSize((uint)signatureSizeEstimate);
            }
            codeSignAllocate.AllocateSpace();

            using (var tmpStream = new MemoryStream())
            {
                MachWriter.Write(tmpStream, objectFile);
                tmpStream.Position = 0;
                machStream.Position = 0;
                tmpStream.CopyTo(machStream);
            }

            var blobs = new List<(CodeDirectorySpecialSlot Slot, byte[] Data)>();
            var codeDirectory = cdBuilders[0].Build(objectFile.GetOriginalStream());

            blobs.Add((CodeDirectorySpecialSlot.CodeDirectory, codeDirectory));

            var requirementsBlob = new byte[12];
            new EmptyRequirementsBlob().WriteBigEndian(requirementsBlob, out _);
            blobs.Add((CodeDirectorySpecialSlot.Requirements, requirementsBlob));

            var cdHashes = new byte[hashTypes.Length][];
            var hasher = hashTypes[0].GetIncrementalHash();
            hasher.AppendData(codeDirectory);
            cdHashes[0] = hasher.GetHashAndReset();
            for (int i = 1; i < hashTypes.Length; i++)
            {
                byte[] alternativeCodeDirectory = cdBuilders[i].Build(objectFile.GetOriginalStream());
                blobs.Add((CodeDirectorySpecialSlot.AlternativeCodeDirectory + i - 1, alternativeCodeDirectory));
                hasher = hashTypes[i].GetIncrementalHash();
                hasher.AppendData(alternativeCodeDirectory);
                cdHashes[i] = hasher.GetHashAndReset();
            }

            var cmsWrapperBlob = CmsWrapperBlob.Create(
                hashTypes,
                cdHashes);
            blobs.Add((CodeDirectorySpecialSlot.CmsWrapper, cmsWrapperBlob));

            // TODO: Hic sunt leones (all code below)
            long size = blobs.Sum(b => b.Data != null ? b.Data.Length + 8 : 0);

            var embeddedSignatureBuffer = new byte[12 + (blobs.Count * 8)];
            BinaryPrimitives.WriteUInt32BigEndian(embeddedSignatureBuffer.AsSpan(0, 4), (uint)BlobMagic.EmbeddedSignature);
            BinaryPrimitives.WriteUInt32BigEndian(embeddedSignatureBuffer.AsSpan(4, 4), (uint)(12 + size));
            BinaryPrimitives.WriteUInt32BigEndian(embeddedSignatureBuffer.AsSpan(8, 4), (uint)blobs.Count);
            int bufferOffset = 12;
            int dataOffset = embeddedSignatureBuffer.Length;
            foreach (var (slot, data) in blobs)
            {
                BinaryPrimitives.WriteUInt32BigEndian(embeddedSignatureBuffer.AsSpan(bufferOffset, 4), (uint)slot);
                BinaryPrimitives.WriteUInt32BigEndian(embeddedSignatureBuffer.AsSpan(bufferOffset + 4, 4), (uint)dataOffset);
                dataOffset += data.Length;
                bufferOffset += 8;
            }
            uint codeSignatureSize = codeSignatureCommand.FileSize;
            using var codeSignatureStream = codeSignatureCommand.Data.GetWriteStream();
            codeSignatureStream.Write(embeddedSignatureBuffer);
            foreach (var (slot, data) in blobs)
            {
                codeSignatureStream.Write(data);
            }
            codeSignatureStream.WritePadding(codeSignatureSize - codeSignatureStream.Position);

            using (MemoryStream tmpStream = new())
            {
                MachWriter.Write(tmpStream, objectFile);
                tmpStream.Position = 0;
                machStream.Position = 0;
                tmpStream.CopyTo(machStream);
            }
            return ((long)codeSignatureCommand.FileOffset) + codeSignatureSize;
        }

        public static long AdHocSign(Stream stream, string identifier)
        {
            if (!MachReader.IsMachOImage(stream))
            {
                throw new ArgumentException("Stream does not contain a Mach-O image");
            }

            return AdHocSignMachO(stream, identifier);
        }

        internal static void AdHocSign(string executablePath)
        {
            var attributes = File.GetAttributes(executablePath);

            if (attributes.HasFlag(FileAttributes.Directory))
            {
                throw new NotImplementedException("Signing bundles is not yet supported");
            }

            var bundleIdentifier = Path.GetFileName(executablePath);
            using (FileStream inputFile = File.Open(executablePath, FileMode.Open, FileAccess.ReadWrite))
            {
                long newSize = AdHocSignMachO(inputFile, bundleIdentifier);
                inputFile.SetLength(newSize);
            }
        }

        internal static bool TryRemoveCodesign(Stream inputStream, out long newSize)
        {
            newSize = inputStream.Length;
            inputStream.Position = 0;
            if (!MachReader.IsMachOImage(inputStream))
            {
                return false;
            }
            MachObjectFile objectFile = MachReader.Read(inputStream).Single();

            MachCodeSignature codeSignature = null;
            MachSegment linkEditSegment = null;
            foreach (var loadCommand in objectFile.LoadCommands)
            {
                if (loadCommand is MachCodeSignature lccs)
                {
                    codeSignature = lccs;
                }
                if (loadCommand is MachSegment segment && segment.IsLinkEditSegment)
                {
                    linkEditSegment = segment;
                }
            }
            if (codeSignature is null)
            {
                return false;
            }

            objectFile.LoadCommands.Remove(codeSignature);
            linkEditSegment.FileSize = codeSignature.FileOffset - linkEditSegment.FileOffset;

            newSize = (long)objectFile.GetSigningLimit();
            using (var outputStream = new MemoryStream())
            {
                outputStream.Position = 0;
                MachWriter.Write(outputStream, objectFile);
                outputStream.Position = 0;
                inputStream.Position = 0;
                outputStream.CopyTo(inputStream);
            }

            return true;
        }

        /// <summary>
        /// Removes the code signature from the Mach-O file on disk.
        /// </summary>
        internal static bool TryRemoveCodesign(string inputPath, string outputPath = null)
        {
            if (outputPath is not null)
            {
                File.Copy(inputPath, outputPath, true);
            }
            outputPath ??= inputPath;
            bool removed;
            using (FileStream machStream = File.Open(outputPath, FileMode.Open, FileAccess.ReadWrite))
            {
                removed = TryRemoveCodesign(machStream, out long newSize);
                machStream.SetLength(newSize);
            }
            return removed;
        }
    }
}
