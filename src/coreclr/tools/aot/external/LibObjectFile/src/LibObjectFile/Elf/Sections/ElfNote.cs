// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// A Note entry in <see cref="ElfNoteTable"/>
    /// </summary>
    public abstract class ElfNote
    {
        protected ElfNote()
        {
        }

        /// <summary>
        /// Gets or sets the name of this note.
        /// </summary>
        public abstract string GetName();

        /// <summary>
        /// Gets or sets the type of this note.
        /// </summary>
        public abstract ElfNoteTypeEx GetNoteType();

        public abstract uint GetDescriptorSize();

        public abstract string GetDescriptorAsText();

        public override string ToString()
        {
            return $"{nameof(ElfNote)} {GetName()}, Type: {GetNoteType()}";
        }
        
        internal void ReadDescriptorInternal(ElfReader reader, uint descriptorLength)
        {
            ReadDescriptor(reader, descriptorLength);
        }

        internal void WriteDescriptorInternal(ElfWriter writer)
        {
            WriteDescriptor(writer);
        }
        protected abstract void ReadDescriptor(ElfReader reader, uint descriptorLength);

        protected abstract void WriteDescriptor(ElfWriter writer);
    }
}