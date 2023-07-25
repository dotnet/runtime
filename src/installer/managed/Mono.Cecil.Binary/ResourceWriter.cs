//
// ResourceWriter.cs
//
// Author:
//   Jb Evain (jbevain@gmail.com)
//
// (C) 2006 Jb Evain
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Diagnostics;
using System.Text;

namespace Mono.Cecil.Binary
{
    public sealed class ResourceWriter
    {
        ResourceDirectoryTable m_table;
        Section m_rsrc;
        MemoryBinaryWriter m_writer;

        readonly List<ResourceDirectoryTable> _directoryTables;
        readonly List<ResourceDataEntry> _dataEntries;
        readonly List<ResourceDirectoryString> _stringEntries;

        long m_pos;

        public ResourceWriter(ResourceDirectoryTable table, Section rsrc, MemoryBinaryWriter writer)
        {
            m_table = table;
            m_rsrc = rsrc;
            m_writer = writer;

            _directoryTables = new List<ResourceDirectoryTable>();
            _dataEntries = new List<ResourceDataEntry>();
            _stringEntries = new List<ResourceDirectoryString>();
        }

        public void Write()
        {
            if (m_table == null)
                return;

            ComputeOffset(m_table);

            foreach (ResourceDirectoryTable table in _directoryTables)
                WriteResourceDirectoryTable(table);
            foreach (ResourceDataEntry data in _dataEntries)
                WriteResourceDataEntry(data);
            foreach (ResourceDirectoryString name in _stringEntries)
                WriteResourceDirectoryString(name);
        }

        void ComputeOffset(ResourceDirectoryTable root)
        {
            int offset = 0;

            _directoryTables.Add(root);

            for (int i = 0; i < _directoryTables.Count; i++)
            {
                ResourceDirectoryTable rdt = _directoryTables[i];
                rdt.Offset = offset;
                offset += 16;

                ResourceDirectoryEntry[] namedEntries = rdt.Entries.Cast<ResourceDirectoryEntry>().Where(x => x.IdentifiedByName).ToArray();
                ResourceDirectoryEntry[] idEntries = rdt.Entries.Cast<ResourceDirectoryEntry>().Where(x => !x.IdentifiedByName).ToArray();

                foreach (ResourceDirectoryEntry rde in namedEntries)
                {
                    rde.Offset = offset;
                    offset += 8;
                    _stringEntries.Add(rde.Name);

                    if (rde.Child is ResourceDirectoryTable table)
                        _directoryTables.Add(table);
                    else
                        _dataEntries.Add((ResourceDataEntry)rde.Child);
                }

                foreach (ResourceDirectoryEntry rde in idEntries)
                {
                    rde.Offset = offset;
                    offset += 8;

                    if (rde.Child is ResourceDirectoryTable table)
                        _directoryTables.Add(table);
                    else
                        _dataEntries.Add((ResourceDataEntry)rde.Child);
                }
            }

            foreach (ResourceDataEntry rde in _dataEntries)
            {
                rde.Offset = offset;
                offset += 16;
            }

            foreach (ResourceDirectoryString rds in _stringEntries)
            {
                rds.Offset = offset;
                byte[] str = Encoding.Unicode.GetBytes(rds.String);
                offset += 2 + str.Length;

                offset += 3;
                offset &= ~3;
            }

            foreach (ResourceDataEntry rde in _dataEntries)
            {
                rde.Data = (uint)offset;

                offset += rde.ResourceData.Length;
                offset += 3;
                offset &= ~3;
            }

            m_writer.Write(new byte [offset]);
        }

        void WriteResourceDirectoryTable(ResourceDirectoryTable rdt)
        {
            GotoOffset(rdt.Offset);

            m_writer.Write(rdt.Characteristics);
            m_writer.Write(rdt.TimeDateStamp);
            m_writer.Write(rdt.MajorVersion);
            m_writer.Write(rdt.MinorVersion);

            ResourceDirectoryEntry[] namedEntries = rdt.Entries.Cast<ResourceDirectoryEntry>().Where(x => x.IdentifiedByName).ToArray();
            ResourceDirectoryEntry[] idEntries = rdt.Entries.Cast<ResourceDirectoryEntry>().Where(x => !x.IdentifiedByName).ToArray();

            m_writer.Write((ushort)namedEntries.Length);
            m_writer.Write((ushort)idEntries.Length);
            RestoreOffset();

            foreach (ResourceDirectoryEntry rde in namedEntries)
                WriteResourceDirectoryEntry(rde);

            foreach (ResourceDirectoryEntry rde in idEntries)
                WriteResourceDirectoryEntry(rde);
        }

        void WriteResourceDirectoryEntry(ResourceDirectoryEntry rde)
        {
            GotoOffset(rde.Offset);

            if (rde.IdentifiedByName)
                m_writer.Write((uint)rde.Name.Offset | 0x80000000);
            else
                m_writer.Write((uint)rde.ID);


            if (rde.Child is ResourceDirectoryTable table)
                m_writer.Write((uint)table.Offset | 0x80000000);
            else
                m_writer.Write(rde.Child.Offset);
            RestoreOffset();
        }

        void WriteResourceDataEntry(ResourceDataEntry rde)
        {
            GotoOffset(rde.Offset);

            m_writer.Write(rde.Data + m_rsrc.VirtualAddress);
            m_writer.Write((uint)rde.ResourceData.Length);
            m_writer.Write(rde.Codepage);
            m_writer.Write(rde.Reserved);

            m_writer.BaseStream.Position = rde.Data;
            m_writer.Write(rde.ResourceData);

            RestoreOffset();
        }

        void WriteResourceDirectoryString(ResourceDirectoryString name)
        {
            GotoOffset(name.Offset);

            byte[] str = Encoding.Unicode.GetBytes(name.String);
            m_writer.Write((ushort)name.String.Length);
            m_writer.Write(str);

            RestoreOffset();
        }

        void GotoOffset(int offset)
        {
            Debug.Assert(m_pos == 0, "GotoOffset in GotoOffset-RestoreOffset pair detected.");
            m_pos = m_writer.BaseStream.Position;
            m_writer.BaseStream.Position = offset;
        }

        void RestoreOffset()
        {
            m_writer.BaseStream.Position = m_pos;
            m_pos = 0;
        }
    }
}
