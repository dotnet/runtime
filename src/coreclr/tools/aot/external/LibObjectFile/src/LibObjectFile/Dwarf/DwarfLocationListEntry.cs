// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace LibObjectFile.Dwarf
{
    public class DwarfLocationListEntry : DwarfObject<DwarfLocationList>
    {
        public ulong Start;

        public ulong End;

        public DwarfExpression Expression;

        public DwarfLocationListEntry()
        {
        }

        protected override void Read(DwarfReader reader)
        {
            Start = reader.ReadUInt();
            End = reader.ReadUInt();

            if (Start == 0 && End == 0)
            {
                // End of list
                return;
            }

            bool isBaseAddress =
                (reader.AddressSize == DwarfAddressSize.Bit64 && Start == ulong.MaxValue) ||
                (reader.AddressSize == DwarfAddressSize.Bit32 && Start == uint.MaxValue);
            if (isBaseAddress)
            {
                // Sets new base address for following entries
                return;
            }

            Expression = new DwarfExpression();
            Expression.ReadInternal(reader, inLocationSection: true);
        }

        protected override void UpdateLayout(DwarfLayoutContext layoutContext)
        {
            var endOffset = Offset;

            endOffset += 2 * DwarfHelper.SizeOfUInt(layoutContext.CurrentUnit.AddressSize);
            if (Expression != null)
            {
                Expression.Offset = endOffset;
                Expression.UpdateLayoutInternal(layoutContext, inLocationSection: true);
                endOffset += Expression.Size;
            }

            Size = endOffset - Offset;
        }

        protected override void Write(DwarfWriter writer)
        {
            bool isBaseAddress =
                (writer.AddressSize == DwarfAddressSize.Bit64 && Start == ulong.MaxValue) ||
                (writer.AddressSize == DwarfAddressSize.Bit32 && Start == uint.MaxValue);
            if (isBaseAddress)
            {
                writer.WriteUInt(Start);
                writer.WriteAddress(DwarfRelocationTarget.Code, End);
            }
            else
            {
                writer.WriteAddress(DwarfRelocationTarget.Code, Start);
                writer.WriteAddress(DwarfRelocationTarget.Code, End);
            }

            if (Expression != null)
            {
                Expression.WriteInternal(writer, inLocationSection: true);
            }
        }

        public override string ToString()
        {
            return $"Location: {Start:x} - {End:x} {Expression}";
        }
    }
}
