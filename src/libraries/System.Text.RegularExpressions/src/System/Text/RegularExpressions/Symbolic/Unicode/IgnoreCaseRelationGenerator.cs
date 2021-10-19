// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;

namespace System.Text.RegularExpressions.Symbolic.Unicode
{
#if DEBUG
    internal static class IgnoreCaseRelationGenerator
    {
        private const string DefaultCultureName = "en-US";

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
            WriteIgnoreCaseBDD(sw);
            sw.WriteLine($@"    }}
}}");
        }

        private static void WriteIgnoreCaseBDD(StreamWriter sw)
        {
            sw.WriteLine("        /// <summary>Serialized BDD for mapping characters to their case-ignoring equivalence classes in the default (en-US) culture.</summary>");

            var solver = new CharSetSolver();
            Dictionary<char, BDD> ignoreCase = ComputeIgnoreCaseDictionary(solver, new CultureInfo(DefaultCultureName));
            BDD ignorecase = solver.False;
            foreach (KeyValuePair<char, BDD> kv in ignoreCase)
            {
                BDD a = solver.CreateCharSetFromRange(kv.Key, kv.Key);
                BDD b = kv.Value;
                ignorecase = solver.Or(ignorecase, solver.And(solver.ShiftLeft(a, 16), b));
            }

            sw.Write("        public static readonly long[] IgnoreCaseEnUsSerializedBDD = ");
            GeneratorHelper.WriteInt64ArrayInitSyntax(sw, ignorecase.Serialize());
            sw.WriteLine(";");
        }

        private static Dictionary<char, BDD> ComputeIgnoreCaseDictionary(CharSetSolver solver, CultureInfo culture)
        {
            CultureInfo originalCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = culture;

                var ignoreCase = new Dictionary<char, BDD>();

                for (uint i = 0; i <= 0xFFFF; i++)
                {
                    char c = (char)i;
                    char cUpper = char.ToUpper(c);
                    char cLower = char.ToLower(c);

                    if (cUpper == cLower)
                    {
                        continue;
                    }

                    // c may be different from both cUpper as well as cLower.
                    // Make sure that the regex engine considers c as being equivalent to cUpper and cLower, else ignore c.
                    // In some cases c != cU but the regex engine does not consider the chacarters equivalent wrt the ignore-case option.
                    if (Regex.IsMatch($"{cUpper}{cLower}", $"^(?i:\\u{i:X4}\\u{i:X4})$"))
                    {
                        BDD equiv = solver.False;

                        if (ignoreCase.ContainsKey(c))
                            equiv = solver.Or(equiv, ignoreCase[c]);

                        if (ignoreCase.ContainsKey(cUpper))
                            equiv = solver.Or(equiv, ignoreCase[cUpper]);

                        if (ignoreCase.ContainsKey(cLower))
                            equiv = solver.Or(equiv, ignoreCase[cLower]);

                        // Make sure all characters are included initially or when some is still missing
                        equiv = solver.Or(equiv, solver.Or(solver.CreateCharSetFromRange(c, c), solver.Or(solver.CreateCharSetFromRange(cUpper, cUpper), solver.CreateCharSetFromRange(cLower, cLower))));

                        // Update all the members with their case-invariance equivalence classes
                        foreach (char d in solver.GenerateAllCharacters(equiv))
                            ignoreCase[d] = equiv;
                    }
                }

                return ignoreCase;
            }
            finally
            {
                CultureInfo.CurrentCulture = originalCulture;
            }
        }
    };
#endif
}
