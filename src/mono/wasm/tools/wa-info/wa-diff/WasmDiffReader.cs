using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebAssemblyInfo
{
    class WasmDiffReader : WasmReader
    {
        public WasmDiffReader(string path) : base(path) { }

        void CompareSections (SectionInfo section1, SectionInfo section2)
        {
            if (section1.size != section2.size)
            {
                Console.WriteLine($"section id: {section1.id} sizes differ");
                Console.WriteLine($" - size: {section1.size}");
                Console.WriteLine($" + size: {section2.size}");
            }
        }

        public int CompareSummary(WasmDiffReader other)
        {
            if (Reader.BaseStream.Length != other.Reader.BaseStream.Length)
                Console.WriteLine($"Files length difference: {other.Reader.BaseStream.Length - Reader.BaseStream.Length}");

            var processedSections = new HashSet<SectionId>();

            foreach (var id in sections.Keys)
            {
                if (!other.sections.ContainsKey(id))
                    Console.WriteLine($"section id: {id} size: {sections[id].size} *1");

                CompareSections(sections[id], other.sections[id]);
                processedSections.Add(id);
            }

            foreach (var id in other.sections.Keys)
            {
                if (!processedSections.Contains(id))
                    Console.WriteLine($"section id: {id} size: {sections[id].size} *2");
            }

            return 0;
        }

        public int CompareDissasembledFunctions(WasmReader other)
        {
            return 0;
        }
    }
}
