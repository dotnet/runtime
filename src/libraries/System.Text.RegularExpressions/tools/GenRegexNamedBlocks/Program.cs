// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

using static System.FormattableString;

namespace GenRegexNamedBlocks
{
    /// <summary>
    /// This program generates RegexCharClass.Tables.cs with Unicode named blocks
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: dotnet run -- <Blocks.txt> <output-file-path>");
                Console.WriteLine("Example: dotnet run -- Blocks.txt ../../src/System/Text/RegularExpressions/RegexCharClass.Tables.cs");
                return;
            }

            string blocksFile = args[0];
            string outputFile = args[1];

            // The input file should be Blocks.txt from the UCD corresponding to the
            // version of the Unicode spec we're consuming.
            // More info: https://www.unicode.org/reports/tr44/
            // Latest Blocks.txt: https://www.unicode.org/Public/UCD/latest/ucd/Blocks.txt

            string[] allInputLines = File.ReadAllLines(blocksFile);

            Regex inputLineRegex = new Regex(@"^(?<startCode>[0-9A-F]{4})\.\.(?<endCode>[0-9A-F]{4}); (?<blockName>.+)$");

            var entries = new List<(string name, string startCode, string endCode)>();

            foreach (string inputLine in allInputLines)
            {
                // We only care about lines of the form "XXXX..XXXX; Block name"
                var match = inputLineRegex.Match(inputLine);
                if (match == null || !match.Success)
                {
                    continue;
                }

                string startCode = match.Groups["startCode"].Value;
                string endCode = match.Groups["endCode"].Value;
                string blockName = match.Groups["blockName"].Value;

                // Exclude the surrogate range and everything outside the BMP.
                uint startCodeAsInt = uint.Parse(startCode, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (startCodeAsInt >= 0x10000 || (startCodeAsInt >= 0xD800 && startCodeAsInt <= 0xDFFF))
                {
                    continue;
                }

                // Exclude any private use areas
                if (blockName.Contains("Private Use", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                // Convert block name to Regex format (with "Is" prefix)
                string regexBlockName = "Is" + RemoveAllNonAlphanumeric(blockName);

                entries.Add((regexBlockName, startCode, endCode));
            }

            // Sort alphabetically for consistent output
            entries.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            // Add special backward-compatibility aliases
            entries.Add(("IsCombiningMarksforSymbols", "20D0", "20FF")); // Alias for IsCombiningDiacriticalMarksforSymbols
            entries.Add(("IsGreek", "0370", "03FF")); // Alias for IsGreekandCoptic
            entries.Add(("IsHighPrivateUseSurrogates", "DB80", "DBFF"));
            entries.Add(("IsHighSurrogates", "D800", "DB7F"));
            entries.Add(("IsLowSurrogates", "DC00", "DFFF"));
            entries.Add(("IsPrivateUse", "E000", "F8FF")); // Alias for IsPrivateUseArea
            entries.Add(("IsPrivateUseArea", "E000", "F8FF"));

            // Re-sort to include the new entries
            entries.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.Ordinal));

            // Generate the output file
            var output = new StringBuilder();
            output.AppendLine("// Licensed to the .NET Foundation under one or more agreements.");
            output.AppendLine("// The .NET Foundation licenses this file to you under the MIT license.");
            output.AppendLine();
            output.AppendLine("// This is a generated file. Do not edit directly.");
            output.AppendLine("// Run the GenRegexNamedBlocks tool to regenerate.");
            output.AppendLine();
            output.AppendLine("namespace System.Text.RegularExpressions");
            output.AppendLine("{");
            output.AppendLine("    internal sealed partial class RegexCharClass");
            output.AppendLine("    {");
            output.AppendLine("        /*");
            output.AppendLine("         *   The property table contains all the block definitions defined in the");
            output.AppendLine("         *   XML schema spec (http://www.w3.org/TR/2001/PR-xmlschema-2-20010316/#charcter-classes), Unicode 17.0 spec (www.unicode.org),");
            output.AppendLine("         *   and Perl 5.6 (see Programming Perl, 3rd edition page 167).   Three blocks defined by Perl (and here) may");
            output.AppendLine("         *   not be in the Unicode: IsHighPrivateUseSurrogates, IsHighSurrogates, and IsLowSurrogates.");
            output.AppendLine("         *");
            output.AppendLine("        **/");
            output.AppendLine("        // Has to be sorted by the first column");
            output.AppendLine("        private static readonly string[][] s_propTable =");
            output.AppendLine("        [");

            foreach (var entry in entries)
            {
                // Special handling for IsSpecials - it goes to the end of BMP
                if (entry.name == "IsSpecials")
                {
                    output.AppendLine($"            [\"{entry.name}\", \"\\u{entry.startCode}\"],");
                }
                else
                {
                    output.AppendLine($"            [\"{entry.name}\", \"\\u{entry.startCode}\\u{GetNextCodePoint(entry.endCode)}\"],");
                }
            }

            output.AppendLine("            [\"_xmlC\", /* Name Char              */    \"\\u002D\\u002F\\u0030\\u003B\\u0041\\u005B\\u005F\\u0060\\u0061\\u007B\\u00B7\\u00B8\\u00C0\\u00D7\\u00D8\\u00F7\\u00F8\\u0132\\u0134\\u013F\\u0141\\u0149\\u014A\\u017F\\u0180\\u01C4\\u01CD\\u01F1\\u01F4\\u01F6\\u01FA\\u0218\\u0250\\u02A9\\u02BB\\u02C2\\u02D0\\u02D2\\u0300\\u0346\\u0360\\u0362\\u0386\\u038B\\u038C\\u038D\\u038E\\u03A2\\u03A3\\u03CF\\u03D0\\u03D7\\u03DA\\u03DB\\u03DC\\u03DD\\u03DE\\u03DF\\u03E0\\u03E1\\u03E2\\u03F4\\u0401\\u040D\\u040E\\u0450\\u0451\\u045D\\u045E\\u0482\\u0483\\u0487\\u0490\\u04C5\\u04C7\\u04C9\\u04CB\\u04CD\\u04D0\\u04EC\\u04EE\\u04F6\\u04F8\\u04FA\\u0531\\u0557\\u0559\\u055A\\u0561\\u0587\\u0591\\u05A2\\u05A3\\u05BA\\u05BB\\u05BE\\u05BF\\u05C0\\u05C1\\u05C3\\u05C4\\u05C5\\u05D0\\u05EB\\u05F0\\u05F3\\u0621\\u063B\\u0640\\u0653\\u0660\\u066A\\u0670\\u06B8\\u06BA\\u06BF\\u06C0\\u06CF\\u06D0\\u06D4\\u06D5\\u06E9\\u06EA\\u06EE\\u06F0\\u06FA\\u0901\\u0904\\u0905\\u093A\\u093C\\u094E\\u0951\\u0955\\u0958\\u0964\\u0966\\u0970\\u0981\\u0984\\u0985\\u098D\\u098F\\u0991\\u0993\\u09A9\\u09AA\\u09B1\\u09B2\\u09B3\\u09B6\\u09BA\\u09BC\\u09BD\\u09BE\\u09C5\\u09C7\\u09C9\\u09CB\\u09CE\\u09D7\\u09D8\\u09DC\"");
            output.Append("                +\"\\u09DE\\u09DF\\u09E4\\u09E6\\u09F2\\u0A02\\u0A03\\u0A05\\u0A0B\\u0A0F\\u0A11\\u0A13\\u0A29\\u0A2A\\u0A31\\u0A32\\u0A34\\u0A35\\u0A37\\u0A38\\u0A3A\\u0A3C\\u0A3D\\u0A3E\\u0A43\\u0A47\\u0A49\\u0A4B\\u0A4E\\u0A59\\u0A5D\\u0A5E\\u0A5F\\u0A66\\u0A75\\u0A81\\u0A84\\u0A85\\u0A8C\\u0A8D\\u0A8E\\u0A8F\\u0A92\\u0A93\\u0AA9\\u0AAA\\u0AB1\\u0AB2\\u0AB4\\u0AB5\\u0ABA\\u0ABC\\u0AC6\\u0AC7\\u0ACA\\u0ACB\\u0ACE\\u0AE0\\u0AE1\\u0AE6\\u0AF0\\u0B01\\u0B04\\u0B05\\u0B0D\\u0B0F\\u0B11\\u0B13\\u0B29\\u0B2A\\u0B31\\u0B32\\u0B34\\u0B36\\u0B3A\\u0B3C\\u0B44\\u0B47\\u0B49\\u0B4B\\u0B4E\\u0B56\\u0B58\\u0B5C\\u0B5E\\u0B5F\\u0B62\\u0B66\\u0B70\\u0B82\\u0B84\\u0B85\\u0B8B\\u0B8E\\u0B91\\u0B92\\u0B96\\u0B99\\u0B9B\\u0B9C\\u0B9D\\u0B9E\\u0BA0\\u0BA3\\u0BA5\\u0BA8\\u0BAB\\u0BAE\\u0BB6\\u0BB7\\u0BBA\\u0BBE\\u0BC3\\u0BC6\\u0BC9\\u0BCA\\u0BCE\\u0BD7\\u0BD8\\u0BE7\\u0BF0\\u0C01\\u0C04\\u0C05\\u0C0D\\u0C0E\\u0C11\\u0C12\\u0C29\\u0C2A\\u0C34\\u0C35\\u0C3A\\u0C3E\\u0C45\\u0C46\\u0C49\\u0C4A\\u0C4E\\u0C55\\u0C57\\u0C60\\u0C62\"");
            output.Append("                +\"\\u0CE6\\u0CF0\\u0D02\\u0D04\\u0D05\\u0D0D\\u0D0E\\u0D11\\u0D12\\u0D29\\u0D2A\\u0D3A\\u0D3E\\u0D44\\u0D46\\u0D49\\u0D4A\\u0D4E\\u0D57\\u0D58\\u0D60\\u0D62\\u0D66\\u0D70\\u0E01\\u0E2F\\u0E30\\u0E3B\\u0E40\\u0E4F\\u0E50\\u0E5A\\u0E81\\u0E83\\u0E84\\u0E85\\u0E87\\u0E89\\u0E8A\\u0E8B\\u0E8D\\u0E8E\\u0E94\\u0E98\\u0E99\\u0EA0\\u0EA1\\u0EA4\\u0EA5\\u0EA6\\u0EA7\\u0EA8\\u0EAA\\u0EAC\\u0EAD\\u0EAF\\u0EB0\\u0EBA\\u0EBB\\u0EBE\\u0EC0\\u0EC5\\u0EC6\\u0EC7\\u0EC8\\u0ECE\\u0ED0\\u0EDA\\u0F18\\u0F1A\\u0F20\\u0F2A\\u0F35\\u0F36\\u0F37\\u0F38\\u0F39\\u0F3A\\u0F3E\\u0F48\\u0F49\\u0F6A\\u0F71\\u0F85\\u0F86\\u0F8C\\u0F90\\u0F96\\u0F97\\u0F98\\u0F99\\u0FAE\\u0FB1\\u0FB8\\u0FB9\\u0FBA\\u10A0\\u10C6\\u10D0\\u10F7\\u1100\\u1101\\u1102\\u1104\\u1105\\u1108\\u1109\\u110A\\u110B\\u110D\\u110E\\u1113\\u113C\\u113D\\u113E\\u113F\\u1140\\u1141\\u114C\\u114D\\u114E\\u114F\\u1150\\u1151\\u1154\\u1156\\u1159\\u115A\\u115F\\u1162\\u1163\\u1164\\u1165\\u1166\\u1167\\u1168\\u1169\\u116A\\u116D\\u116F\\u1172\\u1174\\u1175\\u1176\\u119E\\u119F\\u11A8\\u11A9\\u11AB\\u11AC\\u11AE\\u11B0\\u11B7\\u11B9\\u11BA\\u11BB\\u11BC\\u11C3\\u11EB\\u11EC\\u11F0\\u11F1\\u11F9\\u11FA\\u1E00\\u1E9C\\u1EA0\\u1EFA\\u1F00\"");
            output.Append("                +\"\\u1F16\\u1F18\\u1F1E\\u1F20\\u1F46\\u1F48\\u1F4E\\u1F50\\u1F58\\u1F59\\u1F5A\\u1F5B\\u1F5C\\u1F5D\\u1F5E\\u1F5F\\u1F7E\\u1F80\\u1FB5\\u1FB6\\u1FBD\\u1FBE\\u1FBF\\u1FC2\\u1FC5\\u1FC6\\u1FCD\\u1FD0\\u1FD4\\u1FD6\\u1FDC\\u1FE0\\u1FED\\u1FF2\\u1FF5\\u1FF6\\u1FFD\\u20D0\\u20DD\\u20E1\\u20E2\\u2126\\u2127\\u212A\\u212C\\u212E\\u212F\\u2180\\u2183\\u3005\\u3006\\u3007\\u3008\\u3021\\u3030\\u3031\\u3036\\u3041\\u3095\\u3099\\u309B\\u309D\\u309F\\u30A1\\u30FB\\u30FC\\u30FF\\u3105\\u312D\\u4E00\\u9FA6\\uAC00\\uD7A4\"],");
            output.AppendLine();
            output.AppendLine("            [\"_xmlD\",                                 \"\\u0030\\u003A\\u0660\\u066A\\u06F0\\u06FA\\u0966\\u0970\\u09E6\\u09F0\\u0A66\\u0A70\\u0AE6\\u0AF0\\u0B66\\u0B70\\u0BE7\\u0BF0\\u0C66\\u0C70\\u0CE6\\u0CF0\\u0D66\\u0D70\\u0E50\\u0E5A\\u0ED0\\u0EDA\\u0F20\\u0F2A\\u1040\\u104A\\u1369\\u1372\\u17E0\\u17EA\\u1810\\u181A\\uFF10\\uFF1A\"],");
            output.AppendLine("            [\"_xmlI\", /* Start Name Char       */     \"\\u003A\\u003B\\u0041\\u005B\\u005F\\u0060\\u0061\\u007B\\u00C0\\u00D7\\u00D8\\u00F7\\u00F8\\u0132\\u0134\\u013F\\u0141\\u0149\\u014A\\u017F\\u0180\\u01C4\\u01CD\\u01F1\\u01F4\\u01F6\\u01FA\\u0218\\u0250\\u02A9\\u02BB\\u02C2\\u0386\\u0387\\u0388\\u038B\\u038C\\u038D\\u038E\\u03A2\\u03A3\\u03CF\\u03D0\\u03D7\\u03DA\\u03DB\\u03DC\\u03DD\\u03DE\\u03DF\\u03E0\\u03E1\\u03E2\\u03F4\\u0401\\u040D\\u040E\\u0450\\u0451\\u045D\\u045E\\u0482\\u0490\\u04C5\\u04C7\\u04C9\\u04CB\\u04CD\\u04D0\\u04EC\\u04EE\\u04F6\\u04F8\\u04FA\\u0531\\u0557\\u0559\\u055A\\u0561\\u0587\\u05D0\\u05EB\\u05F0\\u05F3\\u0621\\u063B\\u0641\\u064B\\u0671\\u06B8\\u06BA\\u06BF\\u06C0\\u06CF\\u06D0\\u06D4\\u06D5\\u06D6\\u06E5\\u06E7\\u0905\\u093A\\u093D\\u093E\\u0958\\u0962\\u0985\\u098D\\u098F\\u0991\\u0993\\u09A9\\u09AA\\u09B1\\u09B2\\u09B3\\u09B6\\u09BA\\u09DC\\u09DE\\u09DF\\u09E2\\u09F0\\u09F2\\u0A05\\u0A0B\\u0A0F\\u0A11\\u0A13\\u0A29\\u0A2A\\u0A31\\u0A32\\u0A34\\u0A35\\u0A37\\u0A38\\u0A3A\\u0A59\\u0A5D\\u0A5E\\u0A5F\\u0A72\\u0A75\\u0A85\\u0A8C\\u0A8D\\u0A8E\\u0A8F\\u0A92\\u0A93\\u0AA9\\u0AAA\\u0AB1\\u0AB2\\u0AB4\\u0AB5\\u0ABA\\u0ABD\\u0ABE\\u0AE0\\u0AE1\\u0B05\\u0B0D\\u0B0F\"");
            output.Append("                +\"\\u0B11\\u0B13\\u0B29\\u0B2A\\u0B31\\u0B32\\u0B34\\u0B36\\u0B3A\\u0B3D\\u0B3E\\u0B5C\\u0B5E\\u0B5F\\u0B62\\u0B85\\u0B8B\\u0B8E\\u0B91\\u0B92\\u0B96\\u0B99\\u0B9B\\u0B9C\\u0B9D\\u0B9E\\u0BA0\\u0BA3\\u0BA5\\u0BA8\\u0BAB\\u0BAE\\u0BB6\\u0BB7\\u0BBA\\u0C05\\u0C0D\\u0C0E\\u0C11\\u0C12\\u0C29\\u0C2A\\u0C34\\u0C35\\u0C3A\\u0C60\\u0C62\\u0C85\\u0C8D\\u0C8E\\u0C91\\u0C92\\u0CA9\\u0CAA\\u0CB4\\u0CB5\\u0CBA\\u0CDE\\u0CDF\\u0CE0\\u0CE2\\u0D05\\u0D0D\\u0D0E\\u0D11\\u0D12\\u0D29\\u0D2A\\u0D3A\\u0D60\\u0D62\\u0E01\\u0E2F\\u0E30\\u0E31\\u0E32\\u0E34\\u0E40\\u0E46\\u0E81\\u0E83\\u0E84\\u0E85\\u0E87\\u0E89\\u0E8A\\u0E8B\\u0E8D\\u0E8E\\u0E94\\u0E98\\u0E99\\u0EA0\\u0EA1\\u0EA4\\u0EA5\\u0EA6\\u0EA7\\u0EA8\\u0EAA\\u0EAC\\u0EAD\\u0EAF\\u0EB0\\u0EB1\\u0EB2\\u0EB4\\u0EBD\\u0EBE\\u0EC0\\u0EC5\\u0F40\\u0F48\\u0F49\\u0F6A\\u10A0\\u10C6\\u10D0\\u10F7\\u1100\\u1101\\u1102\\u1104\\u1105\\u1108\\u1109\\u110A\\u110B\\u110D\\u110E\\u1113\\u113C\\u113D\\u113E\\u113F\\u1140\\u1141\\u114C\\u114D\\u114E\\u114F\\u1150\\u1151\\u1154\\u1156\\u1159\\u115A\\u115F\\u1162\\u1163\\u1164\\u1165\\u1166\\u1167\\u1168\\u1169\\u116A\\u116D\\u116F\\u1172\\u1174\\u1175\\u1176\\u119E\\u119F\\u11A8\\u11A9\\u11AB\\u11AC\"");
            output.Append("                +\"\\u11AE\\u11B0\\u11B7\\u11B9\\u11BA\\u11BB\\u11BC\\u11C3\\u11EB\\u11EC\\u11F0\\u11F1\\u11F9\\u11FA\\u1E00\\u1E9C\\u1EA0\\u1EFA\\u1F00\\u1F16\\u1F18\\u1F1E\\u1F20\\u1F46\\u1F48\\u1F4E\\u1F50\\u1F58\\u1F59\\u1F5A\\u1F5B\\u1F5C\\u1F5D\\u1F5E\\u1F5F\\u1F7E\\u1F80\\u1FB5\\u1FB6\\u1FBD\\u1FBE\\u1FBF\\u1FC2\\u1FC5\\u1FC6\\u1FCD\\u1FD0\\u1FD4\\u1FD6\\u1FDC\\u1FE0\\u1FED\\u1FF2\\u1FF5\\u1FF6\\u1FFD\\u2126\\u2127\\u212A\\u212C\\u212E\\u212F\\u2180\\u2183\\u3007\\u3008\\u3021\\u302A\\u3041\\u3095\\u30A1\\u30FB\\u3105\\u312D\\u4E00\\u9FA6\\uAC00\\uD7A4\"],");
            output.AppendLine();
            output.Append("            [\"_xmlW\",                                 \"\\u0024\\u0025\\u002B\\u002C\\u0030\\u003A\\u003C\\u003F\\u0041\\u005B\\u005E\\u005F\\u0060\\u007B\\u007C\\u007D\\u007E\\u007F\\u00A2\\u00AB\\u00AC\\u00AD\\u00AE\\u00B7\\u00B8\\u00BB\\u00BC\\u00BF\\u00C0\\u0221\\u0222\\u0234\\u0250\\u02AE\\u02B0\\u02EF\\u0300\\u0350\\u0360\\u0370\\u0374\\u0376\\u037A\\u037B\\u0384\\u0387\\u0388\\u038B\\u038C\\u038D\\u038E\\u03A2\\u03A3\\u03CF\\u03D0\\u03F7\\u0400\\u0487\\u0488\\u04CF\\u04D0\\u04F6\\u04F8\\u04FA\\u0500\\u0510\\u0531\\u0557\\u0559\\u055A\\u0561\\u0588\\u0591\\u05A2\\u05A3\\u05BA\\u05BB\\u05BE\\u05BF\\u05C0\\u05C1\\u05C3\\u05C4\\u05C5\\u05D0\\u05EB\\u05F0\\u05F3\\u0621\\u063B\\u0640\\u0656\\u0660\\u066A\\u066E\\u06D4\\u06D5\\u06DD\\u06DE\\u06EE\\u06F0\\u06FF\\u0710\\u072D\\u0730\\u074B\\u0780\\u07B2\\u0901\\u0904\\u0905\\u093A\\u093C\\u094E\\u0950\\u0955\\u0958\\u0964\\u0966\\u0970\\u0981\\u0984\\u0985\\u098D\\u098F\\u0991\\u0993\\u09A9\\u09AA\\u09B1\\u09B2\\u09B3\\u09B6\\u09BA\\u09BC\\u09BD\\u09BE\\u09C5\\u09C7\\u09C9\\u09CB\\u09CE\\u09D7\\u09D8\\u09DC\\u09DE\\u09DF\\u09E4\\u09E6\\u09FB\\u0A02\\u0A03\\u0A05\\u0A0B\\u0A0F\\u0A11\\u0A13\\u0A29\\u0A2A\\u0A31\\u0A32\\u0A34\\u0A35\"");
            output.Append("                +\"\\u0A37\\u0A38\\u0A3A\\u0A3C\\u0A3D\\u0A3E\\u0A43\\u0A47\\u0A49\\u0A4B\\u0A4E\\u0A59\\u0A5D\\u0A5E\\u0A5F\\u0A66\\u0A75\\u0A81\\u0A84\\u0A85\\u0A8C\\u0A8D\\u0A8E\\u0A8F\\u0A92\\u0A93\\u0AA9\\u0AAA\\u0AB1\\u0AB2\\u0AB4\\u0AB5\\u0ABA\\u0ABC\\u0AC6\\u0AC7\\u0ACA\\u0ACB\\u0ACE\\u0AD0\\u0AD1\\u0AE0\\u0AE1\\u0AE6\\u0AF0\\u0B01\\u0B04\\u0B05\\u0B0D\\u0B0F\\u0B11\\u0B13\\u0B29\\u0B2A\\u0B31\\u0B32\\u0B34\\u0B36\\u0B3A\\u0B3C\\u0B44\\u0B47\\u0B49\\u0B4B\\u0B4E\\u0B56\\u0B58\\u0B5C\\u0B5E\\u0B5F\\u0B62\\u0B66\\u0B71\\u0B82\\u0B84\\u0B85\\u0B8B\\u0B8E\\u0B91\\u0B92\\u0B96\\u0B99\\u0B9B\\u0B9C\\u0B9D\\u0B9E\\u0BA0\\u0BA3\\u0BA5\\u0BA8\\u0BAB\\u0BAE\\u0BB6\\u0BB7\\u0BBA\\u0BBE\\u0BC3\\u0BC6\\u0BC9\\u0BCA\\u0BCE\\u0BD7\\u0BD8\\u0BE7\\u0BF3\\u0C01\\u0C04\\u0C05\\u0C0D\\u0C0E\\u0C11\\u0C12\\u0C29\\u0C2A\\u0C34\\u0C35\\u0C3A\\u0C3E\\u0C45\\u0C46\\u0C49\\u0C4A\\u0C4E\\u0C55\\u0C57\\u0C60\\u0C62\\u0C66\\u0C70\\u0C82\\u0C84\\u0C85\\u0C8D\\u0C8E\\u0C91\\u0C92\\u0CA9\\u0CAA\\u0CB4\\u0CB5\\u0CBA\\u0CBE\\u0CC5\\u0CC6\\u0CC9\\u0CCA\\u0CCE\\u0CD5\\u0CD7\\u0CDE\\u0CDF\\u0CE0\\u0CE2\\u0CE6\\u0CF0\\u0D02\\u0D04\\u0D05\\u0D0D\\u0D0E\\u0D11\\u0D12\\u0D29\\u0D2A\\u0D3A\\u0D3E\\u0D44\\u0D46\\u0D49\"");
            output.Append("                +\"\\u0D4A\\u0D4E\\u0D57\\u0D58\\u0D60\\u0D62\\u0D66\\u0D70\\u0D82\\u0D84\\u0D85\\u0D97\\u0D9A\\u0DB2\\u0DB3\\u0DBC\\u0DBD\\u0DBE\\u0DC0\\u0DC7\\u0DCA\\u0DCB\\u0DCF\\u0DD5\\u0DD6\\u0DD7\\u0DD8\\u0DE0\\u0DF2\\u0DF4\\u0E01\\u0E3B\\u0E3F\\u0E4F\\u0E50\\u0E5A\\u0E81\\u0E83\\u0E84\\u0E85\\u0E87\\u0E89\\u0E8A\\u0E8B\\u0E8D\\u0E8E\\u0E94\\u0E98\\u0E99\\u0EA0\\u0EA1\\u0EA4\\u0EA5\\u0EA6\\u0EA7\\u0EA8\\u0EAA\\u0EAC\\u0EAD\\u0EBA\\u0EBB\\u0EBE\\u0EC0\\u0EC5\\u0EC6\\u0EC7\\u0EC8\\u0ECE\\u0ED0\\u0EDA\\u0EDC\\u0EDE\\u0F00\\u0F04\\u0F13\\u0F3A\\u0F3E\\u0F48\\u0F49\\u0F6B\\u0F71\\u0F85\\u0F86\\u0F8C\\u0F90\\u0F98\\u0F99\\u0FBD\\u0FBE\\u0FCD\\u0FCF\\u0FD0\\u1000\\u1022\\u1023\\u1028\\u1029\\u102B\\u102C\\u1033\\u1036\\u103A\\u1040\\u104A\\u1050\\u105A\\u10A0\\u10C6\\u10D0\\u10F9\\u1100\\u115A\\u115F\\u11A3\\u11A8\\u11FA\\u1200\\u1207\\u1208\\u1247\\u1248\\u1249\\u124A\\u124E\\u1250\\u1257\\u1258\\u1259\\u125A\\u125E\\u1260\\u1287\\u1288\\u1289\\u128A\\u128E\\u1290\\u12AF\\u12B0\\u12B1\\u12B2\\u12B6\\u12B8\\u12BF\\u12C0\\u12C1\\u12C2\\u12C6\\u12C8\\u12CF\\u12D0\\u12D7\\u12D8\\u12EF\\u12F0\\u130F\\u1310\\u1311\\u1312\\u1316\\u1318\\u131F\\u1320\\u1347\\u1348\\u135B\\u1369\\u137D\\u13A0\"");
            output.Append("                +\"\\u13F5\\u1401\\u166D\\u166F\\u1677\\u1681\\u169B\\u16A0\\u16EB\\u16EE\\u16F1\\u1700\\u170D\\u170E\\u1715\\u1720\\u1735\\u1740\\u1754\\u1760\\u176D\\u176E\\u1771\\u1772\\u1774\\u1780\\u17D4\\u17D7\\u17D8\\u17DB\\u17DD\\u17E0\\u17EA\\u180B\\u180E\\u1810\\u181A\\u1820\\u1878\\u1880\\u18AA\\u1E00\\u1E9C\\u1EA0\\u1EFA\\u1F00\\u1F16\\u1F18\\u1F1E\\u1F20\\u1F46\\u1F48\\u1F4E\\u1F50\\u1F58\\u1F59\\u1F5A\\u1F5B\\u1F5C\\u1F5D\\u1F5E\\u1F5F\\u1F7E\\u1F80\\u1FB5\\u1FB6\\u1FC5\\u1FC6\\u1FD4\\u1FD6\\u1FDC\\u1FDD\\u1FF0\\u1FF2\\u1FF5\\u1FF6\\u1FFF\\u2044\\u2045\\u2052\\u2053\\u2070\\u2072\\u2074\\u207D\\u207F\\u208D\\u20A0\\u20B2\\u20D0\\u20EB\\u2100\\u213B\\u213D\\u214C\\u2153\\u2184\\u2190\\u2329\\u232B\\u23B4\\u23B7\\u23CF\\u2400\\u2427\\u2440\\u244B\\u2460\\u24FF\\u2500\\u2614\\u2616\\u2618\\u2619\\u267E\\u2680\\u268A\\u2701\\u2705\\u2706\\u270A\\u270C\\u2728\\u2729\\u274C\\u274D\\u274E\\u274F\\u2753\\u2756\\u2757\\u2758\\u275F\\u2761\\u2768\\u2776\\u2795\\u2798\\u27B0\\u27B1\\u27BF\\u27D0\\u27E6\\u27F0\\u2983\\u2999\\u29D8\\u29DC\\u29FC\\u29FE\\u2B00\\u2E80\\u2E9A\\u2E9B\\u2EF4\\u2F00\\u2FD6\\u2FF0\\u2FFC\\u3004\\u3008\\u3012\\u3014\\u3020\\u3030\\u3031\\u303D\\u303E\\u3040\"");
            output.Append("                +\"\\u3041\\u3097\\u3099\\u30A0\\u30A1\\u30FB\\u30FC\\u3100\\u3105\\u312D\\u3131\\u318F\\u3190\\u31B8\\u31F0\\u321D\\u3220\\u3244\\u3251\\u327C\\u327F\\u32CC\\u32D0\\u32FF\\u3300\\u3377\\u337B\\u33DE\\u33E0\\u33FF\\u3400\\u4DB6\\u4E00\\u9FA6\\uA000\\uA48D\\uA490\\uA4C7\\uAC00\\uD7A4\\uF900\\uFA2E\\uFA30\\uFA6B\\uFB00\\uFB07\\uFB13\\uFB18\\uFB1D\\uFB37\\uFB38\\uFB3D\\uFB3E\\uFB3F\\uFB40\\uFB42\\uFB43\\uFB45\\uFB46\\uFBB2\\uFBD3\\uFD3E\\uFD50\\uFD90\\uFD92\\uFDC8\\uFDF0\\uFDFD\\uFE00\\uFE10\\uFE20\\uFE24\\uFE62\\uFE63\\uFE64\\uFE67\\uFE69\\uFE6A\\uFE70\\uFE75\\uFE76\\uFEFD\\uFF04\\uFF05\\uFF0B\\uFF0C\\uFF10\\uFF1A\\uFF1C\\uFF1F\\uFF21\\uFF3B\\uFF3E\\uFF3F\\uFF40\\uFF5B\\uFF5C\\uFF5D\\uFF5E\\uFF5F\\uFF66\\uFFBF\\uFFC2\\uFFC8\\uFFCA\\uFFD0\\uFFD2\\uFFD8\\uFFDA\\uFFDD\\uFFE0\\uFFE7\\uFFE8\\uFFEF\\uFFFC\\uFFFE\"],");
            output.AppendLine();
            output.AppendLine("        ];");
            output.AppendLine("    }");
            output.AppendLine("}");

            File.WriteAllText(outputFile, output.ToString());
            Console.WriteLine($"Successfully generated {outputFile}");
        }

        private static string RemoveAllNonAlphanumeric(string blockName)
        {
            // Allow only A-Z a-z 0-9 and hyphens
            // Keep hyphens to preserve naming like "Latin-1" or "Extended-A"
            return new string(blockName.ToCharArray().Where(c => 
                ('A' <= c && c <= 'Z') || 
                ('a' <= c && c <= 'z') || 
                ('0' <= c && c <= '9') ||
                c == '-').ToArray());
        }

        private static string GetNextCodePoint(string hexCode)
        {
            // Regex named blocks use the start of the next block as the end code
            // So we need to add 1 to the end code
            uint code = uint.Parse(hexCode, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            code++;
            return code.ToString("X4");
        }
    }
}
