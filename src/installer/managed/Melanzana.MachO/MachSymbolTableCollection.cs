using System.Buffers.Binary;
using System.Collections;
using System.Diagnostics;
using System.Text;
using Melanzana.MachO.BinaryFormat;
using Melanzana.Streams;

namespace Melanzana.MachO
{
    internal class MachSymbolTableCollection : IList<MachSymbol>
    {
        private readonly MachObjectFile objectFile;
        private readonly MachLinkEditData symbolTableData;
        private readonly MachLinkEditData stringTableData;
        private readonly List<MachSymbol> innerList = new();

        bool isDirty;

        public MachSymbolTableCollection(
            MachObjectFile objectFile,
            MachLinkEditData symbolTableData,
            MachLinkEditData stringTableData,
            Dictionary<byte, MachSection> sectionMap)
        {
            this.objectFile = objectFile;
            this.symbolTableData = symbolTableData;
            this.stringTableData = stringTableData;

            // Read existing symbols
            if (symbolTableData.Size > 0)
            {
                byte[] stringTable = new byte[stringTableData.Size];
                using var stringTableStream = stringTableData.GetReadStream();
                stringTableStream.ReadFully(stringTable);

                uint symbolSize = SymbolHeader.BinarySize + (objectFile.Is64Bit ? 8u : 4u);
                innerList.Capacity = (int)(symbolTableData.Size / symbolSize);

                byte[] symbolBuffer = new byte[symbolSize];
                using var symbolTableStream = symbolTableData.GetReadStream();
                while (symbolTableStream.Position < symbolTableStream.Length)
                {
                    symbolTableStream.ReadFully(symbolBuffer);
                    var symbolHeader = SymbolHeader.Read(symbolBuffer, objectFile.IsLittleEndian, out var _);
                    ulong symbolValue;
                    if (objectFile.IsLittleEndian)
                    {
                        symbolValue = objectFile.Is64Bit ?
                            BinaryPrimitives.ReadUInt64LittleEndian(symbolBuffer.AsSpan(SymbolHeader.BinarySize)) :
                            BinaryPrimitives.ReadUInt32LittleEndian(symbolBuffer.AsSpan(SymbolHeader.BinarySize));
                    }
                    else
                    {
                        symbolValue = objectFile.Is64Bit ?
                            BinaryPrimitives.ReadUInt64BigEndian(symbolBuffer.AsSpan(SymbolHeader.BinarySize)) :
                            BinaryPrimitives.ReadUInt32BigEndian(symbolBuffer.AsSpan(SymbolHeader.BinarySize));
                    }

                    string name = string.Empty;
                    if (symbolHeader.NameIndex != 0)
                    {
                        int nameLength = stringTable.AsSpan((int)symbolHeader.NameIndex).IndexOf((byte)0);
                        Debug.Assert(nameLength >= 0);
                        name = Encoding.UTF8.GetString(stringTable.AsSpan((int)symbolHeader.NameIndex, nameLength));
                    }

                    var symbol = new MachSymbol
                    {
                        Name = name,
                        Descriptor = (MachSymbolDescriptor)symbolHeader.Descriptor,
                        Section = symbolHeader.Section == 0 ? null : sectionMap[symbolHeader.Section],
                        Type = (MachSymbolType)symbolHeader.Type,
                        Value = symbolValue,
                    };

                    innerList.Add(symbol);
                }
            }
        }

        public int Count => innerList.Count;

        public bool IsReadOnly => false;

        public MachSymbol this[int index]
        {
            get => innerList[index];
            set
            {
                innerList[index] = value;
                isDirty = true;
            }
        }

        public void Add(MachSymbol symbol)
        {
            innerList.Add(symbol);
            isDirty = true;
        }

        public void Clear()
        {
            innerList.Clear();
            isDirty = true;
        }

        public bool Contains(MachSymbol symbol)
        {
            return innerList.Contains(symbol);
        }

        public void CopyTo(MachSymbol[] array, int arrayIndex)
        {
            innerList.CopyTo(array, arrayIndex);
        }

        public IEnumerator<MachSymbol> GetEnumerator()
        {
            return innerList.GetEnumerator();
        }

        public bool Remove(MachSymbol item)
        {
            if (innerList.Remove(item))
            {
                isDirty = true;
                return true;
            }

            return false;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void FlushIfDirty()
        {
            if (isDirty)
            {
                var sectionMap = new Dictionary<MachSection, byte>();
                byte sectionIndex = 1;
                foreach (var section in objectFile.Segments.SelectMany(segment => segment.Sections))
                {
                    sectionMap.Add(section, sectionIndex++);
                    Debug.Assert(sectionIndex != 0);
                }

                using var stringTableWriter = stringTableData.GetWriteStream();
                using var symbolTableWriter = symbolTableData.GetWriteStream();

                // Start the table with a NUL byte.
                stringTableWriter.WriteByte(0);

                SymbolHeader symbolHeader = new SymbolHeader();
                Span<byte> symbolHeaderBuffer = stackalloc byte[SymbolHeader.BinarySize];
                Span<byte> symbolValueBuffer = new byte[objectFile.Is64Bit ? 8 : 4];

                foreach (var symbol in innerList)
                {
                    var nameBytes = Encoding.UTF8.GetBytes(symbol.Name);
                    var nameOffset = stringTableWriter.Position;

                    stringTableWriter.Write(nameBytes);
                    stringTableWriter.WriteByte(0);

                    symbolHeader.NameIndex = (uint)nameOffset;
                    symbolHeader.Section = symbol.Section == null ? (byte)0 : sectionMap[symbol.Section];
                    symbolHeader.Descriptor = (ushort)symbol.Descriptor;
                    symbolHeader.Type = (byte)symbol.Type;

                    symbolHeader.Write(symbolHeaderBuffer, objectFile.IsLittleEndian, out _);
                    symbolTableWriter.Write(symbolHeaderBuffer);

                    if (objectFile.Is64Bit)
                    {
                        if (objectFile.IsLittleEndian)
                        {
                            BinaryPrimitives.WriteUInt64LittleEndian(symbolValueBuffer, symbol.Value);
                        }
                        else
                        {
                            BinaryPrimitives.WriteUInt64BigEndian(symbolValueBuffer, symbol.Value);
                        }
                    }
                    else if (objectFile.IsLittleEndian)
                    {
                        BinaryPrimitives.WriteUInt32LittleEndian(symbolValueBuffer, (uint)symbol.Value);
                    }
                    else
                    {
                        BinaryPrimitives.WriteUInt32BigEndian(symbolValueBuffer, (uint)symbol.Value);
                    }

                    symbolTableWriter.Write(symbolValueBuffer);
                }

                // Pad the string table
                int alignment = objectFile.Is64Bit ? 8 : 4;
                while ((stringTableWriter.Position & (alignment - 1)) != 0)
                    stringTableWriter.WriteByte(0);

                isDirty = false;
            }
        }

        public int IndexOf(MachSymbol symbol) => innerList.IndexOf(symbol);

        public void Insert(int index, MachSymbol symbol)
        {
            innerList.Insert(index, symbol);
            isDirty = true;
        }

        public void RemoveAt(int index)
        {
            innerList.RemoveAt(index);
            isDirty = true;
        }
    }
}