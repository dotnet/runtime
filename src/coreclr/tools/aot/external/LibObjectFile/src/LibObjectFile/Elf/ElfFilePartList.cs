// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;

namespace LibObjectFile.Elf
{
    /// <summary>
    /// Internal list used to keep an ordered list of <see cref="ElfFilePart"/> based on offsets.
    /// It is used to track region of the file that are actually referenced by a <see cref="ElfSegment"/>
    /// but are not declared as a <see cref="ElfSection"/>
    /// </summary>
    internal struct ElfFilePartList
    {
        private readonly List<ElfFilePart> _parts;

        public ElfFilePartList(int capacity)
        {
            _parts = new List<ElfFilePart>(capacity);
        }

        public int Count => _parts.Count;

        public ElfFilePart this[int index]
        {
            get => _parts[index];
            set => _parts[index] = value;
        }
        
        public void Insert(ElfFilePart part)
        {
            for (int i = 0; i < _parts.Count; i++)
            {
                var against = _parts[i];
                var delta = part.CompareTo(against);
                if (delta < 0)
                {
                    _parts.Insert(i, part);
                    return;
                }

                // Don't add an overlap
                if (delta == 0)
                {
                    // do nothing
                    return;
                }
            }
            _parts.Add(part);
        }

        public void CreateParts(ulong startOffset, ulong endOffset)
        {
            var offset = startOffset;
            for (int i = 0; i < _parts.Count && offset < endOffset; i++)
            {
                var part = _parts[i];
                if (offset < part.StartOffset)
                {
                    if (endOffset < part.StartOffset)
                    {
                        var newPart = new ElfFilePart(offset, endOffset);
                        _parts.Insert(i, newPart);
                        offset = endOffset + 1;
                        break;
                    }

                    // Don't merge parts, so that we will create a single ElfInlineShadowSection per parts
                    _parts.Insert(i, new ElfFilePart(offset, part.StartOffset - 1));
                }

                offset = part.EndOffset + 1;
            }

            if (offset < endOffset)
            {
                _parts.Add(new ElfFilePart(offset, endOffset));
            }
        }
    }
}