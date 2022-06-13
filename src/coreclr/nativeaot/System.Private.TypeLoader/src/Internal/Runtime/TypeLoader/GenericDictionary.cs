// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;
using Internal.Runtime.Augments;
using Debug = System.Diagnostics.Debug;

namespace Internal.Runtime.TypeLoader
{
    internal abstract class GenericDictionary
    {
        protected GenericDictionaryCell[] _cells;
        protected IntPtr _addressOfFirstCellSlot;

        public GenericDictionary(GenericDictionaryCell[] cells)
        {
            Debug.Assert(cells != null);
            _cells = cells;
        }

        public abstract IntPtr Allocate();

        public unsafe void Finish(TypeBuilder typeBuilder)
        {
            Debug.Assert(_cells.Length == 0 || _addressOfFirstCellSlot != IntPtr.Zero);

            IntPtr* realCells = (IntPtr*)_addressOfFirstCellSlot;
            for (int i = 0; i < _cells.Length; i++)
            {
                _cells[i].WriteCellIntoDictionary(typeBuilder, realCells, i);
            }
        }
    }

    internal class GenericTypeDictionary : GenericDictionary
    {
        public GenericTypeDictionary(GenericDictionaryCell[] cells)
            : base(cells)
        { }

        public override IntPtr Allocate()
        {
            Debug.Assert(_addressOfFirstCellSlot == IntPtr.Zero);

            if (_cells.Length > 0)
            {
                // Use checked typecast to int to ensure there aren't any overflows/truncations
                _addressOfFirstCellSlot = MemoryHelpers.AllocateMemory(checked((int)(_cells.Length * IntPtr.Size)));
            }

            return _addressOfFirstCellSlot;
        }
    }

    internal class GenericMethodDictionary : GenericDictionary
    {
        public GenericMethodDictionary(GenericDictionaryCell[] cells)
            : base(cells)
        { }

        public override unsafe IntPtr Allocate()
        {
            Debug.Assert(_addressOfFirstCellSlot == IntPtr.Zero);

            // Method dictionaries start with a header containing the hash code, which is not part of the native layout.
            // The real first slot is located after the header.
            // Use checked typecast to int to ensure there aren't any overflows/truncations
            IntPtr dictionaryWithHeader = MemoryHelpers.AllocateMemory(checked((int)((_cells.Length + 1) * IntPtr.Size)));

            // Put a magic hash code to indicate dynamically allocated method dictionary for
            // debugging purposes.
            *(int*)dictionaryWithHeader = 0xD1CC0DE; // DICCODE

            _addressOfFirstCellSlot = IntPtr.Add(dictionaryWithHeader, IntPtr.Size);

            return _addressOfFirstCellSlot;
        }
    }
}
