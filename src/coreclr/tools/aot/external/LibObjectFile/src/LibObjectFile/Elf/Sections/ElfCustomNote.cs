// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Buffers;
using System.IO;
using System.Text;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A custom note entry in <see cref="ElfNoteTable"/>
    /// </summary>
    public class ElfCustomNote : ElfNote
    {
        /// <summary>
        /// Gets or sets the name of this note.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the associated descriptor data.
        /// </summary>
        public Stream Descriptor { get; set; }

        /// <summary>
        /// Gets or sets the type of this note.
        /// </summary>
        public ElfNoteTypeEx Type { get; set; }

        public override string GetName()
        {
            return Name;
        }

        public override ElfNoteTypeEx GetNoteType()
        {
            return Type;
        }

        public override uint GetDescriptorSize()
        {
            return Descriptor == null ? 0 : (uint)Descriptor.Length;
        }

        public override string GetDescriptorAsText()
        {
            if (Descriptor == null || Descriptor.Length == 0) return string.Empty;

            Descriptor.Position = 0;

            var length = (int) Descriptor.Length;
            var buffer = ArrayPool<byte>.Shared.Rent(length);
            try
            {
                length = Descriptor.Read(buffer, 0, length);
                Descriptor.Position = 0;
                var hasBinary = false;

                // If we have any binary data (don't take into account a potential null terminated string)
                for (int i = 0; i < length - 1; i++)
                {
                    if (buffer[i] < ' ')
                    {
                        hasBinary = true;
                        break;
                    }
                }

                if (hasBinary)
                {
                    var builder = new StringBuilder();
                    for (int i = 0; i < length; i++)
                    {
                        builder.Append($"{buffer[i]:x2}");
                    }

                    return builder.ToString();
                }
                else
                {
                    return Encoding.UTF8.GetString(buffer, 0, length);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        protected override void ReadDescriptor(ElfReader reader, uint descriptorLength)
        {
            if (descriptorLength > 0)
            {
                Descriptor = reader.ReadAsStream(descriptorLength);
            }
        }

        protected override void WriteDescriptor(ElfWriter writer)
        {
            if (Descriptor != null)
            {
                writer.Write(Descriptor);
            }
        }
    }
}