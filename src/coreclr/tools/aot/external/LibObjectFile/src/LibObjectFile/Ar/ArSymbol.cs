// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Ar
{
    /// <summary>
    /// A symbol stored in a <see cref="ArSymbolTable"/>
    /// </summary>
    public struct ArSymbol
    {
        /// <summary>
        /// Initializes a new instance.
        /// </summary>
        /// <param name="name">The name of the symbol.</param>
        /// <param name="file">The associated file entry this symbol is coming from <see cref="ArArchiveFile.Files"/>.</param>
        public ArSymbol(string name, ArFile file) : this()
        {
            Name = name;
            File = file;
        }

        /// <summary>
        /// Gets or sets the name of this symbol.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Internal offset for the name (used for reading)
        /// </summary>
        internal uint NameOffset { get; set; }

        /// <summary>
        /// Gets or sets the associated file entry this symbol is coming from <see cref="ArArchiveFile.Files"/>.
        /// </summary>
        public ArFile File { get; set; }

        /// <summary>
        /// Internal offset of the file (used for reading)
        /// </summary>
        internal ulong FileOffset { get; set; }

        public override string ToString()
        {
            return $"Symbol: {Name} => {nameof(File)}: {File}";
        }
    }
}