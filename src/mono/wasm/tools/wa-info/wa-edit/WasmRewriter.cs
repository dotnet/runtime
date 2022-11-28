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

        void RewriteDataSection()
        {
            //var oo = Writer.BaseStream.Position;
            var bytes = File.ReadAllBytes(Program.DataSectionFile);
            // TODO: support all modes
            var mode = DataMode.Active;
            uint count = 1;
            var sectionLen = U32Len(count) + U32Len((uint)mode) + ConstI32ExprLen((int)Program.DataOffset) + U32Len((uint)bytes.Length) + (uint)bytes.Length;

            // section beginning
            Writer.Write((byte)SectionId.Data);
            WriteU32(sectionLen);

            // section content
            WriteU32(count);
            WriteU32((uint)mode);
            WriteConstI32Expr((int)Program.DataOffset);
            WriteU32((uint)bytes.Length);
            Writer.Write(bytes);

            //var pos = Writer.BaseStream.Position;
            //Writer.BaseStream.Position = oo;
            //DumpBytes(64);
            //Writer.BaseStream.Position = pos;
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
