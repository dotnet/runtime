using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiffPlex.DiffBuilder;
using DiffPlex.DiffBuilder.Model;

namespace WebAssemblyInfo
{
    class WasmDiffReader : WasmReader
    {
        public WasmDiffReader(WasmContext context, string path) : base(context, path) { }
        public WasmDiffReader(WasmContext context, Stream stream, long length) : base(context, stream, length) { }

        override protected WasmReader CreateEmbeddedReader(WasmContext context, Stream stream, long length)
        {
            return new WasmDiffReader(context, stream, length);
        }

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
            if (Length != other.Length)
                Console.WriteLine($"Files length difference: {other.Length - Length} bytes");

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

        void CompareDisassembledFunctions(UInt32 idx, string? name, object? data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            (var otherReader, var secondPass) = ((WasmDiffReader, bool))data;
            if (otherReader == null || otherReader.functionTypes == null || otherReader.functions == null || functionTypes == null || functions == null || FuncsCode == null || otherReader.FuncsCode == null)
                throw new InvalidOperationException();

            if (secondPass)
            {
                if (Context.ShowFunctionSize)
                    Console.WriteLine($"code size difference: +{otherReader.FuncsCode[idx].Size} bytes");

                otherReader.PrintFunctionWithPrefix(idx, name, "+ ");

                return;
            }

            // 1st pass
            Function? f1 = functions[idx], f2 = null;
            string? otherName = null;
            uint otherIdx;
            if (GetOtherIndex(name, idx, otherReader, out otherIdx))
            {
                otherName = otherReader.GetFunctionName(otherIdx);
                f2 = otherReader.functions[otherIdx];
                processedIndexes?.Add(otherIdx);
            }

            if (f2 == null)
            {
                if (Context.ShowFunctionSize)
                    Console.WriteLine($"code size difference: -{FuncsCode[idx].Size} bytes");

                PrintFunctionWithPrefix(idx, GetFunctionName(idx), "- ");

                return;
            }

            var functionType1 = functionTypes[functions[idx].TypeIdx];
            var functionType2 = otherReader.functionTypes[otherReader.functions[otherIdx].TypeIdx];
            string sig1 = functionType1.ToString(name);
            string sig2 = functionType2.ToString(otherName);

            string sizeDiff = "";
            int sizeDelta = 0;
            if (Context.ShowFunctionSize)
            {
                sizeDelta = (int)otherReader.FuncsCode[otherIdx].Size - (int)FuncsCode[idx].Size;
                sizeDiff = $" code size difference: {sizeDelta} bytes";
            }

            bool sigPrinted = false;
            if (sig1 != sig2)
            {
                Console.WriteLine($"- {sig1}");
                Console.WriteLine($"+ {sig2}{sizeDiff}");
                sigPrinted = true;
            }

            string code1 = FuncsCode[idx].ToString(this, functionType1.Parameters.Types.Length);
            string code2 = otherReader.FuncsCode[otherIdx].ToString(otherReader, functionType1.Parameters.Types.Length);

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

        public int CompareDissasembledFunctions(WasmReader other)
        {
            FilterFunctions(CompareDisassembledFunctions, other);

            return 0;
        }

        Dictionary<string, int> sizeDiffs = new Dictionary<string, int>();

        void CompareFunctionSizes(UInt32 idx, string? name, object? data)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            (var otherReader, var secondPass) = ((WasmDiffReader, bool))data;
            if (otherReader == null || otherReader.functionTypes == null || otherReader.functions == null || functionTypes == null || functions == null || FuncsCode == null || otherReader.FuncsCode == null)
                throw new InvalidOperationException();

            if (secondPass)
            {
                sizeDiffs[$"+{name}"] = (int)otherReader.FuncsCode[idx].Size;

                return;
            }

            // 1st pass
            Function? f1 = functions[idx], f2 = null;
            string? otherName = null;
            uint otherIdx;
            if (GetOtherIndex(name, idx, otherReader, out otherIdx))
            {
                otherName = otherReader.GetFunctionName(otherIdx);
                f2 = otherReader.functions[otherIdx];
                processedIndexes?.Add(otherIdx);
            }

            if (f2 == null)
            {
                sizeDiffs[$"-{name}"] = -(int)FuncsCode[idx].Size;

                return;
            }

            int delta = (int)otherReader.FuncsCode[otherIdx].Size - (int)FuncsCode[idx].Size;
            if (delta != 0)
                sizeDiffs[$" {name}"] = delta;
        }

        static bool GetOtherIndex(string? name, uint idx, WasmDiffReader other, out uint otherIdx)
        {
            otherIdx = idx;
            return other.HasFunctionNames && other.GetFunctionIdx(name, out otherIdx);
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

            long sum = 0;
            foreach (var v in list)
            {
                Console.WriteLine($"{v.Value,10:n0} | {v.Key}");
                sum += v.Value;
            }

            Console.WriteLine("------------------------------------------------");
            Console.WriteLine($"{sum,10:n0}   bytes total");
            var msize = other.FuncMetadataSize - FuncMetadataSize;
            Console.WriteLine($"{sum + msize,10:n0}   bytes total, including metadata");
            Console.WriteLine("------------------------------------------------");

            return 0;
        }

        HashSet<uint>? processedIndexes;

        protected override void FilterFunctions(ProcessFunction processFunction, object? data = null)
        {
            if (data == null)
                throw new ArgumentNullException("data");

            WasmDiffReader otherReader = (WasmDiffReader)data ?? throw new InvalidOperationException();

            processedIndexes ??= new HashSet<uint>();
            base.FilterFunctions(processFunction, (otherReader, false));

            if (otherReader.functions != null)
            {
                for (UInt32 idx = 0; idx < otherReader.functions.Length; idx++)
                {
                    if (processedIndexes.Contains(idx))
                        continue;

                    if (!otherReader.FilterFunction(idx, out string? name, data))
                        continue;

                    processFunction(idx, name, (otherReader, true));
                }
            }

            processedIndexes = null;
        }
    }
}
