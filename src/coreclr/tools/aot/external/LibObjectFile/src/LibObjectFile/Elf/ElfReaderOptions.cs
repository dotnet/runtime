// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Options used by <see cref="ElfObjectFile.Read"/> and <see cref="ElfObjectFile.TryRead"/>
    /// </summary>
    public class ElfReaderOptions
    {
        /// <summary>
        /// Gets or sets a boolean indicating if the stream can be used in read-only mode, or <c>false</c> the resulting
        /// <see cref="ElfObjectFile"/> will be modified.
        /// </summary>
        public bool ReadOnly { get; set; }

        /// <summary>
        /// Gets or sets a delegate that can be used to replace the creation of <see cref="ElfNote"/> when
        /// reading from a Stream.
        /// </summary>
        public TryCreateNoteDelegate TryCreateNote { get; set; }
        
        /// <summary>
        /// Gets or sets a delegate that can be used to replace the creation of <see cref="ElfSection"/> when
        /// reading from a Stream.
        /// </summary>
        public TryCreateSectionDelegate TryCreateSection { get; set; }

        /// <summary>
        /// Tries to create a section instance from the specified type. Might return null.
        /// </summary>
        /// <param name="sectionType">Type of the section to create.</param>
        /// <param name="diagnostics">The diagnostics</param>
        /// <returns><c>null</c> if the section is not supported or an instance of <see cref="ElfSection"/> for the specified type.</returns>
        public delegate ElfSection TryCreateSectionDelegate(ElfSectionType sectionType, DiagnosticBag diagnostics);
        
        /// <summary>
        /// Tries to create a note instance from the specified name and type. Might return null.
        /// </summary>
        /// <param name="noteName">Name of the note.</param>
        /// <param name="noteType">Type of the note</param>
        /// <returns><c>null</c> if the note is not supported or an instance of <see cref="ElfNote"/> for the specified name and type.</returns>
        public delegate ElfNote TryCreateNoteDelegate(string noteName, ElfNoteType noteType);

    }
}