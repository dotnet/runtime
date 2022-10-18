using System.Buffers.Binary;
using System.Collections;
using Melanzana.Streams;

namespace Melanzana.MachO
{
    public class MachRelocationCollection : IList<MachRelocation>
    {
        private readonly MachObjectFile objectFile;
        private readonly MachLinkEditData relocationData;
        private readonly List<MachRelocation> innerList;
        private bool isDirty;

        internal MachRelocationCollection(MachObjectFile objectFile, MachLinkEditData relocationData)
        {
            int relocationCount = (int)(relocationData.Size / 8);

            this.objectFile = objectFile;
            this.relocationData = relocationData;
            this.innerList = new List<MachRelocation>(relocationCount);
            this.isDirty = false;

            // Read existing relocations
            using var relocationStream = relocationData.GetReadStream();
            Span<byte> relocationBuffer = stackalloc byte[8];
            
            for (uint i = 0; i < relocationCount; i++)
            {
                relocationStream.ReadFully(relocationBuffer);

                int address =
                    objectFile.IsLittleEndian ?
                    BinaryPrimitives.ReadInt32LittleEndian(relocationBuffer) :
                    BinaryPrimitives.ReadInt32BigEndian(relocationBuffer);

                uint info =
                    objectFile.IsLittleEndian ?
                    BinaryPrimitives.ReadUInt32LittleEndian(relocationBuffer.Slice(4)) :
                    BinaryPrimitives.ReadUInt32BigEndian(relocationBuffer.Slice(4));

                innerList.Add(new MachRelocation
                {
                    Address = address,
                    SymbolOrSectionIndex = info & 0xff_ff_ff,
                    IsPCRelative = (info & 0x1_00_00_00) > 0,
                    Length = ((info >> 25) & 3) switch { 0 => 1, 1 => 2, 2 => 4, _ => 8 },
                    IsExternal = (info & 0x8_00_00_00) > 0,
                    RelocationType = (MachRelocationType)(info >> 28)
                });
            }
        }

        public MachRelocation this[int index]
        {
            get => innerList[index];
            set
            {
                innerList[index] = value;
                isDirty = true;
            }
        }

        public int Count => innerList.Count;

        public bool IsReadOnly => false;

        public void Add(MachRelocation item)
        {
            innerList.Add(item);
            isDirty = true;
        }

        public void Clear()
        {
            innerList.Clear();
            isDirty = true;
        }

        public void Insert(int index, MachRelocation item)
        {
            innerList.Insert(index, item);
            isDirty = true;
        }

        public bool Remove(MachRelocation item)
        {
            if (innerList.Remove(item))
            {
                isDirty = true;
                return true;
            }

            return false;
        }

        public void RemoveAt(int index)
        {
            innerList.RemoveAt(index);
            isDirty = true;
        }

        IEnumerator IEnumerable.GetEnumerator() => innerList.GetEnumerator();

        public bool Contains(MachRelocation item) => innerList.Contains(item);

        public void CopyTo(MachRelocation[] array, int arrayIndex) => innerList.CopyTo(array, arrayIndex);

        public IEnumerator<MachRelocation> GetEnumerator() => innerList.GetEnumerator();

        public int IndexOf(MachRelocation item) => innerList.IndexOf(item);

        internal void FlushIfDirty()
        {
            if (isDirty)
            {
                Span<byte> relocationBuffer = stackalloc byte[8];
                using var stream = relocationData.GetWriteStream();
                uint info;

                foreach (var relocation in innerList)
                {
                    info = relocation.SymbolOrSectionIndex;
                    info |= relocation.IsPCRelative ? 0x1_00_00_00u : 0u;
                    info |= relocation.Length switch { 1 => 0u << 25, 2 => 1u << 25, 4 => 2u << 25, _ => 3u << 25 };
                    info |= relocation.IsExternal ? 0x8_00_00_00u : 0u;
                    info |= (uint)relocation.RelocationType << 28;

                    if (objectFile.IsLittleEndian)
                    {
                        BinaryPrimitives.WriteInt32LittleEndian(relocationBuffer, relocation.Address);
                        BinaryPrimitives.WriteUInt32LittleEndian(relocationBuffer.Slice(4), info);
                    }
                    else
                    {
                        BinaryPrimitives.WriteInt32BigEndian(relocationBuffer, relocation.Address);
                        BinaryPrimitives.WriteUInt32BigEndian(relocationBuffer.Slice(4), info);
                    }

                    stream.Write(relocationBuffer);
                }

                isDirty = false;
            }
        }
    }
}
