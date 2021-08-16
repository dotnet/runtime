// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Collections.Generic;

namespace ILCompiler.IBC
{
    public class IBCDataReader
    {
        // These fields are only valid during a call to Read()
        private BinaryReader reader;
        private bool minified;
        private uint majorVersion;
        private Dictionary<byte, uint> seenFlags = new Dictionary<byte, uint>();
        private IBC.LastTokens lastTokens;

        #region Stream Functions
        private long CurrentPosition
        {
            get
            {
                return reader.BaseStream.Position;
            }
        }

        private void SeekTo(long position)
        {
            reader.BaseStream.Seek(position, SeekOrigin.Begin);
        }
        #endregion

        #region Minify Utilities
        uint ReadTokenWithMemory(ref uint lastToken)
        {
            if (!minified)
            {
                return reader.ReadUInt32();
            }

            uint current = reader.Read7BitEncodedUInt();
            byte highByte = (byte)(current >> 24);
            uint result;

            if (highByte == 0)
            {
                result = current | (lastToken & 0xff000000);
            }
            else if (highByte == 0xff)
            {
                result = current & 0x00ffffff;
            }
            else
            {
                result = current;
            }

            lastToken = result;
            return result;
        }

        uint ReadFlagWithLookup()
        {
            if (!minified)
            {
                return reader.ReadUInt32();
            }

            byte index = reader.ReadByte();
            if (index == 0xff)
            {
                return reader.ReadUInt32();
            }
            else if (seenFlags.ContainsKey(index))
            {
                return seenFlags[index];
            }
            else
            {
                seenFlags[index] = reader.ReadUInt32();
                return seenFlags[index];
            }
        }

        // When V2 IBC data writes the size of a block of data to follow, it
        // includes the size of the "header", i.e. the size field. More lyrically,
        // size includes the size of size. V2 writes size as a four-byte integer,
        // so this is pretty easily done. V3 data writes size as a variable-length
        // 7-bit-encoded integer, which would require speculatively writing the
        // size in order to determine what the size was. To make things much
        // simpler, when the file is minifed, the size of the header is not
        // included--size does not include the size of size.
        private long ReadSizeAndGetStart(out int size)
        {
            long start;
            if (minified)
            {
                size = reader.Read7BitEncodedInt();
                start = CurrentPosition;
            }
            else
            {
                start = CurrentPosition;
                size = reader.ReadInt32();
            }

            return start;
        }

        // These methods make it much easier to write code to read both formats.
        private int ReadSmallInt()
        {
            return minified ? reader.Read7BitEncodedInt() : reader.ReadInt32();
        }

        private uint ReadSmallUInt()
        {
            return minified ? reader.Read7BitEncodedUInt() : reader.ReadUInt32();
        }
        #endregion

        #region File-level Structures
        void ReadHeader(out Guid mvid, out uint majorVersion, out uint minorVersion, out bool partialNGen)
        {
            // minified should always be false here, so ReadSizeAndGetStart would
            // work. However, since the header's size is always written in the
            // same way, there's no harm making that explicit here.

            long startPosition = CurrentPosition;
            int size = reader.ReadInt32();

            uint magicNumber = reader.ReadUInt32();
            if (magicNumber != IBC.Constants.HeaderMagicNumber)
            {
                throw new IBCException("Invalid file header");
            }

            majorVersion = reader.ReadUInt32();
            mvid = reader.ReadGuid();

            // Look for the optional header. Read only the parts of the optional
            // header that are present (if any).

            uint optionalHeaderSize = reader.ReadUInt32();
            uint remaining = optionalHeaderSize - sizeof(int);

            minorVersion = 0;

            if (remaining >= sizeof(uint))
            {
                minorVersion = reader.ReadUInt32();
                remaining -= sizeof(int);
            }

            minified = false;
            partialNGen = false;

            if (remaining >= sizeof(uint))
            {
                var fileFlags = (IBC.Constants.FileFlags)reader.ReadUInt32();
                minified = fileFlags.HasFlag(IBC.Constants.FileFlags.Minified);
                partialNGen = fileFlags.HasFlag(IBC.Constants.FileFlags.PartialNGen);

                if (minified && (majorVersion < IBC.Constants.LowestMajorVersionSupportingMinify))
                {
                    throw new IBCException("Minified flag is set but the file's version should not support it.");
                }
                remaining -= sizeof(uint);
            }

            SeekTo(startPosition + size);

            // Make sure that CompatibleVersion <= ReadVersion <= CurrentVersion
            // (Ngen only pays attention to major version, but ideally even a new
            // minor version should be accompanied by a new version of IBCMerge.)

            if ((majorVersion > IBC.Constants.CurrentMajorVersion) ||
                ((majorVersion == IBC.Constants.CurrentMajorVersion) && (minorVersion > IBC.Constants.CurrentMinorVersion)) ||
                (majorVersion < IBC.Constants.CompatibleMajorVersion) ||
                ((majorVersion == IBC.Constants.CompatibleMajorVersion) && (minorVersion < IBC.Constants.CompatibleMinorVersion)))
            {
                throw new IBCException(String.Format("Version mismatch: {0}.{1} not between {2}.{3} and {4}.{5}",
                    majorVersion,
                    minorVersion,
                    IBC.Constants.CompatibleMajorVersion,
                    IBC.Constants.CompatibleMinorVersion,
                    IBC.Constants.CurrentMajorVersion,
                    IBC.Constants.CurrentMinorVersion));
            }
        }

        void ReadFooter()
        {
            uint endToken = reader.ReadUInt32();
            if (endToken != IBC.Constants.FooterMagicNumber)
            {
                throw new IBCException("Invalid file footer");
            }
        }

        private struct SectionInfo
        {
            public long Offset;
            public long Length;
        }

        Dictionary<SectionFormat, SectionInfo> ReadSectionTable(out long highestValidOffset)
        {
            uint NumberOfEntries = reader.ReadUInt32();
            var result = new Dictionary<SectionFormat, SectionInfo>();

            highestValidOffset = 0;
            for (int i = 0; i < NumberOfEntries; ++i)
            {
                SectionInfo sectionInfo;
                SectionFormat sectionFormat = (SectionFormat)reader.ReadInt32();

                if ((majorVersion == 1) &&
                    (sectionFormat < SectionFormat.UserStringPoolProfilingData))
                {
                    // The ScenarioInfo section was added in V2 and assigned index
                    // zero. Every other section that existed at that point was
                    // bumped up by one.

                    ++sectionFormat;
                }

                sectionInfo.Offset = reader.ReadUInt32();
                sectionInfo.Length = reader.ReadUInt32();

                highestValidOffset = Math.Max(highestValidOffset, sectionInfo.Offset + sectionInfo.Length);

                result.Add(sectionFormat, sectionInfo);
            }

            return result;
        }
        #endregion

        #region Scenario Section
        IBC.ScenarioRunData ReadScenarioRun()
        {
            var result = new IBC.ScenarioRunData();

            result.RunTime = DateTime.FromFileTime(reader.ReadInt64());
            result.Mvid = reader.ReadGuid();

            int commandLineLength = reader.ReadInt32();
            int systemInformationLength = reader.ReadInt32();

            result.CommandLine = reader.ReadEncodedString(commandLineLength);
            result.SystemInformation = reader.ReadEncodedString(systemInformationLength);

            return result;
        }

        IBC.ScenarioData ReadScenario()
        {
            long startPosition = CurrentPosition;

            var result = new IBC.ScenarioData();

            uint size = reader.ReadUInt32();

            result.Id = reader.ReadUInt32();
            result.Mask = reader.ReadUInt32();
            result.Priority = reader.ReadUInt32();

            uint numberOfRuns = reader.ReadUInt32();

            int nameLength = reader.ReadInt32();
            result.Name = reader.ReadEncodedString(nameLength);

            for (int i = 0; i < numberOfRuns; ++i)
            {
                result.Runs.Add(ReadScenarioRun());
            }

            SeekTo(startPosition + size);

            return result;
        }

        List<IBC.ScenarioData> ReadScenarioSection(ref uint totalRuns)
        {
            var result = new List<IBC.ScenarioData>();

            totalRuns = reader.ReadUInt32();
            uint numberOfScenarios = reader.ReadUInt32();

            for (int i = 0; i < numberOfScenarios; ++i)
            {
                result.Add(ReadScenario());
            }

            return result;
        }
        #endregion

        #region Basic Block Section
        void ReadBlocks(List<IBC.BasicBlockData> blockList, uint numberOfBlocks)
        {
            for (int i = 0; i < numberOfBlocks; ++i)
            {
                uint ilOffset = ReadSmallUInt();
                uint hitCount = ReadSmallUInt();

                blockList.Add(new IBC.BasicBlockData { ILOffset = ilOffset, ExecutionCount = hitCount });
            }
        }

        IBC.MethodData ReadMethod()
        {
            var result = new IBC.MethodData();

            int size;
            long startPosition = ReadSizeAndGetStart(out size);

            if (!minified)
            {
                reader.ReadUInt32(); // Number of "detail" items. Ignored.
            }

            result.Token = ReadTokenWithMemory(ref lastTokens.LastMethodToken);
            result.ILSize = ReadSmallUInt();

            if (minified)
            {
                uint firstBlockHitCount = reader.Read7BitEncodedUInt();
                result.BasicBlocks.Add(new IBC.BasicBlockData { ILOffset = 0, ExecutionCount = firstBlockHitCount });
            }

            uint numberOfBlocks = ReadSmallUInt();

            ReadBlocks(result.BasicBlocks, numberOfBlocks);

            SeekTo(startPosition + size); // Usually a no-op

            return result;
        }

        IBC.MethodData ReadV1Method()
        {
            var result = new IBC.MethodData();
            result.ILSize = 0;

            reader.ReadUInt32(); // Header size. Ignored.
            result.Token = reader.ReadUInt32();

            // V1 files stored the size of the block count data in bytes. For each
            // block an offset and a count are stored, both as uints.
            uint blockDataSize = reader.ReadUInt32();
            uint numberOfBlocks = blockDataSize / (2 * sizeof(uint));

            ReadBlocks(result.BasicBlocks, numberOfBlocks);

            return result;
        }

        List<IBC.MethodData> ReadBasicBlockSection(ref uint totalRuns)
        {
            var result = new List<IBC.MethodData>();
            uint numberOfMethods;
            Func<IBC.MethodData> readFn;

            // Unminified V2+ files store the total number of runs in the
            // scenario section. V1 files didn't have a scenario section and
            // stored that number here. Minified files can omit the scenario
            // section, and therefore store the information here again.

            // Note that V1 and minified V3+ files do not store numberOfMethods
            // and totalRuns in the same order. Minified files also add an extra
            // datum, a block count hint, which is used by Ngen.

            if (majorVersion == 1)
            {
                numberOfMethods = reader.ReadUInt32();
                totalRuns = reader.ReadUInt32();
                readFn = (Func<IBC.MethodData>)ReadV1Method;
            }
            else
            {
                if (minified)
                {
                    totalRuns = reader.ReadUInt32();
                    reader.ReadUInt32(); // Block count hint. Ignored here.
                }

                numberOfMethods = reader.ReadUInt32();
                readFn = (Func<IBC.MethodData>)ReadMethod;
            }

            for (int i = 0; i < numberOfMethods; ++i)
            {
                result.Add(readFn());
            }

            return result;
        }
        #endregion

        #region Token Sections
        List<IBC.TokenData> ReadTokenSection(SectionFormat section)
        {
            var result = new List<IBC.TokenData>();

            uint numberOfTokens = reader.ReadUInt32();

            uint LastToken = IBC.Utilities.InitialTokenForSection(section);

            for (int i = 0; i < numberOfTokens; ++i)
            {
                uint token = ReadTokenWithMemory(ref LastToken);
                uint flags = ReadFlagWithLookup();

                // Neither minified nor V1 files stored the scenario mask.
                uint? scenarioMask = null;
                if (!minified && (majorVersion > 1))
                {
                    scenarioMask = reader.ReadUInt32();
                }

                result.Add(new IBC.TokenData { Token = token, Flags = flags, ScenarioMask = scenarioMask });
            }

            return result;
        }
        #endregion

        #region Blob Stream Section
        IBC.BlobEntry.PoolEntry ReadPoolPayload()
        {
            var result = new IBC.BlobEntry.PoolEntry();

            int size = ReadSmallInt();
            result.Data = reader.ReadBytes(size);

            return result;
        }

        // The trimFirstByte parameter is for reading method signatures in V1
        // files, which contain an unneeded one-byte prefix.
        IBC.BlobEntry.SignatureEntry ReadSignaturePayload(bool trimFirstByte)
        {
            var result = new IBC.BlobEntry.SignatureEntry();

            int signatureLength = ReadSmallInt();

            if (trimFirstByte)
            {
                --signatureLength;
                reader.ReadByte();
            }

            result.Signature = reader.ReadBytes(signatureLength);

            return result;
        }

        IBC.BlobEntry.SignatureEntry ReadSignaturePayload()
        {
            return ReadSignaturePayload(false);
        }

        IBC.BlobEntry.ExternalNamespaceEntry ReadExternalNamespacePayload()
        {
            var result = new IBC.BlobEntry.ExternalNamespaceEntry();

            int nameLength = ReadSmallInt();
            result.Name = reader.ReadBytes(nameLength);

            return result;
        }

        IBC.BlobEntry.ExternalTypeEntry ReadExternalTypePayload()
        {
            var result = new IBC.BlobEntry.ExternalTypeEntry();

            result.AssemblyToken = ReadTokenWithMemory(ref lastTokens.LastAssemblyToken);
            result.NestedClassToken = ReadTokenWithMemory(ref lastTokens.LastExternalTypeToken);
            result.NamespaceToken = ReadTokenWithMemory(ref lastTokens.LastExternalNamespaceToken);

            int nameLength = ReadSmallInt();
            result.Name = reader.ReadBytes(nameLength);

            return result;
        }

        IBC.BlobEntry.ExternalSignatureEntry ReadExternalSignaturePayload()
        {
            var result = new IBC.BlobEntry.ExternalSignatureEntry();

            int signatureLength = ReadSmallInt();
            result.Signature = reader.ReadBytes(signatureLength);

            return result;
        }

        IBC.BlobEntry.ExternalMethodEntry ReadExternalMethodPayload()
        {
            var result = new IBC.BlobEntry.ExternalMethodEntry();

            result.ClassToken = ReadTokenWithMemory(ref lastTokens.LastExternalTypeToken);
            result.SignatureToken = ReadTokenWithMemory(ref lastTokens.LastExternalSignatureToken);

            int nameLength = ReadSmallInt();
            result.Name = reader.ReadBytes(nameLength);

            return result;
        }

        private IBC.BlobEntry ReadBlobEntry()
        {
            int size;
            long startPosition = ReadSizeAndGetStart(out size);

            BlobType type = (BlobType)ReadSmallInt();
            uint token = ReadTokenWithMemory(ref lastTokens.LastBlobToken);
            IBC.BlobEntry blob;

            // The "Read...Payload" functions create objects of the right type and
            // populate the type-specific information from the stream. The
            // fields common to all blob entries (read above) are set after
            // the object is returned.

            switch (type)
            {
                case BlobType.MetadataStringPool:
                case BlobType.MetadataGuidPool:
                case BlobType.MetadataBlobPool:
                case BlobType.MetadataUserStringPool:
                    blob = ReadPoolPayload();
                    break;

                case BlobType.ParamTypeSpec:
                case BlobType.ParamMethodSpec:
                    blob = ReadSignaturePayload();
                    break;

                case BlobType.ExternalNamespaceDef:
                    blob = ReadExternalNamespacePayload();
                    break;

                case BlobType.ExternalTypeDef:
                    blob = ReadExternalTypePayload();
                    break;

                case BlobType.ExternalSignatureDef:
                    blob = ReadExternalSignaturePayload();
                    break;

                case BlobType.ExternalMethodDef:
                    blob = ReadExternalMethodPayload();
                    break;

                case BlobType.EndOfBlobStream:
                    blob = new IBC.BlobEntry.EndOfStreamEntry();
                    break;

                default:
                    long read = CurrentPosition - startPosition;
                    byte[] data = reader.ReadBytes((int)size - (int)read);
                    blob = new IBC.BlobEntry.UnknownEntry { Payload = data };
                    break;
            }

            blob.Token = token;
            blob.Type = type;

            SeekTo(startPosition + size);

            return blob;
        }

        private IBC.BlobEntry ReadV1BlobEntry()
        {
            BlobType type = (BlobType)reader.ReadInt32();
            reader.ReadUInt32(); // Flags. Unused here.

            IBC.BlobEntry blob;

            switch (type)
            {
                case BlobType.MetadataStringPool:
                case BlobType.MetadataGuidPool:
                case BlobType.MetadataBlobPool:
                case BlobType.MetadataUserStringPool:
                    blob = ReadPoolPayload();
                    break;

                case BlobType.ParamTypeSpec:
                    blob = ReadSignaturePayload();
                    break;

                case BlobType.ParamMethodSpec:
                    // V1 data used a special prefix to indicate method signatures.
                    // It's not obvious why this was necessary given that the type
                    // field already contains this information; in any event, the
                    // prefix is removed here by passing true.
                    blob = ReadSignaturePayload(true);
                    break;

                case BlobType.EndOfBlobStream:
                    blob = new IBC.BlobEntry.EndOfStreamEntry();
                    break;

                default:
                    throw new IBCException("Unexpected blob type in V1 file.");
            }

            blob.Token = 0; // V1 files didn't store tokens for blob entries.
            blob.Type = type;

            return blob;
        }

        List<IBC.BlobEntry> ReadBlobStreamSection()
        {
            var blobs = new List<IBC.BlobEntry>();
            IBC.BlobEntry blob = null;

            Func<IBC.BlobEntry> readFn =
                (majorVersion == 1) ? (Func<IBC.BlobEntry>)ReadV1BlobEntry :
                                      (Func<IBC.BlobEntry>)ReadBlobEntry;

            do
            {
                blob = readFn();
                blobs.Add(blob);
            }
            while (blob.Type != BlobType.EndOfBlobStream);

            return blobs;
        }
        #endregion

        #region Top Level
        private void IfPresent(Dictionary<SectionFormat, SectionInfo> sectionTable, SectionFormat section, Action f)
        {
            if (sectionTable.ContainsKey(section))
            {
                SectionInfo info = sectionTable[section];
                SeekTo(info.Offset);
                f();
            }
        }

        private IBC.AssemblyData ReadInternal()
        {
            var result = new IBC.AssemblyData();

            ReadHeader(out result.Mvid, out result.FormatMajorVersion, out result.FormatMinorVersion, out result.PartialNGen);

            this.majorVersion = result.FormatMajorVersion;

            long highestValidOffset;

            var sectionTable = ReadSectionTable(out highestValidOffset);

            IfPresent(sectionTable, SectionFormat.ScenarioInfo, () => result.Scenarios = ReadScenarioSection(ref result.TotalNumberOfRuns));
            IfPresent(sectionTable, SectionFormat.BasicBlockInfo, () => result.Methods = ReadBasicBlockSection(ref result.TotalNumberOfRuns));
            foreach (SectionFormat section in IBCData.SectionIterator(IBCData.SectionIteratorKind.TokenFlags))
            {
                IfPresent(sectionTable, section, () => result.Tokens[section] = ReadTokenSection(section));
            }
            IfPresent(sectionTable, SectionFormat.BlobStream, () => result.BlobStream = ReadBlobStreamSection());

            SeekTo(highestValidOffset);
            ReadFooter();

            return result;
        }

        private void Clear()
        {
            this.minified = false;
            this.majorVersion = 0;
            seenFlags.Clear();
            lastTokens = new IBC.LastTokens();
        }

        public IBC.AssemblyData Read(byte[] buffer, ref int pos, out bool minified)
        {
            Clear();
            using (var m = new MemoryStream(buffer, pos, buffer.Length - pos))
            {
                this.reader = new BinaryReader(m);

                var result = ReadInternal();

                pos += (int)CurrentPosition;

                this.reader = null;

                minified = this.minified;

                return result;
            }
        }

        // IBCMerge stores and manipulates blob entries internally as byte arrays
        // mirroring their verbose on-disk format. This allows the serializer to
        // create an object-model representation from a byte array.
        public IBC.BlobEntry BlobEntryFromByteArray(byte[] buffer)
        {
            Clear();
            using (var m = new MemoryStream(buffer))
            {
                this.reader = new BinaryReader(m);

                var result = ReadBlobEntry();

                this.reader = null;

                return result;
            }
        }
        #endregion
    }
}
