// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Collections.Generic;

namespace LibObjectFile.Dwarf
{

    public class DwarfLocationList : DwarfContainer
    {
        private readonly List<DwarfLocationListEntry> _locationListEntries;

        public DwarfLocationList()
        {
            _locationListEntries = new List<DwarfLocationListEntry>();
        }

        public IReadOnlyList<DwarfLocationListEntry> LocationListEntries => _locationListEntries;

        public void AddLocationListEntry(DwarfLocationListEntry locationListEntry)
        {
            _locationListEntries.Add(this, locationListEntry);
        }

        public void RemoveLocationList(DwarfLocationListEntry locationListEntry)
        {
            _locationListEntries.Remove(this, locationListEntry);
        }

        public DwarfLocationListEntry RemoveLocationListEntryAt(int index)
        {
            return _locationListEntries.RemoveAt(this, index);
        }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            var endOffset = Offset;

            foreach (var locationListEntry in _locationListEntries)
            {
                locationListEntry.Offset = endOffset;
                locationListEntry.UpdateLayoutInternal(layoutContext);
                endOffset += locationListEntry.Size;
            }

            // End of list
            endOffset += 2 * DwarfHelper.SizeOfUInt(layoutContext.CurrentUnit.AddressSize);

            Size = endOffset - Offset;
        }

        protected override void Read(DwarfReader reader)
        {
            reader.OffsetToLocationList.Add(reader.Offset, this);

            while (reader.Offset < reader.Length)
            {
                var locationListEntry = new DwarfLocationListEntry();
                locationListEntry.ReadInternal(reader);

                if (locationListEntry.Start == 0 && locationListEntry.End == 0)
                {
                    // End of list
                    return;
                }

                _locationListEntries.Add(locationListEntry);
            }
        }

        protected override void Write(DwarfWriter writer)
        {
            foreach (var locationListEntry in _locationListEntries)
            {
                locationListEntry.WriteInternal(writer);
            }

            // End of list
            writer.WriteUInt(0);
            writer.WriteUInt(0);
        }
    }
}