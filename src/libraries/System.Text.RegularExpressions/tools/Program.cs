// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace GenerateRegexCasingTable
{
    /// <summary>
    /// Program that takes a parameter pointing to UnicodeData.txt and generates a file to be used
    /// as the Regex case equivalence table to be used for matching when using RegexOptions.IgnoreCase
    /// </summary>
    public partial class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length < 1 || !File.Exists(args[0]))
                Console.WriteLine("Error: Please pass in the full path to UnicodeData.txt so that the files can be generated.");

            string unicodeTxtFilePath = args[0];

            // Flip this boolean to true if you want to test the generated table against invariantCulture.ToLower()/invariantCulture.ToUpper()
            bool testCompat = false;
            bool generateTable = true;

            Dictionary<char, char> lowerCasingTable = UnicodeDataCasingParser.Parse(unicodeTxtFilePath, upperCase: false);
            Dictionary<char, char> upperCasingTable = UnicodeDataCasingParser.Parse(unicodeTxtFilePath, upperCase: true);

            (Dictionary<char, int>? equivalenceMap, Dictionary<int, SortedSet<char>>? equivalenceValues) = GenerateMapAndValuesFromCasingTable(lowerCasingTable, upperCasingTable);

            if (testCompat)
            {
                var textInfo = CultureInfo.InvariantCulture.TextInfo;
                // Ensure that all of the calculated equivalences are not introducing new changes to existing Regex behavior.
                foreach (KeyValuePair<char, int> equivalenceMapEntry in equivalenceMap)
                {
                    foreach (char equivalence in equivalenceValues[equivalenceMapEntry.Value])
                    {
                        if (equivalenceMapEntry.Key != equivalence)
                        {
                            if (textInfo.ToLower(equivalenceMapEntry.Key) != equivalence &&
                                textInfo.ToUpper(equivalenceMapEntry.Key) != equivalence &&
                                textInfo.ToLower(equivalence) != equivalenceMapEntry.Key &&
                                textInfo.ToUpper(equivalence) != equivalenceMapEntry.Key)
                            {
                                Console.WriteLine($"There shouldn't be a mapping between \\u{((ushort)equivalenceMapEntry.Key).ToString("X4")} and \\u{((ushort)equivalence).ToString("X4")}");
                            }
                        }
                    }
                }
            }

            if (generateTable)
            {
                DataTable dataTable = new(equivalenceMap, equivalenceValues);
                var fileName = "RegexCaseFolding.Data.cs";
                Console.WriteLine("Generating Regex case folding table...");
                dataTable.GenerateDataTableWithPartitions(64, fileName);
                Console.WriteLine($"Regex case folding table file was generated at: {Path.Combine(Directory.GetCurrentDirectory(), fileName)}");
                Console.WriteLine("Please use it to replace the existing one at src/libraries/System.Text.RegularExpressions/src/System/Text/RegularExpressions/ directory.");
            }
        }

        /// <summary>
        /// Takes a <paramref name="lowerCasingTable"/>, and generates a character map and values to be used for finding equivalence classes
        /// for each unicode character.
        /// </summary>
        /// <param name="lowerCasingTable">The lower casing table to use to generate the equivalence classes.</param>
        /// <param name="upperCasingTable">The upper casing table to use to generate the equivalence classes.</param>
        /// <returns>A pair containing the map and value dictionaries with the equivalence classes.</returns>
        public static (Dictionary<char, int>, Dictionary<int, SortedSet<char>>) GenerateMapAndValuesFromCasingTable(Dictionary<char, char> lowerCasingTable, Dictionary<char, char> upperCasingTable)
        {
            Dictionary<char, int> map = new Dictionary<char, int>();
            Dictionary<int, SortedSet<char>> values = new Dictionary<int, SortedSet<char>>();
            int equivalenceValueCount = 0;

            // Fill the equivalence Map and values Dictinaries using Invariant.ToLower
            foreach (var lowerCaseMapping in lowerCasingTable)
            {
                AddMapping(lowerCaseMapping);
            }

            // Uncomment the following 3 lines if we also want to consider the ToUpper() mappings
            //foreach (var upperCaseMapping in upperCasingTable)
            //{
            //    AddMapping(upperCaseMapping);
            //}

            return (map, values);

            void AddMapping(KeyValuePair<char, char> caseMapping)
            {
                int mapIndex = -1;

                char originalChar = caseMapping.Key;
                char lowerCase = caseMapping.Value;

                if (!map.ContainsKey(originalChar) && !map.ContainsKey(lowerCase))
                {
                    mapIndex = equivalenceValueCount++;
                    map.Add(originalChar, mapIndex);
                    map.Add(lowerCase, mapIndex);
                }
                else if (!map.ContainsKey(originalChar))
                {
                    mapIndex = map[lowerCase];
                    map.Add(originalChar, mapIndex);
                }
                else if (!map.ContainsKey(lowerCase))
                {
                    mapIndex = map[originalChar];
                    map.Add(lowerCase, mapIndex);
                }
                else
                {
                    Debug.Assert(map[originalChar] == map[lowerCase]);
                    return;
                }

                if (!values.TryGetValue(mapIndex, out SortedSet<char>? value))
                {
                    values.Add(mapIndex, new SortedSet<char>(new CharComparer()));
                }

                if (!values[mapIndex].Contains(originalChar))
                    values[mapIndex].Add(originalChar);
                if (!values[mapIndex].Contains(lowerCase))
                    values[mapIndex].Add(lowerCase);
            }
        }
    }

    /// <summary>
    /// In order to be able to use the table in an optimum fashion in Regex, we need the
    /// values for the equivalences to be sorted by char value number. This comparer will
    /// keep the inner lists sorted.
    /// </summary>
    public class CharComparer : IComparer<char>
    {
        public int Compare(char x, char y)
        {
            if ((int)x == (int)y)
                return 0;
            else if ((int)x > (int)y)
                return 1;
            else
                return -1;
        }
    }
}
