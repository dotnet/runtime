using System;
using System.Collections.Generic;

using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

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

        void CompareFunction(UInt32 idx, string? name, UInt32 otherIdx, string? otherName, WasmDiffReader? other)
        {
            if (other == null || other.functionTypes == null || other.functions == null || functionTypes == null || functions == null || funcsCode == null || other.funcsCode == null)
                throw new InvalidOperationException();

            Function? f1 = null, f2 = null;

            if (functions != null && idx < functions.Length)
                f1 = functions[idx];

            if (other != null && other.functions != null && otherIdx < other.functions.Length)
                f2 = other.functions[otherIdx];

            if (f1 != null && f2 == null)
            {
                PrintFunctionWithPrefix(idx, GetFunctionName(idx), "- ");
                return;
            }

            if (f1 == null && f2 != null)
            {
                other.PrintFunctionWithPrefix(otherIdx, other.GetFunctionName(otherIdx), "+ ");
                return;
            }

            if (name == null)
                name = GetFunctionName(idx);

            if (otherName == null)
                otherName = other.GetFunctionName(otherIdx);

            string sig1 = functionTypes[functions[idx].TypeIdx].ToString(name);
            string sig2 = other.functionTypes[other.functions[otherIdx].TypeIdx].ToString(otherName);

            bool sigPrinted = false;
            if (sig1 != sig2)
            {
                Console.WriteLine($"- {sig1}");
                Console.WriteLine($"+ {sig2}");
                sigPrinted = true;
            }

            string code1 = funcsCode[idx].ToString(this);
            string code2 = other.funcsCode[otherIdx].ToString(other);

            if (code1 == code2)
                return;

            if (!sigPrinted)
                Console.WriteLine($"{functionTypes[functions[idx].TypeIdx].ToString(name)}");

            var diff = InlineDiffBuilder.Diff(code1, code2);
            string? lineM1 = null, lineM2 = null;
            int after = 0;
            var origColor = Console.ForegroundColor;
            foreach (var line in diff.Lines)
            {
                switch (line.Type)
                {
                    case ChangeType.Inserted:
                        PrintPrevLines(lineM2, lineM1);
                        lineM1 = lineM2 = null;
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine($"+ {line.Text}");
                        Console.ForegroundColor = origColor;
                        after = 2;
                        break;
                    case ChangeType.Deleted:
                        PrintPrevLines(lineM2, lineM1);
                        lineM1 = lineM2 = null;
                        Console.ForegroundColor = ConsoleColor.Red;
                        Console.WriteLine($"- {line.Text}");
                        Console.ForegroundColor = origColor;
                        after = 2;
                        break;
                    default:
                        if (after > 0)
                        {
                            Console.WriteLine($"  {line.Text}");
                            after--;
                        }
                        else
                        {
                            lineM2 = lineM1;
                            lineM1 = line.Text;
                        }
                        break;
                }
            }

            Console.WriteLine();
        }

        void PrintPrevLines(string? l1, string? l2)
        {
            if (l1 != null && l2 != null)
                Console.WriteLine("...");
            if (l1 != null)
                Console.WriteLine($"  {l1}");
            if (l2 != null)
                Console.WriteLine($"  {l2}");
        }

        void CompareFunction(UInt32 idx, string name, object? data)
        {
            UInt32 otherIdx;

            if (HasFunctionNames && data != null && ((WasmDiffReader)data).GetFunctionIdx(name, out otherIdx))
                CompareFunction(idx, name, otherIdx, name, (WasmDiffReader?)data);
            else
                CompareFunction(idx, name, idx, null, (WasmDiffReader?)data);
        }

        public int CompareDissasembledFunctions(WasmReader other)
        {
            FilterFunctions(CompareFunction, other);

            return 0;
        }
    }
}
