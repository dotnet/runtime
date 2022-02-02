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
    [ExcludeFromCodeCoverage]
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
            List<EquivalenceClass> ignoreCaseEquivalenceClasses = ComputeIgnoreCaseEquivalenceClasses(solver, new CultureInfo(DefaultCultureName));
            BDD ignorecase = solver.False;
            foreach (EquivalenceClass ec in ignoreCaseEquivalenceClasses)
            {
                // Create the Cartesian product of ec._set with itself
                BDD crossproduct = solver.And(solver.ShiftLeft(ec._set, 16), ec._set);
                // Add the product into the overall lookup table
                ignorecase = solver.Or(ignorecase, crossproduct);
            }

            sw.Write("        public static readonly byte[] IgnoreCaseEnUsSerializedBDD = ");
            GeneratorHelper.WriteByteArrayInitSyntax(sw, ignorecase.SerializeToBytes());
            sw.WriteLine(";");
        }

        private static List<EquivalenceClass> ComputeIgnoreCaseEquivalenceClasses(CharSetSolver solver, CultureInfo culture)
        {
            var ignoreCase = new Dictionary<char, EquivalenceClass>();
            var sets = new List<EquivalenceClass>();

            for (uint i = 65; i <= 0xFFFF; i++)
            {
                char C = (char)i;
                char c = char.ToLower(C, culture);

                if (c == C)
                {
                    continue;
                }

                EquivalenceClass? ec;
                if (!ignoreCase.TryGetValue(c, out ec))
                {
                    ec = new EquivalenceClass(solver.CharConstraint(c));
                    ignoreCase[c] = ec;
                    sets.Add(ec);
                }
                ec._set = solver.Or(ec._set, solver.CharConstraint(C));
            }
            return sets;
        }

        private class EquivalenceClass
        {
            public BDD _set;
            public EquivalenceClass(BDD set)
            {
                _set = set;
            }
        }
    };
#endif
}
