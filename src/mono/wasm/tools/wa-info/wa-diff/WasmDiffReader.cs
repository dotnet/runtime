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

        void CompareSections(SectionInfo section1, SectionInfo section2)
        {
            if (section1.size != section2.size)
            {
                Console.WriteLine($"section id: {section1.id} sizes differ by {(long)section2.size - (long)section1.size} bytes");
                Console.WriteLine($" - size: {section1.size}");
                Console.WriteLine($" + size: {section2.size}");
            }
        }

        public int CompareSummary(WasmDiffReader other)
        {
            if (Reader.BaseStream.Length != other.Reader.BaseStream.Length)
                Console.WriteLine($"Files length difference: {other.Reader.BaseStream.Length - Reader.BaseStream.Length} bytes");

            var processedSections = new HashSet<SectionId>();

            foreach (var id in sectionsById.Keys)
            {
                var otherContainsId = other.sectionsById.ContainsKey(id);
                if (!otherContainsId || sectionsById[id].Count != other.sectionsById[id].Count)
                {
                    var otherCount = otherContainsId ? other.sectionsById[id].Count : 0;
                    Console.WriteLine($"{id} sections count differ");
                    Console.WriteLine($" - count: {sectionsById[id].Count}");
                    Console.WriteLine($" + count: {otherCount}");

                    continue;
                }

                foreach (var section in sectionsById[id])
                {
                    if (!other.sectionsById.ContainsKey(id))
                        Console.WriteLine($"section id: {id} size: {section.size} *1");

                    for (int i = 0; i < sectionsById[id].Count; i++)
                        CompareSections(sectionsById[id][i], other.sectionsById[id][i]);
                }

                processedSections.Add(id);
            }

            foreach (var id in other.sectionsById.Keys)
            {
                if (processedSections.Contains(id))
                    continue;

                foreach (var section in sectionsById[id])
                    Console.WriteLine($"section id: {id} size: {section.size} *2");
            }

            return 0;
        }

        public int CompareDissasembledFunctions(WasmReader other)
        {
            return 0;
        }
    }
}
