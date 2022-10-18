// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Ar
{
    /// <summary>
    /// Reader options used by <see cref="ArArchiveFile.Read(System.IO.Stream,ArArchiveKind)"/> and other methods.
    /// </summary>
    public class ArArchiveFileReaderOptions
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="archiveKind">Type of the 'ar' file to load (GNU, BSD...)</param>
        public ArArchiveFileReaderOptions(ArArchiveKind archiveKind)
        {
            ArchiveKind = archiveKind;
            ProcessObjectFiles = true;
        }

        /// <summary>
        /// Gets or sets a boolean indicating if the file entries must keep a readonly view
        /// on the original stream for the content of the file entries, or it should copy
        /// them to modifiable <see cref="System.IO.MemoryStream"/>.
        /// </summary>
        public bool IsReadOnly { get; set; }
        
        /// <summary>
        /// Gets or sets the type of file to load
        /// </summary>
        public ArArchiveKind ArchiveKind { get; set; }

        /// <summary>
        /// Gets or sets a boolean indicating if object files are being processed to return
        /// typed entries (<see cref="ArElfFile"/>) instead of generic binary file entry (<see cref="ArBinaryFile"/>).
        /// Default is <c>true</c>
        /// </summary>
        public bool ProcessObjectFiles { get; set; }
    }
}