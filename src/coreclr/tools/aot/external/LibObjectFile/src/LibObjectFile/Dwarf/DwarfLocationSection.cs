
// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;

namespace LibObjectFile.Dwarf
{
    [DebuggerDisplay("Count = {LineTables.Count,nq}")]
    public sealed class DwarfLocationSection : DwarfRelocatableSection
    {
        private readonly List<DwarfLocationList> _locationLists;

        public DwarfLocationSection()
        {
            _locationLists = new List<DwarfLocationList>();
        }

        public IReadOnlyList<DwarfLocationList> LocationLists => _locationLists;

        public void AddLocationList(DwarfLocationList locationList)
        {
            _locationLists.Add(this, locationList);
        }

        public void RemoveLocationList(DwarfLocationList locationList)
        {
            _locationLists.Remove(this, locationList);
        }

        public DwarfLocationList RemoveLineProgramTableAt(int index)
        {
            return _locationLists.RemoveAt(this, index);
        }

        protected override void Read(DwarfReader reader)
        {
            while (reader.Offset < reader.Length)
            {
                var locationList = new DwarfLocationList();
                locationList.Offset = reader.Offset;
                locationList.ReadInternal(reader);
                AddLocationList(locationList);
            }
        }

        public override void Verify(DiagnosticBag diagnostics)
        {
            base.Verify(diagnostics);

            foreach (var locationList in _locationLists)
            {
                locationList.Verify(diagnostics);
            }
        }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            ulong sizeOf = 0;

            foreach (var locationList in _locationLists)
            {
                locationList.Offset = Offset + sizeOf;
                locationList.UpdateLayoutInternal(layoutContext);
                sizeOf += locationList.Size;
            }
            Size = sizeOf;
        }

        protected override void Write(DwarfWriter writer)
        {
            var startOffset = writer.Offset;

            foreach (var locationList in _locationLists)
            {
                locationList.WriteInternal(writer);
            }

            Debug.Assert(Size == writer.Offset - startOffset, $"Expected Size: {Size} != Written Size: {writer.Offset - startOffset}");
        }

        public override string ToString()
        {
            return $"Section .debug_loc, Entries: {_locationLists.Count}";
        }
    }
}