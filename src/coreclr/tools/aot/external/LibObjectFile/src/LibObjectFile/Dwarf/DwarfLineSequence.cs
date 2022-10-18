// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    /// <summary>
    /// A sequence of <see cref="DwarfLine"/>
    /// </summary>
    [DebuggerDisplay("Count = {Lines.Count,nq}")]
    public class DwarfLineSequence : DwarfObject<DwarfLineProgramTable>, IEnumerable<DwarfLine>
    {
        private readonly List<DwarfLine> _lines;

        public DwarfLineSequence()
        {
            _lines = new List<DwarfLine>();
        }

        public IReadOnlyList<DwarfLine> Lines => _lines;

        public void Add(DwarfLine line)
        {
            _lines.Add(this, line);
        }

        public void Remove(DwarfLine line)
        {
            _lines.Remove(this, line);
        }

        public DwarfLine RemoveAt(int index)
        {
            return _lines.RemoveAt(this, index);
        }
        
        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            // This is implemented in DwarfLineSection
        }

        protected override void Read(DwarfReader reader)
        {
            // This is implemented in DwarfLineSection
        }

        protected override void Write(DwarfWriter writer)
        {
            // This is implemented in DwarfLineSection
        }

        public List<DwarfLine>.Enumerator GetEnumerator()
        {
            return _lines.GetEnumerator();
        }

        IEnumerator<DwarfLine> IEnumerable<DwarfLine>.GetEnumerator()
        {
            return GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable) _lines).GetEnumerator();
        }
    }
}