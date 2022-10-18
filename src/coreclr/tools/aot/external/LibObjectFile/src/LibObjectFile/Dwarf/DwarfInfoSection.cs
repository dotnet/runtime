// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    public sealed class DwarfInfoSection : DwarfRelocatableSection
    {
        private readonly List<DwarfUnit> _units;

        public DwarfInfoSection()
        {
            _units = new List<DwarfUnit>();
        }

        public IReadOnlyList<DwarfUnit> Units => _units;

        public void AddUnit(DwarfUnit unit)
        {
            _units.Add(this, unit);
        }

        public void RemoveUnit(DwarfUnit unit)
        {
            _units.Remove(this, unit);
        }

        public DwarfUnit RemoveUnitAt(int index)
        {
            return _units.RemoveAt(this, index);
        }

        protected override void Read(DwarfReader reader)
        {
            var addressRangeTable = reader.File.AddressRangeTable;
            
            while (reader.Offset < reader.Length)
            {
                // 7.5 Format of Debugging Information
                // - Each such contribution consists of a compilation unit header

                var startOffset = Offset;

                reader.ClearResolveAttributeReferenceWithinCompilationUnit();

                var cu = DwarfUnit.ReadInstance(reader, out var offsetEndOfUnit);
                if (cu == null)
                {
                    reader.Offset = offsetEndOfUnit;
                    continue;
                }

                reader.CurrentUnit = cu;

                // Link AddressRangeTable to Unit
                if (addressRangeTable.DebugInfoOffset == cu.Offset)
                {
                    addressRangeTable.Unit = cu;
                }
                
                AddUnit(cu);
            }

            reader.ResolveAttributeReferenceWithinSection();
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            foreach (var unit in _units)
            {
                unit.Verify(diagnostics);
            }
        }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            var offset = Offset;
            foreach (var unit in Units)
            {
                layoutContext.CurrentUnit = unit;
                unit.Offset = offset;
                unit.UpdateLayoutInternal(layoutContext);
                offset += unit.Size;
            }
            Size = offset - Offset;
        }

        protected override void Write(DwarfWriter writer)
        {
            Debug.Assert(Offset == writer.Offset);
            foreach (var unit in _units)
            {
                writer.CurrentUnit = unit;
                unit.WriteInternal(writer);
            }
            writer.CurrentUnit = null;
            Debug.Assert(Size == writer.Offset - Offset);
        }
    }
}