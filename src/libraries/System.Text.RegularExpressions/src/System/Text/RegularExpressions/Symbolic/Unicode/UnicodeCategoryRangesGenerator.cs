// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

namespace System.Text.RegularExpressions.Symbolic.Unicode
{
#if DEBUG
    /// <summary>Utility for generating unicode category ranges and corresponing binary decision diagrams.</summary>
    [ExcludeFromCodeCoverage]
    internal static class UnicodeCategoryRangesGenerator
    {
        /// <summary>Generator for BDD Unicode category definitions.</summary>
        /// <param name="namespacename">namespace for the class</param>
        /// <param name="classname">name of the class</param>
        /// <param name="path">path where the file classname.cs is written</param>
        public static void Generate(string namespacename, string classname, string path)
        {
            Debug.Assert(namespacename != null);
            Debug.Assert(classname != null);
            Debug.Assert(path != null);

            using StreamWriter sw = new StreamWriter($"{Path.Combine(path, classname)}.cs");
            sw.WriteLine(
$@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This is a programmatically generated file from Regex.GenerateUnicodeTables.
// It provides serialized BDD Unicode category definitions for System.Environment.Version = {Environment.Version}

namespace {namespacename}
{{
    internal static class {classname}
    {{");
            WriteSerializedBDDs(sw);
            sw.WriteLine($@"    }}
}}");
        }

        private static void WriteSerializedBDDs(StreamWriter sw)
        {
            int maxChar = 0xFFFF;
            var catMap = new Dictionary<UnicodeCategory, Ranges>();
            foreach (UnicodeCategory c in Enum.GetValues<UnicodeCategory>())
            {
                catMap[c] = new Ranges();
            }

            Ranges whitespace = new Ranges();
            Regex whitespaceRegex = new(@"\s");
            for (int i = 0; i <= maxChar; i++)
            {
                char ch = (char)i;
                catMap[char.GetUnicodeCategory(ch)].Add(i);

                if (whitespaceRegex.IsMatch(ch.ToString()))
                    whitespace.Add(i);
            }

            //generate bdd reprs for each of the category ranges
            BDD[] catBDDs = new BDD[catMap.Count];
            CharSetSolver bddb = CharSetSolver.Instance;
            for (int c = 0; c < catBDDs.Length; c++)
                catBDDs[c] = bddb.CreateBddForIntRanges(catMap[(UnicodeCategory)c].ranges);

            BDD whitespaceBdd = bddb.CreateBddForIntRanges(whitespace.ranges);

            sw.WriteLine("        /// <summary>Serialized BDD representations of all the Unicode categories.</summary>");
            sw.WriteLine("        public static readonly byte[][] AllCategoriesSerializedBDD = new byte[][]");
            sw.WriteLine("        {");
            for (int i = 0; i < catBDDs.Length; i++)
            {
                sw.WriteLine("            // {0}({1}):", (UnicodeCategory)i, i);
                sw.Write("            ");
                WriteByteArrayInitSyntax(sw, catBDDs[i].SerializeToBytes());
                sw.WriteLine(",");
            }
            sw.WriteLine("        };");
            sw.WriteLine();

            sw.WriteLine("        /// <summary>Serialized BDD representation of the set of all whitespace characters.</summary>");
            sw.Write($"        public static readonly byte[] WhitespaceSerializedBDD = ");
            WriteByteArrayInitSyntax(sw, whitespaceBdd.SerializeToBytes());
            sw.WriteLine(";");
        }

        public static void WriteInt64ArrayInitSyntax(StreamWriter sw, long[] values)
        {
            sw.Write("new long[] {");
            for (int i = 0; i < values.Length; i++)
            {
                sw.Write($" 0x{values[i]:X}, ");
            }
            sw.Write("}");
        }

        public static void WriteByteArrayInitSyntax(StreamWriter sw, byte[] values)
        {
            sw.Write("new byte[] {");
            for (int i = 0; i < values.Length; i++)
            {
                sw.Write($" 0x{values[i]:X}, ");
            }
            sw.Write("}");
        }
    }

    /// <summary>Used internally for creating a collection of ranges for serialization.</summary>
    [ExcludeFromCodeCoverage]
    internal sealed class Ranges
    {
        public readonly List<int[]> ranges = new List<int[]>();

        public void Add(int n)
        {
            for (int i = 0; i < ranges.Count; i++)
            {
                if (ranges[i][1] == (n - 1))
                {
                    ranges[i][1] = n;
                    return;
                }
            }

            ranges.Add(new int[] { n, n });
        }

        public int Count => ranges.Count;
    }
#endif
}
