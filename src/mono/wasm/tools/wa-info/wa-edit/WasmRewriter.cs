using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAssemblyInfo
{
    internal class WasmRewriter : WasmReaderBase
    {
        readonly string DestinationPath;
        BinaryWriter Writer;

        public WasmRewriter(string source, string destination) : base(source)
        {
            if (Program.Verbose)
                Console.WriteLine($"Writing wasm file: {destination}");

            DestinationPath = destination;
            var stream = File.Open(DestinationPath, FileMode.Create);
            Writer = new BinaryWriter(stream);
        }

        protected override void ReadModule()
        {
            Writer.Write(MagicWasm);
            Writer.Write(1); // Version

            base.ReadModule();

            Writer.BaseStream.Position = MagicWasm.Length;
            Writer.Write(Version);
        }

        override protected void ReadSection(SectionInfo section)
        {
            if (section.id == SectionId.Data && File.Exists(Program.DataSectionFile))
            {
                // TODO: rewrite also DataCount section
                RewriteDataSection();
                return;
            }

            WriteSection(section);
        }

        void WriteSection(SectionInfo section)
        {
            Reader.BaseStream.Seek(section.offset, SeekOrigin.Begin);
            Writer.Write(Reader.ReadBytes((int)section.size + (int)(section.begin - section.offset)));
        }

        struct Chunk
        {
            public int index, size;
        }

        List<Chunk> Split(byte[] data)
        {
            int zeroesLen = 9;
            var list = new List<Chunk>();
            var span = new ReadOnlySpan<byte>(data);
            var zeroes = new ReadOnlySpan<byte>(new byte[zeroesLen]);
            int offset = 0;
            int stripped = 0;

            do
            {
                int index = span.IndexOf(zeroes);
                if (index == -1)
                {
                    if (Program.Verbose2)
                        Console.WriteLine($"  add last idx: {offset} size: {data.Length - offset} span remaining len: {span.Length}");

                    list.Add(new Chunk { index = offset, size = data.Length - offset });
                    return list;
                }
                if (index != 0)
                {
                    if (Program.Verbose2)
                        Console.WriteLine($"  add idx: {offset} size: {index} span remaining len: {span.Length} span index: {index}");

                    list.Add(new Chunk { index = offset, size = index });
                    span = span.Slice(index + zeroesLen);
                    offset += index + zeroesLen;
                    stripped += zeroesLen;
                }
                index = span.IndexOfAnyExcept((byte)0);
                if (index == -1)
                {
                    stripped += data.Length - offset;
                    break;
                }

                //Console.WriteLine($"skip: {index}");
                if (index != 0)
                {
                    span = span.Slice(index);
                    offset += index;
                    stripped += index;
                }
            } while (true);

            if (Program.Verbose)
                Console.Write($"    segments detected: {list.Count:N0} zero bytes stripped: {stripped:N0}");

            return list;
        }

        void RewriteDataSection()
        {
            //var oo = Writer.BaseStream.Position;
            var bytes = File.ReadAllBytes(Program.DataSectionFile);
            var chunk = new Chunk { index = 0, size = bytes.Length };
            var segments = Program.DataSectionAutoSplit ? Split(bytes) : new List<Chunk> { chunk };

            // TODO: support all modes
            var mode = DataMode.Active;
            var sectionLen = U32Len((uint)segments.Count);
            foreach (var segment in segments)
                sectionLen += GetDataSegmentLength(mode, segment, Program.DataOffset + segment.index);

            // section beginning
            Writer.Write((byte)SectionId.Data);
            WriteU32(sectionLen);

            // section content
            WriteU32((uint)segments.Count);
            foreach (var segment in segments)
                WriteDataSegment(mode, bytes, segment, Program.DataOffset + segment.index);

            //var pos = Writer.BaseStream.Position;
            //Writer.BaseStream.Position = oo;
            //DumpBytes(64);
            //Writer.BaseStream.Position = pos;
        }

        uint GetDataSegmentLength(DataMode mode, Chunk chunk, int memoryOffset)
        {
            return U32Len((uint)mode) + ConstI32ExprLen(memoryOffset) + U32Len((uint)chunk.size) + (uint)chunk.size;
        }

        void WriteDataSegment(DataMode mode, byte[] data, Chunk chunk, int memoryOffset)
        {
            // data segment
            WriteU32((uint)mode);
            WriteConstI32Expr(memoryOffset);
            WriteU32((uint)chunk.size);
            Writer.Write(data, chunk.index, chunk.size);
        }

        public void DumpBytes(int count)
        {
            var pos = Writer.BaseStream.Position;
            Console.WriteLine("bytes");

            for (int i = 0; i < count; i++)
            {
                Console.Write($" {Writer.BaseStream.ReadByte():x}");
            }

            Console.WriteLine();
            Writer.BaseStream.Position = pos;
        }

        uint ConstI32ExprLen(int cn)
        {
            return 2 + I32Len(cn);
        }

        // i32.const <cn>
        void WriteConstI32Expr(int cn)
        {
            Writer.Write((byte)Opcode.I32_Const);
            WriteI32(cn);
            Writer.Write((byte)Opcode.End);
        }

        public void WriteU32(uint n)
        {
            do
            {
                byte b = (byte)(n & 0x7f);
                n >>= 7;
                if (n != 0)
                    b |= 0x80;
                Writer.Write(b);
            } while (n != 0);
        }

        public static uint U32Len(uint n)
        {
            uint len = 0u;
            do
            {
                n >>= 7;
                len++;
            } while (n != 0);

            return len;
        }

        public void WriteI32(int n)
        {
            var final = false;
            do
            {
                byte b = (byte)(n & 0x7f);
                n >>= 7;

                if ((n == 0 && ((n & 0x80000000) == 0)) || (n == -1 && ((n & 0x80000000) == 0x80)))
                    final = true;
                else
                    b |= 0x80;

                Writer.Write(b);
            } while (!final);
        }

        public static uint I32Len(int n)
        {
            var final = false;
            var len = 0u;
            do
            {
                n >>= 7;

                if ((n == 0 && ((n & 0x80000000) == 0)) || (n == -1 && ((n & 0x80000000) == 0x80)))
                    final = true;

                len++;
            } while (!final);

            return len;
        }
    }
}
