using System;
using System.Collections.Generic;
using System.Linq;
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

        void CompareDisassembledFunction(UInt32 idx, string? name, UInt32 otherIdx, string? otherName, WasmDiffReader? other)
        {
            if (other == null || other.functionTypes == null || other.functions == null || functionTypes == null || functions == null || funcsCode == null || other.funcsCode == null)
                throw new InvalidOperationException();

            Function? f1 = null, f2 = null;

            if (functions != null && idx < functions.Length)
                f1 = functions[idx];

            if (other != null && (otherName != null || !other.HasFunctionNames) && other.functions != null && otherIdx < other.functions.Length)
                f2 = other.functions[otherIdx];

            if (f1 != null && f2 == null)
            {
                if (Program.ShowFunctionSize)
                    Console.WriteLine($"code size difference: -{funcsCode[idx].Size} bytes");

                PrintFunctionWithPrefix(idx, GetFunctionName(idx), "- ");
                return;
            }

            if (f1 == null && f2 != null)
            {
                if (Program.ShowFunctionSize)
                    Console.WriteLine($"code size difference: +{other.funcsCode[idx].Size} bytes");

                other.PrintFunctionWithPrefix(otherIdx, other.GetFunctionName(otherIdx), "+ ");
                return;
            }

            if (name == null)
                name = GetFunctionName(idx);

            if (otherName == null)
                otherName = other.GetFunctionName(otherIdx);

            var functionType1 = functionTypes[functions[idx].TypeIdx];
            var functionType2 = other.functionTypes[other.functions[otherIdx].TypeIdx];
            string sig1 = functionType1.ToString(name);
            string sig2 = functionType2.ToString(otherName);

            string sizeDiff = "";
            int sizeDelta = 0;
            if (Program.ShowFunctionSize)
            {
                sizeDelta = (int)other.funcsCode[idx].Size - (int)funcsCode[idx].Size;
                sizeDiff = $" code size difference: {sizeDelta} bytes";
            }

            bool sigPrinted = false;
            if (sig1 != sig2)
            {
                Console.WriteLine($"- {sig1}");
                Console.WriteLine($"+ {sig2}{sizeDiff}");
                sigPrinted = true;
            }

            string code1 = funcsCode[idx].ToString(this, functionType1.Parameters.Types.Length);
            string code2 = other.funcsCode[otherIdx].ToString(other, functionType1.Parameters.Types.Length);

            if (code1 == code2)
                return;

            if (!sigPrinted)
                Console.WriteLine($"{functionTypes[functions[idx].TypeIdx].ToString(name)}{sizeDiff}");

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

        void CompareDissassembledFunction(UInt32 idx, string name, object? data)
        {
            if (data == null)
                return;

            UInt32 otherIdx;
            var otherReader = (WasmDiffReader)data;

            if (otherReader.HasFunctionNames && otherReader != null && otherReader.GetFunctionIdx(name, out otherIdx))
                CompareDisassembledFunction(idx, name, otherIdx, name, otherReader);
            else
                CompareDisassembledFunction(idx, name, idx, null, otherReader);
        }

        public int CompareDissasembledFunctions(WasmReader other)
        {
            FilterFunctions(CompareDissassembledFunction, other);

            return 0;
        }

        Dictionary<string, int> sizeDiffs = new Dictionary<string, int>();

        void CompareFunctionSizes(UInt32 idx, string? name, UInt32 otherIdx, string? otherName, WasmDiffReader? other)
        {
            if (other == null || other.functionTypes == null || other.functions == null || functionTypes == null || functions == null || funcsCode == null || other.funcsCode == null)
                throw new InvalidOperationException();

            Function? f1 = null, f2 = null;

            if (functions != null && idx < functions.Length)
                f1 = functions[idx];

            if (other != null && (otherName != null || !other.HasFunctionNames) && other.functions != null && otherIdx < other.functions.Length)
                f2 = other.functions[otherIdx];

            if (f1 != null && f2 == null)
            {
                sizeDiffs[$"-{GetFunctionName(idx)}"] = -(int)funcsCode[idx].Size;

                return;
            }

            if (f1 == null && f2 != null)
            {
                sizeDiffs[$"+{other.GetFunctionName(idx)}"] = (int)other.funcsCode[idx].Size;

                return;
            }

            if (name == null)
                name = GetFunctionName(idx);

            if (otherName == null)
                otherName = other.GetFunctionName(otherIdx);

            if (name == null && otherName == null)
                return;

            sizeDiffs[name == null ? otherName : name] = (int)other.funcsCode[idx].Size - (int)funcsCode[idx].Size;
        }

        void CompareFunctionSizes(UInt32 idx, string name, object? data)
        {
            if (data == null)
                return;

            UInt32 otherIdx;
            var otherReader = (WasmDiffReader)data;
            if (otherReader.HasFunctionNames && otherReader != null && otherReader.GetFunctionIdx(name, out otherIdx))
                CompareFunctionSizes(idx, name, otherIdx, name, otherReader);
            else
                CompareFunctionSizes(idx, name, idx, null, otherReader);
        }

        public int CompareFunctions(WasmReader other)
        {
            FilterFunctions(CompareFunctionSizes, other);

            var list = sizeDiffs.ToList();
            list.Sort((v1, v2) => v1.Value.CompareTo(v2.Value));

            Console.WriteLine("function code size difference in ascending order");
            Console.WriteLine("------------------------------------------------");
            Console.WriteLine("difference | function name");
            Console.WriteLine("in bytes   |");
            Console.WriteLine("------------------------------------------------");

            foreach (var v in list)
            {
                Console.WriteLine($"{v.Value,10} | {v.Key}");
            }

            return 0;
        }
    }
}
