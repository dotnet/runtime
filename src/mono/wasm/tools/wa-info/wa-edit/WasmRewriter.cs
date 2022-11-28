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

        public WasmRewriter(string source, string destination) : base (source)
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
            //if (section.id == SectionId.Data)
            //{
            //    RewriteDataSection();
            //    return;
            //}

            WriteSection(section);
        }

        void WriteSection(SectionInfo section)
        {
            Reader.BaseStream.Seek(section.offset, SeekOrigin.Begin);
            Writer.Write(Reader.ReadBytes((int)section.size + (int)(section.begin - section.offset)));
        }
    }
}
