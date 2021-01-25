// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace GenUnicodeProp
{
    internal sealed class DataTable
    {
        // This contains the data mapping between codepoints and values.
        private readonly SortedDictionary<uint, byte> RawData = new SortedDictionary<uint, byte>();

        private readonly List<byte> Level1Index = new List<byte>();
        private readonly List<ushort> Level2Index = new List<ushort>();
        private bool Level2HasBytes;
        private readonly List<byte> Level3Data = new List<byte>();

        /// <summary>
        /// Add the value data for the specified codepoint.
        /// </summary>
        public void AddData(uint codepoint, byte value) => RawData[codepoint] = value;

        /// <summary>
        /// Create the 12:4:4 data table structure after all the codepoint/value pairs are added by using AddData.
        /// </summary>
        public void GenerateTable(string name, int level2bits = 4, int level3bits = 4, bool cutOff = false)
        {
            Console.WriteLine();
            (int _, string[] lines) = GenerateTable(level2bits, level3bits, cutOff, name, Level1Index, Level2Index, Level3Data);
            foreach (var l in lines)
                Console.WriteLine(l);
        }

        public void CalculateTableVariants(bool cutOff = false)
        {
            foreach (((int l2, int l3), (int total, string[] stats)) in (
              from l2 in Enumerable.Range(1, 7)
              from l3 in Enumerable.Range(1, 7)
              select (l2, l3)).AsParallel().Select(l => (l, res: GenerateTable(l.l2, l.l3, cutOff))).OrderBy(v => v.res.Total))
            {
                Console.WriteLine($"Stats for {l2}:{l3}");
                foreach (var line in stats)
                    Console.WriteLine(line);
            }
        }

        private (int Total, string[] Stats) GenerateTable(int level2bits, int level3bits, bool cutOff, string name = null, List<byte> level1Index = null, List<ushort> level2Index = null, List<byte> level3Data = null)
        {
            if (name != null)
                Console.WriteLine($"Process {20 - level3bits - level2bits}:{level2bits}:{level3bits} table {name}.");

            var level2Hash = new Dictionary<string, ushort>();
            var level3Hash = new Dictionary<string, ushort>();

            const int planes = 17;
            var level1block = planes << (16 - level2bits - level3bits);
            var level2block = 1 << level2bits;
            var level3block = 1 << level3bits;

            var level1Count = 0;
            var level2Count = 0;
            var level3Count = 0;

            var level3RowData = new byte[level3block];
            var level2RowData = new ushort[level2block];

            if (cutOff)
            {
                level1Index ??= new List<byte>();
            }

            // Process plan 0 ~ 16.
            var ch = 0u;
            for (var i = 0; i < level1block; i++)
            {
                // Generate level 1 indice

                // This is the row data which contains a row of indice for level 2 table.
                for (var j = 0; j < level2RowData.Length; j++)
                {
                    // Generate level 2 indice
                    for (var k = 0; k < level3RowData.Length; k++)
                    {
                        // Generate level 3 values by grouping 16 values together.
                        RawData.TryGetValue(ch, out var value);
                        level3RowData[k] = value;
                        ch++;
                    }

                    // Check if the pattern of these 16 values happens before.
                    var level3key = string.Join(";", level3RowData);
                    if (!level3Hash.TryGetValue(level3key, out var valueInHash3))
                    {
                        // This is a new group in the level 3 values.
                        // Get the current count of level 3 group count for this plane.
                        valueInHash3 = checked((ushort)level3Count);
                        // Store this count to the hash table, keyed by the pattern of these 16 values.
                        level3Hash[level3key] = valueInHash3;

                        // Populate the 16 values into level 3 data table for this plane.
                        level3Data?.AddRange(level3RowData);
                        level3Count++;
                    }
                    level2RowData[j] = valueInHash3;
                }

                var level2key = string.Join(";", level2RowData);
                if (!level2Hash.TryGetValue(level2key, out var valueInHash))
                {
                    // Get the count of the current level 2 index table.
                    valueInHash = checked((ushort)level2Count);
                    level2Hash[level2key] = valueInHash;

                    // Populate the 16 values into level 2 data table for this plane.
                    level2Index?.AddRange(level2RowData);
                    level2Count++;
                }
                // Populate the index values into level 1 index table.
                level1Index?.Add(checked((byte)valueInHash));
                level1Count++;
            }

            if (cutOff)
            {
                Array.Fill(level3RowData, default);
                if (level3Hash.TryGetValue(string.Join(";", level3RowData), out var index))
                {
                    Array.Fill(level2RowData, index);
                    if (level2Hash.TryGetValue(string.Join(";", level2RowData), out index))
                    {
                        while (level1Index.Count > 0 && level1Index[level1Index.Count - 1] == index)
                        {
                            level1Index.RemoveAt(level1Index.Count - 1);
                            level1Count--;
                        }
                    }
                }
            }

            var level1uint = level2Hash.Count < 256 ? 1 : 2;
            var level2uint = level3Hash.Count < 256 ? 1 : 2;

            if (level2Index != null)
                Level2HasBytes = level2uint == 1;

            var stats = new string[4];
            stats[0] = $"level 1: {level1Count,4} [{level1Count *= level1uint,5}]{(level1uint > 1 ? "*" : null)}";
            stats[1] = $"level 2: {level2Count,4} [{level2Count *= level2uint * level2block,5}]{(level2uint > 1 ? "*" : null)}";
            stats[2] = $"level 3: {level3Count,4} [{level3Count *= level3block,5}]";

            var total = level1Count + level2Count + level3Count;
            stats[3] = $"Total:         {total,5}";
            return (total, stats);
        }

        public byte[][] GetBytes()
        {
            var level2 = new List<byte>();
            for (var i = 0; i < Level2Index.Count; i++)
            {
                if (Level2HasBytes)
                    level2.Add(checked((byte)Level2Index[i]));
                else
                    level2.AddRange(BitConverter.GetBytes(Level2Index[i]));
            }

            return new[] { Level1Index.ToArray(), level2.ToArray(), Level3Data.ToArray() };
        }
    }
}
