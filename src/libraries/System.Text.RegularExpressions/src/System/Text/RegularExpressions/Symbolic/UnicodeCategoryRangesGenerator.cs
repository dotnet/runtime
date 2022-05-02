// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;

namespace System.Text.RegularExpressions.Symbolic
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

using System.Globalization;

namespace {namespacename}
{{
    internal static class {classname}
    {{");
            var catMap = new Dictionary<UnicodeCategory, Ranges>();
            foreach (UnicodeCategory c in Enum.GetValues<UnicodeCategory>())
            {
                catMap[c] = new Ranges();
            }

            Ranges whitespace = new Ranges();
            Regex whitespaceRegex = new(@"\s");
            for (int i = 0; i <= char.MaxValue; i++)
            {
                char ch = (char)i;
                catMap[char.GetUnicodeCategory(ch)].Add(ch);
                if (whitespaceRegex.IsMatch(ch.ToString()))
                {
                    whitespace.Add(ch);
                }
            }

            var charSetSolver = new CharSetSolver();

            sw.WriteLine("        /// <summary>Serialized BDD representation of the set of all whitespace characters.</summary>");
            sw.Write($"        public static ReadOnlySpan<byte> SerializedWhitespaceBDD => ");
            WriteByteArrayInitSyntax(sw, charSetSolver.CreateSetFromRanges(whitespace.ranges).SerializeToBytes());
            sw.WriteLine(";");

            // Generate a BDD representation of each UnicodeCategory.
            BDD[] catBDDs = new BDD[catMap.Count];
            for (int c = 0; c < catBDDs.Length; c++)
            {
                catBDDs[c] = charSetSolver.CreateSetFromRanges(catMap[(UnicodeCategory)c].ranges);
            }

            sw.WriteLine();
            sw.WriteLine("        /// <summary>Gets the serialized BDD representations of any defined UnicodeCategory.</summary>");
            sw.WriteLine("        public static ReadOnlySpan<byte> GetSerializedCategory(UnicodeCategory category) =>");
            sw.WriteLine("            (int)category switch");
            sw.WriteLine("            {");
            for (int i = 0; i < catBDDs.Length; i++)
            {
                sw.WriteLine($"                {i} => SerializedCategory{i}_{(UnicodeCategory)i},");
            }
            sw.WriteLine($"                _ => default,");
            sw.WriteLine("            };");

            for (int i = 0; i < catBDDs.Length; i++)
            {
                sw.WriteLine();
                sw.WriteLine($"        /// <summary>Serialized BDD representation of the set of all characters in UnicodeCategory.{(UnicodeCategory)i}.</summary>");
                sw.Write($"        private static ReadOnlySpan<byte> SerializedCategory{i}_{(UnicodeCategory)i} => ");
                WriteByteArrayInitSyntax(sw, catBDDs[i].SerializeToBytes());
                sw.WriteLine(";");
            }

            sw.WriteLine($@"    }}
}}");

            static void WriteByteArrayInitSyntax(StreamWriter sw, byte[] values)
            {
                sw.Write("new byte[] {");
                for (int i = 0; i < values.Length; i++)
                {
                    sw.Write($" 0x{values[i]:X},");
                }
                sw.Write(" }");
            }
        }
    }

    /// <summary>Used internally for creating a collection of ranges for serialization.</summary>
    [ExcludeFromCodeCoverage]
    internal sealed class Ranges
    {
        public readonly List<(char Lower, char Upper)> ranges = new List<(char Lower, char Upper)>();

        public void Add(char n)
        {
            // Add the character, extending an existing range if there's already one contiguous
            // with the new character.

            for (int i = 0; i < ranges.Count; i++)
            {
                (char lower, char upper) = ranges[i];
                if (upper == (n - 1))
                {
                    ranges[i] = (lower, n);
                    return;
                }
            }

            ranges.Add((n, n));
        }

        public int Count => ranges.Count;
    }
#endif
}
