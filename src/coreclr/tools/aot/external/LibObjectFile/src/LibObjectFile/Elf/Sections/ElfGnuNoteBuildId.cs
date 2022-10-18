// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Buffers;
using System.IO;
using System.Text;

namespace LibObjectFile.Elf
{
    public class ElfGnuNoteBuildId : ElfGnuNote
    {
        public override ElfNoteTypeEx GetNoteType() => new ElfNoteTypeEx(ElfNoteType.GNU_BUILD_ID);

        public Stream BuildId { get; set; }

        public override uint GetDescriptorSize() => BuildId != null ? (uint)BuildId.Length : 0;

        public override string GetDescriptorAsText()
        {
            var builder = new StringBuilder();
            builder.Append("Build ID: ");

            if (BuildId != null)
            {
                BuildId.Position = 0;
                var length = (int)BuildId.Length;
                var buffer = ArrayPool<byte>.Shared.Rent(length);
                length = BuildId.Read(buffer, 0, length);
                BuildId.Position = 0;

                for (int i = 0; i < length; i++)
                {
                    builder.Append($"{buffer[i]:x2}");
                }
            }

            return builder.ToString();
        }


        protected override void ReadDescriptor(ElfReader reader, uint descriptorLength)
        {
            if (descriptorLength > 0)
            {
                BuildId = reader.ReadAsStream(descriptorLength);
            }
        }

        protected override void WriteDescriptor(ElfWriter writer)
        {
            if (BuildId != null)
            {
                writer.Write(BuildId);
            }
        }
    }
}