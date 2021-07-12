using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace InvariantCasing
{
    public class Program
    {

        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage:");
                Console.WriteLine("InvariantCasing [UnicodeData.txt]");
                Console.WriteLine("Unicode Data File can be downloaded from https://www.unicode.org/Public/UCD/latest/ucd/");
                return;
            }
            Parser p = new();
            p.ParseFile(args[0]);

            p.WriteSurrogateCasing(true);
            p.WriteSurrogateCasing(false);

            p.GenerateTables();

            Console.WriteLine("Done!");
        }
    }

    public class Parser
    {
        private Dictionary<int, int> upperCasing = new();
        private Dictionary<int, int> lowerCasing = new();

        public void ParseFile(string unicodeFileName) 
        {
            try
            {
                using StreamReader stream = new StreamReader(unicodeFileName);
                while (!stream.EndOfStream)
                {
                    string line = stream.ReadLine();
                    string[] parts = line.Split(';');
                    if (parts.Length < 15)
                    {
                        Console.WriteLine($"Error in the line: {line}");
                        return;
                    }

                    int from = int.Parse(parts[0], NumberStyles.HexNumber, null);
                    if (from != 0x0130 && from != 0x0131 && from != 0x017f)
                    {
                        if (parts[13].Length > 0)
                        {
                            lowerCasing[from] = int.Parse(parts[13], NumberStyles.HexNumber, null);
                        }
                        else if (parts[14].Length > 0)
                        {
                            upperCasing[from] = int.Parse(parts[14], NumberStyles.HexNumber, null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"{e}");
            }
        }

        internal void WriteSurrogateCasing(bool upper)
        {
            // H = (S - 0x10000) / 0x400 + 0xD800
            // L = (S - 0x10000) % 0x400 + 0xDC00

            var table = upper ? upperCasing : lowerCasing;

            Console.WriteLine("----------------------------------------------------------------");
            Console.WriteLine(upper ? $" ******* Surrogate Upper Casing" : $" ******* Surrogate Lower Casing");
            Console.WriteLine("----------------------------------------------------------------");

            int last = 0;

            foreach (KeyValuePair<int, int> kvp in table)
            {
                if (kvp.Key > 0xFFFF)
                {
                    int hFrom = (kvp.Key - 0x10000) / 0x400 + 0xD800;
                    int lFrom = (kvp.Key - 0x10000) % 0x400 + 0xDC00;

                    int hTo = (kvp.Value - 0x10000) / 0x400 + 0xD800;
                    int lTo = (kvp.Value - 0x10000) % 0x400 + 0xDC00;

                    if (last+1 != lFrom)
                    {
                        Console.WriteLine();
                    }

                    last = lFrom;

                    Console.WriteLine($"0x{kvp.Key, -10:x}(0x{hFrom,-4:x}, 0x{lFrom,-4:x})  ..... 0x{kvp.Value,-10:x}(0x{hTo,-4:x}, 0x{lTo,-4:x})");
                }
            }
        }

        public void WriteTable(ushort [] table, string name)
        {
            int index = 0;
            Console.WriteLine($"    private static readonly ushort [] {name} = ");
            Console.WriteLine($"    {{");
            Console.WriteLine($"        // 0       1       2       3       4       5       6       7       8       9       A       B       C       D       E       F");
            Console.Write($"        ");

            while (index < table.Length)
            {
                if (index > 0 && index % 16 == 0)
                {
                    Console.WriteLine($"    // 0x{index - 16:x4} - 0x{index - 1:x4}");
                    Console.Write($"        ");
                }
                Console.Write($"0x{table[index]:x4}, ");
                index++;
            }

            Console.WriteLine($"    // 0x{index - 16:x4} - 0x{index - 1:x4}");
            Console.WriteLine($"    }};");
        }

        public void GenerateTables()
        {
            GenerateTable8_4_4(upperCasing, out ushort[] u1, out ushort[] u2, out ushort[] u3);
            GenerateTable8_4_4(lowerCasing, out ushort[] l1, out ushort[] l2, out ushort[] l3);
           
            WriteTable(u1, "UpperCase1");
            WriteTable(u2, "UpperCase2");
            WriteTable(u3, "UpperCase3");
            WriteTable(l1, "LowerCase1");
            WriteTable(l2, "LowerCase2");
            WriteTable(l3, "LowerCase3");

            Console.WriteLine($"Upper Casing Tables Sizes:  u1 = {u1.Length}, u2 = {u2.Length}, u3 = {u3.Length} .... total = {u1.Length + u2.Length + u3.Length}, Total Bytes: {(u1.Length + u2.Length + u3.Length) * sizeof(ushort)} ");
            Console.WriteLine($"lower Casing Tables Sizes:  l1 = {l1.Length}, l2 = {l2.Length}, l3 = {l3.Length} .... total = {l1.Length + l2.Length + l3.Length}, Total Bytes: {(l1.Length + l2.Length + l3.Length) * sizeof(ushort)} ");
            Console.WriteLine($"Total Upper Casing entries: {upperCasing.Keys.Count}  ... in bytes {sizeof(ushort) * upperCasing.Keys.Count}");
            Console.WriteLine($"Total Lower Casing entries: {lowerCasing.Keys.Count}  ... in bytes {sizeof(ushort) * lowerCasing.Keys.Count}");
        }

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static char ToLower(char c)
        //{
        //    ushort v = LowerCase1[c >> 8];
        //    v = LowerCase2[v + ((c >> 4) & 0xF)];
        //    v = LowerCase3[v + (c & 0xF)];

        //    return v == 0 ? c : (char)v;
        //}

        //[MethodImpl(MethodImplOptions.AggressiveInlining)]
        //private static char ToUpper(char c)
        //{
        //    ushort v = UpperCase1[c >> 8];
        //    v = UpperCase2[v + ((c >> 4) & 0xF)];
        //    v = UpperCase3[v + (c & 0xF)];

        //    return v == 0 ? c : (char)v;
        //}

        private static void GenerateTable8_4_4(Dictionary<int, int> rawData, out ushort[] l1, out ushort[] l2, out ushort[] l3)
        {
            Dictionary<string, ushort> level2Hash = new Dictionary<string, ushort>();
            Dictionary<string, ushort> level3Hash = new Dictionary<string, ushort>();

            List<ushort> level1Index = new List<ushort>();
            List<ushort> level2Index = new List<ushort>();
            List<ushort> level3Data = new List<ushort>();

            const ushort planes = 1; // can be 17
            ushort ch = 0;
            ushort valueInHash;

            for (ushort i = 0; i < 256 * planes; i++)
            {
                // Generate level 1 indice

                // This is the row data which contains a row of indice for level 2 table.
                string level2RowData = "";

                for (ushort j = 0; j < 16; j++)
                {
                    // Generate level 2 indice
                    string level3RowData = "";
                    for (ushort k = 0; k < 16; k++)
                    {
                        // Generate level 3 values by grouping 16 values together.
                        // each element of the 16 value group is seperated by ";"

                        if (rawData.TryGetValue(ch, out int value))
                        {
                            // There is data defined for this codepoint.  Use it.
                            level3RowData = level3RowData + value + ";";
                        }
                        else
                        {
                            // There is no data defined for this codepoint.  Use the default value
                            // specified in the ctor.
                            level3RowData = level3RowData + 0 + ";";
                        }
                        ch++;
                    }

                    // Check if the pattern of these 16 values happens before.

                    if (!level3Hash.TryGetValue(level3RowData, out valueInHash))
                    {
                        // This is a new group in the level 3 values.
                        // Get the current count of level 3 group count for this plane.
                        valueInHash = (ushort)level3Data.Count;
                        // Store this count to the hash table, keyed by the pattern of these 16 values.
                        level3Hash[level3RowData] = valueInHash;

                        // Populate the 16 values into level 3 data table for this plane.
                        string[] values = level3RowData.Split(';');
                        foreach (string s in values)
                        {
                            if (s.Length > 0)
                                level3Data.Add(ushort.Parse(s));
                        }

                    }

                    level2RowData = level2RowData + String.Format("{0:x4}", valueInHash) + ",";
                }

                if (!level2Hash.TryGetValue(level2RowData, out valueInHash))
                {
                    // Get the count of the current level 2 index table.
                    valueInHash = (ushort)level2Index.Count;
                    level2Hash[level2RowData] = valueInHash;

                    // Populate the 16 values into level 2 data table for this plane.
                    foreach (string s in level2RowData.Split(','))
                    {
                        if (s.Length > 0)
                            level2Index.Add(ushort.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    }

                }

                // Populate the index values into level 1 index table.
                level1Index.Add(valueInHash);
            }

            l1 = level1Index.ToArray();
            l2 = level2Index.ToArray();
            l3 = level3Data.ToArray();
        }
    }
}
