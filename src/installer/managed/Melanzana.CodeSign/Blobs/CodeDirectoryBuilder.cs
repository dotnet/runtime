using System.Buffers.Binary;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Melanzana.MachO;
using Melanzana.Streams;

namespace Melanzana.CodeSign.Blobs
{
    class CodeDirectoryBuilder
    {
        private readonly MachObjectFile executable;
        private readonly string identifier;
        private readonly string teamId;
        private readonly uint pageSize = 4096;
        private readonly byte[][] specialSlots = new byte[7][];
        private int specialSlotCount;

        public CodeDirectoryBuilder(MachObjectFile executable, string identifier, string teamId)
        {
            this.executable = executable;
            this.identifier = identifier;
            this.teamId = teamId;

            if (executable.FileType == MachFileType.Execute)
                ExecutableSegmentFlags |= ExecutableSegmentFlags.MainBinary;
        }

        public void SetSpecialSlotData(CodeDirectorySpecialSlot slot, byte[] data)
        {
            Debug.Assert((int)slot >= 1 && (int)slot <= specialSlots.Length);
            specialSlots[(int)(slot - 1)] = data;
            specialSlotCount = Math.Max(specialSlotCount, (int)slot);
        }

        public HashType HashType { get; set; } = HashType.SHA256;

        public ExecutableSegmentFlags ExecutableSegmentFlags { get; set; }

        public CodeDirectoryFlags Flags { get; set; }

        private static int GetFixedHeaderSize(CodeDirectoryVersion version)
        {
            int size = CodeDirectoryBaselineHeader.BinarySize;
            if (version >= CodeDirectoryVersion.SupportsScatter)
                size += CodeDirectoryScatterHeader.BinarySize;
            if (version >= CodeDirectoryVersion.SupportsTeamId)
                size += CodeDirectoryTeamIdHeader.BinarySize;
            if (version >= CodeDirectoryVersion.SupportsCodeLimit64)
                size += CodeDirectoryCodeLimit64Header.BinarySize;
            if (version >= CodeDirectoryVersion.SupportsExecSegment)
                size += CodeDirectoryExecSegmentHeader.BinarySize;
            if (version >= CodeDirectoryVersion.SupportsPreEncrypt)
                size += CodeDirectoryPreencryptHeader.BinarySize;
            return size;
        }

        public long Size(CodeDirectoryVersion version)
        {
            ulong execLength = executable.GetSigningLimit();
            uint codeSlotCount = (uint)((execLength + pageSize - 1) / pageSize);
            byte hashSize = HashType.GetSize();

            byte[] utf8Identifier = Encoding.UTF8.GetBytes(identifier);
            byte[] utf8TeamId = Encoding.UTF8.GetBytes(teamId);

            long codeDirectorySize = GetFixedHeaderSize(version);
            codeDirectorySize += utf8Identifier.Length + 1;
            if (!String.IsNullOrEmpty(teamId))
                codeDirectorySize += utf8TeamId.Length + 1;
            codeDirectorySize += (specialSlotCount + codeSlotCount) * hashSize;
            if (version >= CodeDirectoryVersion.SupportsPreEncrypt)
                codeDirectorySize += codeSlotCount * hashSize;

            return codeDirectorySize;
        }

        public byte[] Build(Stream machOStream)
        {
            CodeDirectoryVersion version = CodeDirectoryVersion.HighestVersion;

            ulong execLength = executable.GetSigningLimit();
            uint codeSlotCount = (uint)((execLength + pageSize - 1) / pageSize);
            byte[] utf8Identifier = Encoding.UTF8.GetBytes(identifier);
            byte[] utf8TeamId = Encoding.UTF8.GetBytes(teamId);
            byte hashSize = HashType.GetSize();
            var size = Size(version);
            byte[] blobBuffer = new byte[size];

            var textSegment = executable.LoadCommands.OfType<MachSegment>().First(s => s.Name == "__TEXT");
            Debug.Assert(textSegment != null);

            var baselineHeader = new CodeDirectoryBaselineHeader();
            baselineHeader.Magic = BlobMagic.CodeDirectory;
            baselineHeader.Size = (uint)size;
            baselineHeader.Version = version;
            baselineHeader.Flags = Flags;
            // baselineHeader.HashesOffset = 0; -- Filled in later
            // baselineHeader.IdentifierOffset = 0; -- Filled in later
            baselineHeader.SpecialSlotCount = (uint)specialSlotCount;
            baselineHeader.CodeSlotCount = codeSlotCount;
            baselineHeader.ExecutableLength = (execLength > uint.MaxValue) ? uint.MaxValue : (uint)execLength;
            baselineHeader.HashSize = hashSize;
            baselineHeader.HashType = HashType;
            baselineHeader.Platform = 0; // TODO
            baselineHeader.Log2PageSize = (byte)Math.Log2(pageSize);
            baselineHeader.Reserved = 0;
            var scatterHeader = new CodeDirectoryScatterHeader();
            scatterHeader.ScatterOffset = 0;
            var teamIdHeader = new CodeDirectoryTeamIdHeader();
            // teamIdHeader.TeamIdOffset = 0; -- Filled in later
            var codeLimit64Header = new CodeDirectoryCodeLimit64Header();
            codeLimit64Header.Reserved = 0;
            codeLimit64Header.CodeLimit64 = execLength >= uint.MaxValue ? execLength : 0;
            var execSegementHeader = new CodeDirectoryExecSegmentHeader();
            execSegementHeader.Base = textSegment.FileOffset;
            execSegementHeader.Limit = textSegment.FileSize;
            execSegementHeader.Flags = ExecutableSegmentFlags;

            // Fill in flexible fields
            int flexibleOffset = GetFixedHeaderSize(version);

            if (version >= CodeDirectoryVersion.SupportsScatter)
            {
                // TODO
            }

            // Identifier
            baselineHeader.IdentifierOffset = (uint)flexibleOffset;
            utf8Identifier.AsSpan().CopyTo(blobBuffer.AsSpan(flexibleOffset, utf8Identifier.Length));
            flexibleOffset += utf8Identifier.Length + 1;

            // Team ID
            if (version >= CodeDirectoryVersion.SupportsTeamId && !string.IsNullOrEmpty(teamId))
            {
                teamIdHeader.TeamIdOffset = (uint)flexibleOffset;
                utf8TeamId.AsSpan().CopyTo(blobBuffer.AsSpan(flexibleOffset, utf8TeamId.Length));
                flexibleOffset += utf8TeamId.Length + 1;
            }

            // Pre-encrypt hashes
            if (version >= CodeDirectoryVersion.SupportsPreEncrypt)
            {
                // TODO
            }

            var hasher = HashType.GetIncrementalHash();

            // Special slot hashes
            for (int i = specialSlotCount - 1; i >= 0; i--)
            {
                if (specialSlots[i] != null)
                {
                    hasher.AppendData(specialSlots[i]);
                    hasher.GetHashAndReset().CopyTo(blobBuffer.AsSpan(flexibleOffset, hashSize));
                }
                flexibleOffset += hashSize;
            }

            baselineHeader.HashesOffset = (uint)flexibleOffset;

            // Code hashes
            Span<byte> buffer = stackalloc byte[(int)pageSize];
            long remaining = (long)execLength;
            while (remaining > 0)
            {
                int codePageSize = (int)Math.Min(remaining, 4096);
                machOStream.ReadFully(buffer.Slice(0, codePageSize));
                hasher.AppendData(buffer.Slice(0, codePageSize));
                hasher.GetHashAndReset().CopyTo(blobBuffer.AsSpan(flexibleOffset, hashSize));
                remaining -= codePageSize;
                flexibleOffset += hashSize;
            }

            Debug.Assert(flexibleOffset == blobBuffer.Length);

            // Write headers
            int writeOffset = 0;
            baselineHeader.Write(blobBuffer, out var bytesWritten);
            writeOffset += bytesWritten;
            if (version >= CodeDirectoryVersion.SupportsScatter)
            {
                scatterHeader.Write(blobBuffer.AsSpan(writeOffset), out bytesWritten);
                writeOffset += bytesWritten;
            }
            if (version >= CodeDirectoryVersion.SupportsTeamId)
            {
                teamIdHeader.Write(blobBuffer.AsSpan(writeOffset), out bytesWritten);
                writeOffset += bytesWritten;
            }
            if (version >= CodeDirectoryVersion.SupportsCodeLimit64)
            {
                codeLimit64Header.Write(blobBuffer.AsSpan(writeOffset), out bytesWritten);
                writeOffset += bytesWritten;
            }
            if (version >= CodeDirectoryVersion.SupportsExecSegment)
            {
                execSegementHeader.Write(blobBuffer.AsSpan(writeOffset), out bytesWritten);
                writeOffset += bytesWritten;
            }

            return blobBuffer;
        }
    }
}
