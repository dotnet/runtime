// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Xunit;

namespace System.Text.Unicode
{
    internal sealed class ParsedUnicodeData
    {
        // Mappings from https://www.unicode.org/Public/UCD/latest/ucd/PropertyValueAliases.txt (bc).
        private static readonly Dictionary<string, BidiClass> BidiClassMap = new Dictionary<string, BidiClass>()
        {
            ["AL"] = BidiClass.Arabic_Letter,
            ["AN"] = BidiClass.Arabic_Number,
            ["B"] = BidiClass.Paragraph_Separator,
            ["BN"] = BidiClass.Boundary_Neutral,
            ["CS"] = BidiClass.Common_Separator,
            ["EN"] = BidiClass.European_Number,
            ["ES"] = BidiClass.European_Separator,
            ["ET"] = BidiClass.European_Terminator,
            ["FSI"] = BidiClass.First_Strong_Isolate,
            ["L"] = BidiClass.Left_To_Right,
            ["LRE"] = BidiClass.Left_To_Right_Embedding,
            ["LRI"] = BidiClass.Left_To_Right_Isolate,
            ["LRO"] = BidiClass.Left_To_Right_Override,
            ["NSM"] = BidiClass.Nonspacing_Mark,
            ["ON"] = BidiClass.Other_Neutral,
            ["PDF"] = BidiClass.Pop_Directional_Format,
            ["PDI"] = BidiClass.Pop_Directional_Isolate,
            ["R"] = BidiClass.Right_To_Left,
            ["RLE"] = BidiClass.Right_To_Left_Embedding,
            ["RLI"] = BidiClass.Right_To_Left_Isolate,
            ["RLO"] = BidiClass.Right_To_Left_Override,
            ["S"] = BidiClass.Segment_Separator,
            ["WS"] = BidiClass.White_Space,
        };

        internal readonly Dictionary<int, int> CaseFoldingData;
        internal readonly Dictionary<int, BidiClass> DerivedBidiClassData;
        internal readonly Dictionary<int, string> DerivedNameData;
        internal readonly Dictionary<int, GraphemeClusterBreakProperty> GraphemeBreakPropertyData;
        internal readonly Dictionary<int, CodePointFlags> PropListData;
        internal readonly Dictionary<int, UnicodeDataFileEntry> UnicodeDataData;

        public ParsedUnicodeData()
        {
            CaseFoldingData = ProcessCaseFoldingFile();
            DerivedBidiClassData = ProcessDerivedBidiClassFile();
            DerivedNameData = ProcessDerivedNameFile();
            GraphemeBreakPropertyData = ProcessGraphemeClusterBreakAndEmojiDataFiles();
            PropListData = ProcessPropListFile();
            UnicodeDataData = ProcessUnicodeDataFile();
        }

        /// <summary>
        /// Reads CaseFolding.txt and parses each entry in that file.
        /// </summary>
        private static Dictionary<int, int> ProcessCaseFoldingFile()
        {
            using Stream stream = Resources.OpenResource(Resources.CaseFolding);
            using StreamReader reader = new StreamReader(stream);

            Dictionary<int, int> dict = new Dictionary<int, int>();

            string thisLine;
            while ((thisLine = reader.ReadLine()) != null)
            {
                // Ignore blank lines or comment lines

                if (string.IsNullOrEmpty(thisLine) || thisLine[0] == '#') { continue; }

                // Line should be in format "<code>; <status>; <mapping>; # <name>"

                string[] split = thisLine.Split(';');
                Assert.Equal(4, split.Length);

                // We only support common and simple case folding; ignore everything else.

                char status = split[1].AsSpan().Trim()[0];
                if (status != 'C' && status != 'S')
                {
                    continue;
                }

                int fromCodePoint = (int)uint.Parse(split[0], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                int toCodePoint = (int)uint.Parse(split[2], NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                dict.Add(fromCodePoint, toCodePoint);
            }

            return dict;
        }

        /// <summary>
        /// Reads DerivedBidiClass.txt and parses each entry in that file.
        /// </summary>
        private static Dictionary<int, BidiClass> ProcessDerivedBidiClassFile()
        {
            using Stream stream = Resources.OpenResource(Resources.DerivedBidiClass);
            using StreamReader reader = new StreamReader(stream);

            Dictionary<int, BidiClass> dict = new Dictionary<int, BidiClass>();

            string thisLine;
            while ((thisLine = reader.ReadLine()) != null)
            {
                if (PropsFileEntry.TryParseLine(thisLine, out PropsFileEntry value))
                {
                    BidiClass bidiClass = BidiClassMap[value.PropName];

                    for (int i = value.FirstCodePoint; i <= value.LastCodePoint /* inclusive */; i++)
                    {
                        dict.Add(i, bidiClass);
                    }
                }
            }

            return dict;
        }

        /// <summary>
        /// Reads DerivedName.txt and parses each entry in that file.
        /// </summary>
        private static Dictionary<int, string> ProcessDerivedNameFile()
        {
            using Stream stream = Resources.OpenResource(Resources.DerivedName);
            using StreamReader reader = new StreamReader(stream);

            Dictionary<int, string> dict = new Dictionary<int, string>();

            string thisLine;
            while ((thisLine = reader.ReadLine()) != null)
            {
                if (PropsFileEntry.TryParseLine(thisLine, out PropsFileEntry value))
                {
                    if (value.IsSingleCodePoint)
                    {
                        // Single code point of format "XXXX ; <Name>" (name shouldn't end with '*')

                        Assert.False(value.PropName.EndsWith('*'));
                        dict.Add(value.FirstCodePoint, value.PropName);
                    }
                    else
                    {
                        // Range of format "XXXX..YYYY ; <Name>-*"

                        Assert.True(value.PropName.EndsWith('*'));

                        string baseName = value.PropName[..^1];
                        for (int i = value.FirstCodePoint; i <= value.LastCodePoint /* inclusive */; i++)
                        {
                            dict.Add(i, $"{baseName}{i:X4}");
                        }
                    }
                }
            }

            return dict;
        }

        /// <summary>
        /// Reads GraphemeBreakProperty.txt and emoji-data.txt and parses each entry in those files.
        /// </summary>
        private static Dictionary<int, GraphemeClusterBreakProperty> ProcessGraphemeClusterBreakAndEmojiDataFiles()
        {
            Dictionary<int, GraphemeClusterBreakProperty> dict = new Dictionary<int, GraphemeClusterBreakProperty>();

            foreach (string resourceName in new[] { Resources.GraphemeBreakProperty, Resources.EmojiData })
            {
                using Stream stream = Resources.OpenResource(resourceName);
                using StreamReader reader = new StreamReader(stream);

                string thisLine;
                while ((thisLine = reader.ReadLine()) != null)
                {
                    if (PropsFileEntry.TryParseLine(thisLine, out PropsFileEntry value))
                    {
                        if (Enum.TryParse<GraphemeClusterBreakProperty>(value.PropName, out GraphemeClusterBreakProperty property))
                        {
                            for (int i = value.FirstCodePoint; i <= value.LastCodePoint /* inclusive */; i++)
                            {
                                dict.Add(i, property);
                            }
                        }
                    }
                }
            }

            return dict;
        }

        /// <summary>
        /// Reads PropList.txt and parses each entry in that file.
        /// </summary>
        private static Dictionary<int, CodePointFlags> ProcessPropListFile()
        {
            using Stream stream = Resources.OpenResource(Resources.PropList);
            using StreamReader reader = new StreamReader(stream);

            Dictionary<int, CodePointFlags> dict = new Dictionary<int, CodePointFlags>();

            string thisLine;
            while ((thisLine = reader.ReadLine()) != null)
            {
                // Expect "XXXX[..YYYY] ; <prop_name> # <comment>"

                if (PropsFileEntry.TryParseLine(thisLine, out PropsFileEntry value))
                {
                    CodePointFlags newFlag = Enum.Parse<CodePointFlags>(value.PropName);
                    for (int i = value.FirstCodePoint; i <= value.LastCodePoint /* inclusive */; i++)
                    {
                        dict.TryGetValue(i, out CodePointFlags flagsForThisCodePoint /* could be default(T) */);
                        flagsForThisCodePoint |= newFlag;
                        dict[i] = flagsForThisCodePoint;
                    }
                }
            }

            return dict;
        }

        /// <summary>
        /// Reads UnicodeData.txt and parses each entry in that file.
        /// </summary>
        private static Dictionary<int, UnicodeDataFileEntry> ProcessUnicodeDataFile()
        {
            using Stream stream = Resources.OpenResource(Resources.UnicodeData);
            using StreamReader reader = new StreamReader(stream);

            Dictionary<int, UnicodeDataFileEntry> dict = new Dictionary<int, UnicodeDataFileEntry>();

            string thisLine;
            while ((thisLine = reader.ReadLine()) != null)
            {
                // Skip blank lines at beginning or end of the file

                if (thisLine.Length == 0) { continue; }

                UnicodeDataFileEntry entry = new UnicodeDataFileEntry(thisLine);

                if (entry.Name.EndsWith(", First>", StringComparison.Ordinal))
                {
                    // This is an entry of the form XXXX;<Name, First>;...
                    // We expect it to be followed by YYYY;<Name, Last>;...

                    UnicodeDataFileEntry nextEntry = new UnicodeDataFileEntry(reader.ReadLine());
                    Assert.EndsWith(", Last>", nextEntry.Name, StringComparison.Ordinal);
                    Assert.Equal(entry.Name[..^", First>".Length], nextEntry.Name[..^", Last>".Length]);

                    string baseName = entry.Name.Remove(entry.Name.Length - ", First>".Length, ", First".Length); // remove the ", First" part of the name
                    for (int i = entry.CodePoint; i <= nextEntry.CodePoint /* inclusive */; i++)
                    {
                        dict.Add(i, new UnicodeDataFileEntry(i, baseName, entry.GeneralCategory));
                    }
                }
                else
                {
                    // This is a single code point entry, not a range.

                    dict.Add(entry.CodePoint, entry);
                }
            }

            return dict;
        }
    }
}
