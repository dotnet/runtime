// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace LinkerMapTool
{
    public class Section
    {
        public readonly int SectionIndex;
        public readonly int StartRva;
        public readonly int Length;
        public readonly string SectionName;
        public readonly string ClassName;

        public Section(int sectionIndex, int startRva, int length, string sectionName, string className)
        {
            SectionIndex = sectionIndex;
            StartRva = startRva;
            Length = length;
            SectionName = sectionName;
            ClassName = className;
        }
    }

    public class LinkerMap
    {
        private enum MapSection
        {
            Unknown,
            Sections,
            Publics,
        }

        private class SymbolRva
        {
            public readonly int SectionIndex;
            public readonly int StartRva;
            public readonly string SymbolName;
            public readonly string LibObjectName;

            public SymbolRva(int sectionIndex, int startRva, string symbolName, string libObjectName)
            {
                SectionIndex = sectionIndex;
                StartRva = startRva;
                SymbolName = symbolName;
                LibObjectName = libObjectName;
            }
        }

        /// <summary>
        /// Raw list of sections parsed from the map file
        /// </summary>
        public readonly List<Section> SectionList;

        /// <summary>
        /// Maps section names to their sizes.
        /// </summary>
        public readonly Dictionary<string, int> Sections;

        /// <summary>
        /// Maps section classes to their sizes.
        /// </summary>
        public readonly Dictionary<string, int> SectionClasses;

        /// <summary>
        /// Maps method names to their sizes.
        /// </summary>
        public readonly Dictionary<string, int> Methods;

        /// <summary>
        /// Maps static variable names to their sizes.
        /// </summary>
        public readonly Dictionary<string, int> Statics;

        /// <summary>
        /// Map from publics to object file names
        /// </summary>
        public readonly Dictionary<string, string> ObjectFiles;

        /// <summary>
        /// Total section size
        /// </summary>
        public int SectionSize;

        private LinkerMap()
        {
            SectionList = new List<Section>();
            Sections = new Dictionary<string, int>();
            SectionClasses = new Dictionary<string, int>();
            Methods = new Dictionary<string, int>();
            Statics = new Dictionary<string, int>();
            ObjectFiles = new Dictionary<string, string>();
        }

        public static LinkerMap Parse(string mapFile)
        {
            LinkerMap map = new LinkerMap();
            using (StreamReader reader = new StreamReader(mapFile))
            {
                Dictionary<int, List<SymbolRva>> symbolsPerSection = new Dictionary<int, List<SymbolRva>>();
                MapSection currentSection = MapSection.Unknown;
                int lineIndex = 0;

                do
                {
                    lineIndex++;
                    string line = reader.ReadLine().TrimStart();
                    if (string.IsNullOrEmpty(line))
                    {
                        continue;
                    }

                    try
                    {
                        bool isNumeric = (line.Length >= 5 && IsHexDigit(line[0]) && IsHexDigit(line[1]) && IsHexDigit(line[2]) && IsHexDigit(line[3]) && line[4] == ':');

                        if (isNumeric)
                        {
                            switch (currentSection)
                            {
                                case MapSection.Sections:
                                    ParseSection(line, map.SectionList);
                                    break;

                                case MapSection.Publics:
                                    ParsePublic(line, symbolsPerSection);
                                    break;

                                case MapSection.Unknown:
                                    break;

                                default:
                                    throw new NotImplementedException(currentSection.ToString());
                            }
                        }
                        else if (line.StartsWith("Start "))
                        {
                            currentSection = MapSection.Sections;
                        }
                        else if (line.StartsWith("Address "))
                        {
                            currentSection = MapSection.Publics;
                        }
                        else
                        {
                            currentSection = MapSection.Unknown;
                        }
                    }
                    catch (Exception ex)
                    {
                        throw new Exception($"Error {mapFile}({lineIndex}): {ex.Message}");
                    }
                }
                while (!reader.EndOfStream);

                foreach (Section section in map.SectionList)
                {
                    map.Sections.Add(section.SectionName, section.Length);
                    map.SectionSize += section.Length;
                    map.SectionClasses.TryGetValue(section.ClassName, out int length);
                    map.SectionClasses[section.ClassName] = length + section.Length;
                }
                foreach (KeyValuePair<int, List<SymbolRva>> sectionSymbols in symbolsPerSection)
                {
                    sectionSymbols.Value.Sort((symbolRva1, symbolRva2) => symbolRva1.StartRva.CompareTo(symbolRva2.StartRva));
                    int sectionLength = 0;
                    bool isData = false;
                    foreach (Section section in map.SectionList.Where(sec => sec.SectionIndex == sectionSymbols.Key))
                    {
                        if (section.StartRva + section.Length > sectionLength)
                        {
                            sectionLength = section.StartRva + section.Length;
                        }
                        if (section.ClassName == "DATA")
                        {
                            isData = true;
                        }
                    }
                    sectionSymbols.Value.Add(new SymbolRva(sectionSymbols.Key, sectionLength, "<end-of-section>", null));
                    for (int nextSymbolIndex = 1; nextSymbolIndex < sectionSymbols.Value.Count; nextSymbolIndex++)
                    {
                        SymbolRva symbol = sectionSymbols.Value[nextSymbolIndex - 1];
                        SymbolRva nextSymbol = sectionSymbols.Value[nextSymbolIndex - 0];
                        int length = nextSymbol.StartRva - symbol.StartRva;
                        map.ObjectFiles.Add(symbol.SymbolName, symbol.LibObjectName);
                        Dictionary<string, int> targetDictionary = (isData ? map.Statics : map.Methods);
                        targetDictionary.Add(symbol.SymbolName, length);
                    }
                }
            }
            return map;
        }

        private static int GetHexDigitValue(char c)
        {
            if (c >= '0' && c <= '9')
            {
                return (int)(c - '0');
            }
            if (c >= 'A' && c <= 'F')
            {
                return (int)(c - 'A') + 10;
            }
            if (c >= 'a' && c <= 'f')
            {
                return (int)(c - 'a') + 10;
            }
            return -1;
        }

        private static bool IsHexDigit(char c)
        {
            return GetHexDigitValue(c) >= 0;
        }

        private static int ParseHexNumber(string line, ref int index, int length, char separator = '\0')
        {
            int endIndex = index + length;
            if (endIndex >= line.Length)
            {
                throw new Exception($"cannot parse {length}-digit hex number at index {index}");
            }
            int value = 0;
            for (; index < endIndex; index++)
            {
                char hexDigitChar = line[index];
                int hexDigit = GetHexDigitValue(hexDigitChar);
                if (hexDigit < 0)
                {
                    throw new Exception($"invalid hex digit '{hexDigitChar}'");
                }
                value = value * 16 + hexDigit;
            }
            if (separator != '\0')
            {
                if (index >= line.Length || line[index] != separator)
                {
                    throw new Exception($"'{separator}' expected at index {index}");
                }
                index++;
            }
            return value;
        }

        private static void SkipWhitespace(string line, ref int index)
        {
            while (index < line.Length && Char.IsWhiteSpace(line[index]))
            {
                index++;
            }
        }

        private static string ParseSymbol(string line, ref int index)
        {
            int startIndex = index;
            while (index < line.Length && !Char.IsWhiteSpace(line[index]))
            {
                index++;
            }
            if (index <= startIndex)
            {
                throw new Exception($"cannot parse symbol at index {startIndex}");
            }
            return line.Substring(startIndex, index - startIndex);
        }

        private static void ParseSection(string line, List<Section> sections)
        {
            int index = 0;
            int sectionIndex = ParseHexNumber(line, ref index, length: 4, separator: ':');
            int startRva = ParseHexNumber(line, ref index, length: 8);
            SkipWhitespace(line, ref index);
            int length = ParseHexNumber(line, ref index, length: 8, separator: 'H');
            SkipWhitespace(line, ref index);
            string sectionName = ParseSymbol(line, ref index);
            SkipWhitespace(line, ref index);
            string className = ParseSymbol(line, ref index);

            sections.Add(new Section(sectionIndex, startRva, length, sectionName, className));
        }

        private static void ParsePublic(string line, Dictionary<int, List<SymbolRva>> publics)
        {
            int index = 0;
            int sectionIndex = ParseHexNumber(line, ref index, 4, separator: ':');
            int rva = ParseHexNumber(line, ref index, 8);
            SkipWhitespace(line, ref index);
            string symbolName = ParseSymbol(line, ref index);
            SkipWhitespace(line, ref index);
            // Unused - Rva+Base
            ParseHexNumber(line, ref index, 16);
            // f & i flags (no idea what they mean)
            index += 5;
            string libObjectName = ParseSymbol(line, ref index);

            if (libObjectName == "<absolute>")
            {
                // Ignore absolute symbols
                return;
            }

            if (!publics.TryGetValue(sectionIndex, out List<SymbolRva> publicsPerSection))
            {
                publicsPerSection = new List<SymbolRva>();
                publics.Add(sectionIndex, publicsPerSection);
            }
            publicsPerSection.Add(new SymbolRva(sectionIndex, rva, symbolName, libObjectName));
        }
    };

    class MapSummary
    {
        public readonly List<Section> LeftSections;
        public readonly int LeftSectionSize;
        public readonly List<Section> RightSections;
        public readonly int RightSectionSize;

        public MapSummary(List<Section> leftSections, int leftSectionSize, List<Section> rightSections, int rightSectionSize)
        {
            LeftSections = leftSections;
            LeftSectionSize = leftSectionSize;
            RightSections = rightSections;
            RightSectionSize = rightSectionSize;
        }
    }

    class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                new Program().TryMain(args);
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine(ex.Message);
                return 1;
            }
        }

        private void TryMain(string[] args)
        {
            for (int argIndex = 0; argIndex < args.Length; argIndex++)
            {
                string arg = args[argIndex];
                if (arg.ToLower() == "-d")
                {
                    // Compare directories
                    argIndex += 2;
                    if (argIndex >= args.Length)
                    {
                        throw new Exception("Error: option '-d' should be followed by a pair of directory names");
                    }
                    string dir1 = args[argIndex - 1];
                    string dir2 = args[argIndex - 0];
                    CompareDirectories(dir1, dir2);
                }
                else
                {
                    // Compare individual files
                    argIndex += 1;
                    if (argIndex >= args.Length)
                    {
                        throw new Exception($"Error: '{arg}' should be followed by the second map file name for diffing");
                    }
                    string file2 = args[argIndex - 0];
                    CompareFiles(arg, file2);
                }
            }
        }

        private void CompareDirectories(string dir1, string dir2)
        {
            Dictionary<string, int> leftMapFiles = new Dictionary<string, int>();
            Dictionary<string, int> rightMapFiles = new Dictionary<string, int>();

            Dictionary<string, int> leftSections = new Dictionary<string, int>();
            Dictionary<string, int> rightSections = new Dictionary<string, int>();

            Dictionary<string, int> leftSectionClasses = new Dictionary<string, int>();
            Dictionary<string, int> rightSectionClasses = new Dictionary<string, int>();

            HashSet<string> dir1Files = new HashSet<string>(Directory.EnumerateFiles(dir1, "*.map").Select(file => Path.GetFileName(file)));
            foreach (string dir2File in Directory.EnumerateFiles(dir2, "*.map"))
            {
                string fileName = Path.GetFileName(dir2File);
                if (dir1Files.Contains(fileName))
                {
                    string dir1File = Path.Combine(dir1, fileName);
                    MapSummary summary = CompareFiles(dir1File, dir2File);
                    leftMapFiles.Add(fileName, summary.LeftSectionSize);
                    rightMapFiles.Add(fileName, summary.RightSectionSize);
                    foreach (Section section in summary.LeftSections)
                    {
                        AddSize(leftSections, section.SectionName, section.Length);
                        AddSize(leftSectionClasses, section.ClassName, section.Length);
                    }
                    foreach (Section section in summary.RightSections)
                    {
                        AddSize(rightSections, section.SectionName, section.Length);
                        AddSize(rightSectionClasses, section.ClassName, section.Length);
                    }
                }
            }

            DiffLengthMaps(leftMapFiles, rightMapFiles, "Map File", skipZeroDiffEntries: false);
            DiffLengthMaps(leftSectionClasses, rightSectionClasses, "Section Classes (totals)", skipZeroDiffEntries: true);
            DiffLengthMaps(leftSections, rightSections, "Sections (totals)", skipZeroDiffEntries: true);
        }

        private MapSummary CompareFiles(string leftFile, string rightFile)
        {
            Console.WriteLine($"Left file:  {leftFile}");
            Console.WriteLine($"Right file: {rightFile}");
            Console.WriteLine();

            LinkerMap leftMap = LinkerMap.Parse(leftFile);
            LinkerMap rightMap = LinkerMap.Parse(rightFile);

            DiffLengthMaps(leftMap.SectionClasses, rightMap.SectionClasses, "Section Classes", skipZeroDiffEntries: true);
            DiffLengthMaps(leftMap.Sections, rightMap.Sections, "Sections", skipZeroDiffEntries: true);
            DiffLengthMaps(leftMap.Statics, rightMap.Statics, "Statics", skipZeroDiffEntries: true);
            DiffMethods(leftMap.Methods, rightMap.Methods);

            return new MapSummary(leftMap.SectionList, leftMap.SectionSize, rightMap.SectionList, rightMap.SectionSize);
        }

        private static void AddSize(Dictionary<string, int> map, string key, int sizeToAdd)
        {
            map.TryGetValue(key, out int length);
            map[key] = length + sizeToAdd;
        }

        /// <summary>
        /// Display differences between two name to size dictionaries representing various map file
        /// objects. The two dictionaries are matched and sorted by names (keys).
        /// </summary>
        /// <param name="leftMap">Left dictionary to compare</param>
        /// <param name="rightMap">Right dictionary to compare</param>
        /// <param name="description">Descriptive logical name of the elements being compared</param>
        private void DiffLengthMaps(Dictionary<string, int> leftMap, Dictionary<string, int> rightMap, string description, bool skipZeroDiffEntries = false)
        {
            IEnumerable<string> allKeys = leftMap.Keys.Union(rightMap.Keys);

            Console.WriteLine("     Delta       Left (Hex)           Right (Hex)        Name / {0}", description);
            Console.WriteLine(new string('-', 64 + description.Length));
            DumpDiffLength(leftMap, rightMap, allKeys.OrderBy(name => name), skipZeroDiffEntries);

            Console.WriteLine("     Delta       Left (Hex)           Right (Hex)        Delta / {0}", description);
            Console.WriteLine(new string('-', 65 + description.Length));
            DumpDiffLength(leftMap, rightMap,
                allKeys.OrderBy(key => (leftMap.ContainsKey(key) ? leftMap[key] : 0) - (rightMap.ContainsKey(key) ? rightMap[key] : 0)),
                skipZeroDiffEntries);
        }

        private void DumpDiffLength(Dictionary<string, int> leftMap, Dictionary<string, int> rightMap, IEnumerable<string> keyOrdering, bool skipZeroDiffEntries)
        {
            foreach (string key in keyOrdering)
            {
                leftMap.TryGetValue(key, out int leftSize);
                rightMap.TryGetValue(key, out int rightSize);
                if (leftSize != rightSize || !skipZeroDiffEntries)
                {
                    Console.WriteLine("{0,10} {1,10} ({1:X8}) {2,10} ({2:X8})   {3}",
                        rightSize - leftSize,
                        leftSize,
                        rightSize,
                        key);
                }
            }

            int totalLeftSize = leftMap.Values.Sum();
            int totalRightSize = rightMap.Values.Sum();
            Console.WriteLine("{0,10} {1,10} ({1:X8}) {2,10} ({2:X8})   (total)",
                totalRightSize - totalLeftSize,
                totalLeftSize,
                totalRightSize);
            Console.WriteLine();
        }

        /// <summary>
        /// Display a special diff for methods including a list of added / removed methods
        /// and per-method size differences for methods present in both left and right map.
        /// </summary>
        /// <param name="leftMap">Left method map (name to size) for the comparison</param>
        /// <param name="rightMap">Right method map (name to size) for the comparison</param>
        private void DiffMethods(Dictionary<string, int> leftMap, Dictionary<string, int> rightMap)
        {
            List<string> addedMethods = new List<string>();
            int addedMethodSize = 0;
            List<string> removedMethods = new List<string>();
            int removedMethodSize = 0;
            List<string> methodsChangedSize = new List<string>();
            int equivalentMethodCount = 0;
            int equivalentMethodLeftSize = 0;
            int equivalentMethodRightSize = 0;
            foreach (string key in leftMap.Keys.Union(rightMap.Keys))
            {
                bool leftExist = leftMap.TryGetValue(key, out int leftSize);
                bool rightExist = rightMap.TryGetValue(key, out int rightSize);
                if (!leftExist)
                {
                    addedMethods.Add(key);
                    addedMethodSize += rightSize;
                }
                else if (!rightExist)
                {
                    removedMethods.Add(key);
                    removedMethodSize += leftSize;
                }
                else
                {
                    equivalentMethodCount++;
                    equivalentMethodLeftSize += leftSize;
                    equivalentMethodRightSize += rightSize;
                    if (leftSize != rightSize)
                    {
                        methodsChangedSize.Add(key);
                    }
                }
            }

            Console.WriteLine("     Delta       Left      Right    Count   Code category");
            Console.WriteLine(new string('-', 50));

            Console.WriteLine("{0,10} {1,10} {2,10} {3,8}   {4}",
                addedMethodSize,
                0,
                addedMethodSize,
                addedMethods.Count,
                "New code");

            Console.WriteLine("{0,10} {1,10} {2,10} {3,8}   {4}",
                -removedMethodSize,
                removedMethodSize,
                0,
                removedMethods.Count,
                "Removed code");

            Console.WriteLine("{0,10} {1,10} {2,10} {3,8}   {4}",
                equivalentMethodRightSize - equivalentMethodLeftSize,
                equivalentMethodLeftSize,
                equivalentMethodRightSize,
                equivalentMethodCount,
                "Code in both");

            Console.WriteLine();

            Console.WriteLine("      Size   Name / Added method");
            Console.WriteLine(new string('-', 40));
            foreach (string method in addedMethods.OrderBy(m => m))
            {
                Console.WriteLine("{0,10}   {1}", rightMap[method], method);
            }

            Console.WriteLine();

            Console.WriteLine("      Size   Size / Added method");
            Console.WriteLine(new string('-', 40));
            foreach (string method in addedMethods.OrderBy(m => -rightMap[m]))
            {
                Console.WriteLine("{0,10}   {1}", rightMap[method], method);
            }

            Console.WriteLine();

            Console.WriteLine("      Size   Name / Removed method");
            Console.WriteLine(new string('-', 30));
            foreach (string method in removedMethods.OrderBy(m => m))
            {
                Console.WriteLine("{0,10}   {1}", leftMap[method], method);
            }

            Console.WriteLine();

            Console.WriteLine("      Size   Size / Removed method");
            Console.WriteLine(new string('-', 30));
            foreach (string method in removedMethods.OrderBy(m => -leftMap[m]))
            {
                Console.WriteLine("{0,10}   {1}", leftMap[method], method);
            }

            Console.WriteLine();

            Console.WriteLine("     Delta       Left      Right   Method changed size");
            Console.WriteLine(new string('-', 58));
            foreach (string method in methodsChangedSize.OrderBy(m => m))
            {
                int leftSize = leftMap[method];
                int rightSize = rightMap[method];
                Console.WriteLine("{0,10} {1,10} {2,10}   {3}", rightSize - leftSize, leftSize, rightSize, method); ;
            }

            Console.WriteLine();
        }
    }
}
