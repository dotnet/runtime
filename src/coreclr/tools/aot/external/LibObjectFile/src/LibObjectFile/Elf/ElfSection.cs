// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Diagnostics;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Defines the base class for a section in an <see cref="ElfObjectFile"/>.
    /// </summary>
    [DebuggerDisplay("{ToString(),nq}")]
    public abstract class ElfSection : ElfObject
    {
        private ElfSectionType _type;

        protected ElfSection() : this(ElfSectionType.Null)
        {
        }

        protected ElfSection(ElfSectionType sectionType)
        {
            _type = sectionType;
        }

        public virtual ElfSectionType Type
        {
            get => _type;
            set
            {
                _type = value;
            }
        }

        protected override void ValidateParent(ObjectFileNode parent)
        {
            if (!(parent is ElfObjectFile))
            {
                throw new ArgumentException($"Parent must inherit from type {nameof(ElfObjectFile)}");
            }
        }


        /// <summary>
        /// Gets the containing <see cref="ElfObjectFile"/>. Might be null if this section or segment
        /// does not belong to an existing <see cref="ElfObjectFile"/>.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        public new ElfObjectFile Parent
        {
            get => (ElfObjectFile)base.Parent;
            internal set => base.Parent = value;
        }

        /// <summary>
        /// Gets or sets the <see cref="ElfSectionFlags"/> of this section.
        /// </summary>
        public ElfSectionFlags Flags { get; set; }

        /// <summary>
        /// Gets or sets the name of this section.
        /// </summary>
        public ElfString Name { get; set; }

        /// <summary>
        /// Gets or sets the virtual address of this section.
        /// </summary>
        public ulong VirtualAddress { get; set; }

        /// <summary>
        /// Gets or sets the alignment requirement of this section.
        /// </summary>
        public ulong Alignment { get; set; }

        /// <summary>
        /// Gets or sets the link element of this section.
        /// </summary>
        public ElfSectionLink Link { get; set; }

        /// <summary>
        /// Gets or sets the info element of this section.
        /// </summary>
        public ElfSectionLink Info { get; set; }

        /// <summary>
        /// Gets the table entry size of this section.
        /// </summary>
        public virtual ulong TableEntrySize => 0;

        /// <summary>
        /// Gets the index of the visible sections in <see cref="ElfObjectFile.Sections"/> (visible == not <see cref="ElfShadowSection"/>)
        /// </summary>
        public uint SectionIndex { get; internal set; }

        /// <summary>
        /// Gets or sets the ordering index used when writing back this section.
        /// </summary>
        public uint StreamIndex { get; set; }

        /// <summary>
        /// Gets the size of the original table entry size of this section.
        /// </summary>
        public ulong OriginalTableEntrySize { get; internal set; }

        /// <summary>
        /// Gets a boolean indicating if this section is a <see cref="ElfShadowSection"/>.
        /// </summary>
        public bool IsShadow => this is ElfShadowSection;

        /// <summary>
        /// Gets a boolean indicating if this section has some content (Size should be taken into account).
        /// </summary>
        public bool HasContent => Type != ElfSectionType.NoBits && (Type != ElfSectionType.Null || this is ElfShadowSection);

        /// <summary>
        /// Read data from the specified reader to this instance.
        /// </summary>
        /// <param name="reader">The reader to read data from.</param>
        protected abstract void Read(ElfReader reader);

        /// <summary>
        /// Writes data to the specified writer from this instance.
        /// </summary>
        /// <param name="writer">The writer to write data to.</param>
        protected abstract void Write(ElfWriter writer);

        internal void WriteInternal(ElfWriter writer)
        {
            Write(writer);
        }

        internal void ReadInternal(ElfReader reader)
        {
            Read(reader);
        }
        
        public override void Verify(DiagnosticBag diagnostics)
        {
            if (diagnostics == null) throw new ArgumentNullException(nameof(diagnostics));

            // Check parent for link section
            if (Link.Section != null)
            {
                if (Link.Section.Parent != this.Parent)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionLinkParent, $"Invalid parent for {nameof(Link)}: `{Link}` used by section `{this}`. The {nameof(Link)}.{nameof(ElfSectionLink.Section)} must have the same parent {nameof(ElfObjectFile)} than this section");
                }
            }

            if (Info.Section != null)
            {
                if (Info.Section.Parent != this.Parent)
                {
                    diagnostics.Error(DiagnosticId.ELF_ERR_InvalidSectionInfoParent, $"Invalid parent for {nameof(Info)}: `{Info}` used by section `{this}`. The {nameof(Info)}.{nameof(ElfSectionLink.Section)} must have the same parent {nameof(ElfObjectFile)} than this section");
                }
            }

            // Verify that Link is correctly setup for this section
            switch (Type)
            {
                case ElfSectionType.DynamicLinking:
                case ElfSectionType.DynamicLinkerSymbolTable:
                case ElfSectionType.SymbolTable:
                    Link.TryGetSectionSafe<ElfStringTable>(this.GetType().Name, nameof(Link), this, diagnostics, out _, ElfSectionType.StringTable);
                    break;
                case ElfSectionType.SymbolHashTable:
                case ElfSectionType.Relocation:
                case ElfSectionType.RelocationAddends:
                    Link.TryGetSectionSafe<ElfSymbolTable>(this.GetType().Name, nameof(Link), this, diagnostics, out _, ElfSectionType.SymbolTable, ElfSectionType.DynamicLinkerSymbolTable);
                    break;
            }
        }

        protected virtual void AfterRead(ElfReader reader)
        {
        }

        protected virtual void BeforeWrite(ElfWriter writer)
        {
        }

        public override string ToString()
        {
            return $"Section [{SectionIndex}](Internal: {Index}) `{Name}` ";
        }

        internal void BeforeWriteInternal(ElfWriter writer)
        {
            BeforeWrite(writer);
        }

        internal void AfterReadInternal(ElfReader reader)
        {
            AfterRead(reader);
        }
    }
}