// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using Melanzana.CodeSign.Blobs;
using Melanzana.MachO;
using Melanzana.Streams;

namespace Melanzana.CodeSign
{
    public class Signer
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

        private static void AdHocSignMachO(string executablePath)
        {
            var bundleIdentifier = Path.GetFileName(executablePath);

            var hashTypesPerArch = new Dictionary<(MachCpuType cpuType, uint cpuSubType), HashType[]>();
            var cdBuildersPerArch = new Dictionary<(MachCpuType cpuType, uint cpuSubType), CodeDirectoryBuilder[]>();

            ExecutableSegmentFlags executableSegmentFlags = 0;

            List<MachObjectFile> objectFiles;
            CodeSignAllocate codeSignAllocate;
            string tempFileName;
            using (FileStream inputFile = File.Open(executablePath, FileMode.Open, FileAccess.Read))
            {
                objectFiles = MachReader.Read(inputFile).ToList();
                codeSignAllocate = new CodeSignAllocate(objectFiles);
                foreach (var objectFile in objectFiles)
                {
                    var hashTypes = GetHashTypesForObjectFile(objectFile);
                    var cdBuilders = new CodeDirectoryBuilder[hashTypes.Length];

                    hashTypesPerArch.Add((objectFile.CpuType, objectFile.CpuSubType), hashTypes);
                    cdBuildersPerArch.Add((objectFile.CpuType, objectFile.CpuSubType), cdBuilders);

                    long signatureSizeEstimate = 18000; // Blob Wrapper (CMS)
                    for (int i = 0; i < hashTypes.Length; i++)
                    {
                        cdBuilders[i] = new CodeDirectoryBuilder(objectFile, bundleIdentifier)
                        {
                            HashType = hashTypes[i],
                        };

                        cdBuilders[i].ExecutableSegmentFlags |= executableSegmentFlags;

                        cdBuilders[i].Flags |= CodeDirectoryFlags.Adhoc;

                        signatureSizeEstimate += cdBuilders[i].Size(CodeDirectoryVersion.HighestVersion);
                    }

                    var codeSignatureCommand = objectFile.LoadCommands.OfType<MachCodeSignature>().FirstOrDefault();
                    if (codeSignatureCommand == null ||
                        codeSignatureCommand.FileSize < signatureSizeEstimate)
                    {
                        codeSignAllocate.SetArchSize(objectFile, (uint)signatureSizeEstimate);
                    }
                }
                tempFileName = codeSignAllocate.Allocate();
            }


            // Re-read the object files
            using (FileStream tempFileStream = File.Open(tempFileName, FileMode.Open, FileAccess.ReadWrite))
            {

                objectFiles = MachReader.Read(tempFileStream).ToList();
                foreach (var objectFile in objectFiles)
                {
                    var hashTypes = hashTypesPerArch[(objectFile.CpuType, objectFile.CpuSubType)];
                    var cdBuilders = cdBuildersPerArch[(objectFile.CpuType, objectFile.CpuSubType)];

                    var blobs = new List<(CodeDirectorySpecialSlot Slot, byte[] Data)>();
                    var codeDirectory = cdBuilders[0].Build(objectFile.GetOriginalStream());

                    blobs.Add((CodeDirectorySpecialSlot.CodeDirectory, codeDirectory));

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
                        codeDirectory,
                        hashTypes,
                        cdHashes);
                    blobs.Add((CodeDirectorySpecialSlot.CmsWrapper, cmsWrapperBlob));

                    /// TODO: Hic sunt leones (all code below)

                    // Rewrite LC_CODESIGNATURE data
                    var codeSignatureCommand = objectFile.LoadCommands.OfType<MachCodeSignature>().First();

                    // FIXME: Adjust the size to match LinkEdit section?
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
                }

                using (var outputFile = File.OpenWrite(executablePath))
                    MachWriter.Write(outputFile, objectFiles.Single());
            }
        }

        public static void AdHocSign(string path)
        {
            var attributes = File.GetAttributes(path);

            // Assume a directory is a bundle
            if (attributes.HasFlag(FileAttributes.Directory))
            {
                throw new NotImplementedException("Signing bundles is not yet supported");
            }
            AdHocSignMachO(path);
        }

        public static bool TryRemoveCodesign(Stream inputStream, Stream outputStream)
        {
            inputStream.Position = 0;
            if (!MachReader.IsMachOImage(inputStream))
            {
                return false;
            }
            MachObjectFile objectFile = MachReader.Read(inputStream).Single();

            MachCodeSignature? codeSignature = objectFile.LoadCommands.OfType<MachCodeSignature>().FirstOrDefault();
            if (codeSignature is null)
            {
                return false;
            }
            objectFile!.LoadCommands.Remove(codeSignature!);
            outputStream.Position = 0;
            MachWriter.Write(outputStream, objectFile);
            outputStream.SetLength(outputStream.Position);
            return true;
        }

        /// <summary>
        /// Removes the code signature from the Mach-O file on disk.
        /// </summary>
        public static bool TryRemoveCodesign(string inputPath, string? outputPath = null)
        {
            outputPath ??= inputPath;

            using FileStream inputStream = File.Open(inputPath, FileMode.Open, FileAccess.ReadWrite);
            if (inputPath == outputPath)
            {
                return TryRemoveCodesign(inputStream, inputStream);
            }
            using FileStream outputStream = File.Open(outputPath, FileMode.Create, FileAccess.Write);
            return TryRemoveCodesign(inputStream, outputStream);
        }
    }
}
