// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace EncodingDataGenerator
{
    class EncodingDataGenerator
    {
        private const string CommentIndicator = "#";
        private const char FieldDelimiter = ';';

        private static void Main(string[] args)
        {
            try
            {
                EncodingDataGenerator edg = new EncodingDataGenerator();
                if (!edg.ParseParams(args) || edg.IanaMappings == null || edg.PreferredIANANames == null || edg.OutputDataTable == null)
                {
                    edg.Usage();
                    return;
                }

                edg.Generate();
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }

        private bool ParseParams(string[] args)
        {
            int paramIndex = 0;

            while (paramIndex < args.Length - 1)
            {
                if (args[paramIndex] == "--IanaMapping" || args[paramIndex] == "-i")
                {
                    IanaMappings = args[paramIndex + 1];
                    paramIndex += 2;
                    continue;
                }

                if (args[paramIndex] == "--PreferredIANANames" || args[paramIndex] == "-p")
                {
                    PreferredIANANames = args[paramIndex + 1];
                    paramIndex += 2;
                    continue;
                }

                if (args[paramIndex] == "--Output" || args[paramIndex] == "-o")
                {
                    OutputDataTable = args[paramIndex + 1];
                    paramIndex += 2;
                    continue;
                }

                if (args[paramIndex] == "--Namespace" || args[paramIndex] == "-n")
                {
                    Namespace = args[paramIndex + 1];
                    paramIndex += 2;
                    continue;
                }

                if (args[paramIndex] == "--ClassName" || args[paramIndex] == "-c")
                {
                    ClassName = args[paramIndex + 1];
                    paramIndex += 2;
                    continue;
                }

                break;
            }

            return paramIndex >= args.Length;
        }

        private void Usage()
        {
            Console.WriteLine("EncodingDataGenerator [options] [Option argument]");

            Console.WriteLine("Options [Option argument] ");
            Console.WriteLine("    --IanaMapping, -i         [IANA Mapping File Path]         specify the file containing IANA encoding names mapping. This is required parameter");
            Console.WriteLine("    --PreferredIANANames, -p  [IANA Preferred Names File Path] specify the file containing IANA encoding names mapping. This is required parameter");
            Console.WriteLine("    --Output, -o              [Output File Path]               specify the output CS file. This is required parameter");
            Console.WriteLine("    --Namespace, -n           [The namespace]                  specify namespace to use for the generated code. This is optional parameter, if not specified, `System.Text` will be used");
            Console.WriteLine("    --ClassName, -c           [The Class name]                 specify class name to use for the generated code. This is optional parameter, if not specified, `EncodingTable` will be used");

            Console.WriteLine("Example:");
            Console.WriteLine("EncodingDataGenerator.exe  --IanaMapping CodePageNameMappings.csv --PreferredIANANames PreferredCodePageNames.csv --Output EncodingTable.Data.cs");
        }

        private string IanaMappings { get; set; }
        private string PreferredIANANames { get; set; }

        private string OutputDataTable { get; set; }

        private string Namespace { get; set; }
        private string ClassName { get; set; }

        private bool Generate()
        {
            Dictionary<string, ushort> nameMappings = ParseNameMappings(IanaMappings);
            Dictionary<ushort, KeyValuePair<string, string>> preferredNames = ParsePreferredNames(PreferredIANANames);

            if (!ValidateMappings(nameMappings, preferredNames))
            {
                return false;
            }

            using (StreamWriter output = File.CreateText(OutputDataTable))
            {
                output.Write(Header, IanaMappings, PreferredIANANames, Namespace ?? "System.Text", ClassName ?? "EncodingTable");

                OutputData(output, EncodingNames, nameMappings.OrderBy(kv => kv.Key, StringComparer.Ordinal), kv => new object[] { kv.Key, kv.Value });
                {
                    int nextStart = 0;
                    OutputData(output, EncodingNameIndices, nameMappings.OrderBy(kv => kv.Key, StringComparer.Ordinal), kv => new object[] { kv.Key, kv.Value, nextStart += kv.Key.Length });
                }

                OutputData(output, CodePagesByName, nameMappings.OrderBy(kv => kv.Key, StringComparer.Ordinal), kv => new object[] { kv.Value, kv.Key });
                OutputData(output, MappedCodePages, preferredNames.OrderBy(kv => kv.Key), kv => new object[] { kv.Key, kv.Value.Key });

                OutputData(output, WebNames, preferredNames.OrderBy(kv => kv.Key), kv => new object[] { kv.Value.Key, kv.Key });
                {
                    int nextStart = 0;
                    OutputData(output, WebNameIndices, preferredNames.OrderBy(kv => kv.Key), kv => new object[] { kv.Value.Key, kv.Key, nextStart += kv.Value.Key.Length });
                }

                OutputData(output, EnglishNames, preferredNames.OrderBy(kv => kv.Key), kv => new object[] { kv.Value.Value, kv.Key });
                {
                    int nextStart = 0;
                    OutputData(output, EnglishNameIndices, preferredNames.OrderBy(kv => kv.Key), kv => new object[] { kv.Value.Value, kv.Key, nextStart += kv.Value.Value.Length });
                }

                output.Write(Footer);
            }
            return true;
        }

        // Takes and formats data to the format inside the source.
        private void OutputData<TKey, TValue>(StreamWriter output, string source, IEnumerable<KeyValuePair<TKey, TValue>> data, Func<KeyValuePair<TKey, TValue>, object[]> translator)
        {
            string[] sourceData = source.Split('|');
            string format = sourceData[1];

            output.Write(sourceData[0]);

            foreach (object[] parameters in data.Select(translator))
            {
                output.Write(format, parameters);
            }

            output.Write(sourceData[2]);
        }

        private bool ValidateMappings(Dictionary<string, ushort> nameMappings, Dictionary<ushort, KeyValuePair<string, string>> preferredNames)
        {
            bool ret = true;

            // There are multiple mapped names, and each must have a matching preferred name/English name.
            foreach (ushort codepage in nameMappings.Values.Except(preferredNames.Keys))
            {
                Console.WriteLine("Code page {0} is mapped to name(s), but has no preferred entry/English name", codepage);
                ret = false;
            }

            // Each preferred name must have a matching mapped name.
            foreach (string name in preferredNames.Values.Select(kv => kv.Key).Except(nameMappings.Keys))
            {
                Console.WriteLine("Preferred name {0} exists, but isn't mapped to a codepage", name);
                ret = false;
            }

            return ret;
        }

        private Dictionary<string, ushort> ParseNameMappings(string path)
        {
            Dictionary<string, ushort> mapping = new Dictionary<string, ushort>();

            foreach (var line in DelimitedFileRows(path, 2))
            {
                string name = line.Value[0].Trim().ToLowerInvariant();

                if (name != line.Value[0])
                {
                    Console.WriteLine("Code page name in file {0} at line {1} has whitespace or upper-case characters.  Was: ->{2}<-, Using ->{3}<-", path, line.Key, line.Value[0], name);
                }

                ushort codepage;
                if (!ushort.TryParse(line.Value[1], out codepage))
                {
                    Console.WriteLine("Code page in file {0} at line {1} is not valid, expecting numeric entry in range [" + ushort.MinValue + ", " + ushort.MaxValue + "]  Was: ->{2}<-", path, line.Key, line.Value[1]);
                    continue;
                }

                ushort existing;
                if (mapping.TryGetValue(name, out existing))
                {
                    if (existing == codepage)
                    {
                        Console.WriteLine("Code page mapping {0} to {1} in file {2} at line {3} is a duplicate entry, and can be removed.", name, codepage, path, line.Key);
                    }
                    else
                    {
                        Console.WriteLine("Code page name {0} in file {1} at line {2} is mapped to multiple code pages; new is {3}, old was {4}", name, path, line.Key, name, codepage, existing);
                    }
                }
                else
                {
                    mapping[name] = codepage;
                }
            }

            return mapping;
        }

        private Dictionary<ushort, KeyValuePair<string, string>> ParsePreferredNames(string path)
        {
            Dictionary<ushort, KeyValuePair<string, string>> preferredNames = new Dictionary<ushort, KeyValuePair<string, string>>();

            foreach (var line in DelimitedFileRows(path, 3))
            {
                ushort codepage;
                if (!ushort.TryParse(line.Value[0], out codepage))
                {
                    Console.WriteLine("Code page in file {0} at line {1} is not valid, expecting numeric entry in range [" + ushort.MinValue + ", " + ushort.MaxValue + "]  Was: ->{2}<-", path, line.Key, line.Value[0]);
                    continue;
                }

                string name = line.Value[1].Trim().ToLowerInvariant();
                if (name != line.Value[1])
                {
                    Console.WriteLine("Code page name in file {0} at line {1} has whitespace or upper-case characters.  Was: ->{2}<-, Using ->{3}<-", path, line.Key, line.Value[1], name);
                }

                string englishName = line.Value[2].Trim();
                if (englishName != line.Value[2])
                {
                    Console.WriteLine("English name in file {0} at line {1} has whitespace.  Was: ->{2}<-, Using ->{3}<-", path, line.Key, line.Value[2], englishName);
                }

                KeyValuePair<string, string> names = KeyValuePair.Create(name, englishName);

                KeyValuePair<string, string> existing;
                if (preferredNames.TryGetValue(codepage, out existing))
                {
                    if (names.Equals(existing))
                    {
                        Console.WriteLine("Code page names {0} for code page {1} in file {2} at line {3} is a duplicate entry, and can be removed.", names, codepage, path, line.Key);
                    }
                    else
                    {
                        Console.WriteLine("Code page {0} in file {1} at line {2} is mapped to multiple names; new is {3}, old was {4}", codepage, path, line.Key, names, existing);
                    }
                }
                else
                {
                    preferredNames[codepage] = names;
                }
            }

            return preferredNames;
        }

        private IEnumerable<KeyValuePair<int, string[]>> DelimitedFileRows(string path, int columns = 0)
        {
            using (var input = File.OpenText(path))
            {
                int lineNumber = 1;
                string line;

                for (; (line = input.ReadLine()) != null; ++lineNumber)
                {
                    if (line.StartsWith(CommentIndicator) || string.IsNullOrWhiteSpace(line))
                    {
                        continue;
                    }

                    string[] values = line.Split(FieldDelimiter);

                    if (columns > 0 && values.Length != columns)
                    {
                        Console.WriteLine("Parsing mapping in file {0}, line {1}.  Expected {2} fields, saw {3}: {4}", path, lineNumber, columns, values.Length, line);
                    }

                    yield return KeyValuePair.Create(lineNumber, line.Split(FieldDelimiter));
                }
            }
        }

        private const string Header =

@"// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// THIS IS AN AUTOGENERATED FILE
// IT IS GENERATED FROM EncodingDataGenerator Tool.
//
//     EncodingDataGenerator.exe  --IanaMapping ..\..\Data\CodePageNameMappings.csv --PreferredIANANames ..\..\Data\PreferredCodePageNames.csv --Output EncodingTable.Data.cs
//

namespace {2}
{{
    internal static partial class {3}
    {{
";

        // The format is:
        //     0 - IANA name
        //     1 - codepage
        // Ordered by alphabetized IANA name
        private const string EncodingNames =
@"
        // EncodingNames is the concatenation of all supported IANA names for each codepage.
        // This is done rather than using a large readonly array of strings to avoid
        // generating a large amount of code in the static constructor.
        // Using indices from EncodingNamesIndices, we binary search this string when mapping
        // an encoding name to a codepage. Note that these names are all lowercase and are
        // sorted alphabetically.
        private const string EncodingNames =|
            ""{0}"" + // {1:D}|
            """";
";

        // The format is:
        //     0 - IANA name
        //     1 - codepage
        //     2 - Start index of encoding name
        // The layout is to properly populate the end value
        // Ordered by alphabetized IANA name
        private const string EncodingNameIndices =
@"
        // EncodingNameIndices contains the start index of every encoding name in the string
        // EncodingNames. We infer the length of each string by looking at the start index
        // of the next string.
        private static ReadOnlySpan<int> EncodingNameIndices =>
        [
            0|, // {0} ({1:D})
            {2:D}|
        ];
";

        // The format is:
        //     0 - codepage
        //     1 - IANA name
        // Ordered by alphabetized IANA name
        private const string CodePagesByName =
@"
        // CodePagesByName contains the list of supported codepages which match the encoding
        // names listed in EncodingNames. The way mapping works is we binary search
        // EncodingNames using EncodingNamesIndices until we find a match for a given name.
        // The index of the entry in EncodingNamesIndices will be the index of codepage in CodePagesByName.
        private static ReadOnlySpan<ushort> CodePagesByName =>
        [|
            {0:D}, // {1}|
        ];
";

        // The format is:
        //     0 - codepage
        //     1 - IANA name
        // Ordered by codepage
        private const string MappedCodePages =
@"
        // When retrieving the value for System.Text.Encoding.WebName or
        // System.Text.Encoding.EncodingName given System.Text.Encoding.CodePage,
        // we perform a linear search on s_mappedCodePages to find the index of the
        // given codepage. This is used to index WebNameIndices to get the start
        // index of the web name in the string WebNames, and to index
        // EnglishNameIndices to get the start of the English name in EnglishNames.
        private static ReadOnlySpan<ushort> s_mappedCodePages =>
        [|
            {0:D}, // {1}|
        ];
";

        // The format is:
        //     0 - IANA name
        //     1 - codepage
        // Ordered by codepage
        private const string WebNames =
@"
        // WebNames is a concatenation of the default encoding names
        // for each code page. It is used in retrieving the value for
        // System.Text.Encoding.WebName given System.Text.Encoding.CodePage.
        // This is done rather than using a large readonly array of strings to avoid
        // generating a large amount of code in the static constructor.
        private const string WebNames =|
            ""{0}"" + // {1:D}|
            """";
";

        // The format is:
        //     0 - IANA name
        //     1 - codepage
        //     2 - Start index of (default) web name
        // The layout is to properly populate the end value
        // Ordered by codepage
        private const string WebNameIndices =
@"
        // WebNameIndices contains the start index of each code page's default
        // web name in the string WebNames. It is indexed by an index into
        // s_mappedCodePages.
        private static ReadOnlySpan<int> WebNameIndices =>
        [
            0|, // {0} ({1:D})
            {2:D}|
        ];
";

        // The format is:
        //     0 - English name
        //     1 - codepage
        // Ordered by codepage
        private const string EnglishNames =
@"
        // EnglishNames is the concatenation of the English names for each codepage.
        // It is used in retrieving the value for System.Text.Encoding.EncodingName
        // given System.Text.Encoding.CodePage.
        // This is done rather than using a large readonly array of strings to avoid
        // generating a large amount of code in the static constructor.
        private const string EnglishNames =|
            ""{0}"" + // {1:D}|
            """";
";

        // The format is:
        //     0 - English name
        //     1 - codepage
        //     2 - Start index of English name
        // The layout is to properly populate the end value
        // Ordered by codepage
        private const string EnglishNameIndices =
@"
        // EnglishNameIndices contains the start index of each code page's English
        // name in the string EnglishNames. It is indexed by an index into s_mappedCodePages.
        private static ReadOnlySpan<int> EnglishNameIndices =>
        [
            0|, // {0} ({1:D})
            {2:D}|
        ];
";

        private const string Footer =
@"
    }
}
";

        private static class KeyValuePair
        {
            public static KeyValuePair<TKey, TValue> Create<TKey, TValue>(TKey key, TValue value)
            {
                return new KeyValuePair<TKey, TValue>(key, value);
            }
        }
    }
}
