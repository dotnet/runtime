// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexLWCGCompiler : RegexCompiler
    {
        /// <summary>
        /// Name of the environment variable used to opt-in to including the regex pattern in the DynamicMethod name.
        /// Set the environment variable to "1" to turn this on.
        /// </summary>
        private const string IncludePatternInNamesEnvVar = "DOTNET_SYSTEM_TEXT_REGULAREXPRESSIONS_PATTERNINNAME";

        /// <summary>
        /// If true, the pattern (or a portion of it) are included in the generated DynamicMethod names.
        /// </summary>
        /// <remarks>
        /// This is opt-in to avoid exposing the pattern, which may itself be dynamically built in diagnostics by default.
        /// </remarks>
        private static readonly bool s_includePatternInName = Environment.GetEnvironmentVariable(IncludePatternInNamesEnvVar) == "1";

        /// <summary>Parameter types for the generated Go and FindFirstChar methods.</summary>
        private static readonly Type[] s_paramTypes = new Type[] { typeof(RegexRunner), typeof(ReadOnlySpan<char>) };

        /// <summary>Id number to use for the next compiled regex.</summary>
        private static int s_regexCount;

        /// <summary>The top-level driver. Initializes everything then calls the Generate* methods.</summary>
        [RequiresDynamicCode("Compiling a RegEx requires dynamic code.")]
        public RegexRunnerFactory? FactoryInstanceFromCode(string pattern, RegexTree regexTree, RegexOptions options, bool hasTimeout)
        {
            if (!regexTree.Root.SupportsCompilation(out _))
            {
                return null;
            }

            _regexTree = regexTree;
            _options = options;
            _hasTimeout = hasTimeout;

            // Pick a unique number for the methods we generate.
            uint regexNum = (uint)Interlocked.Increment(ref s_regexCount);

            // Get a description of the regex to use in the name.  This is helpful when profiling, and is opt-in.
            string description = string.Empty;
            if (s_includePatternInName)
            {
                const int DescriptionLimit = 100; // arbitrary limit to avoid very long method names
                description = string.Concat("_", pattern.Length > DescriptionLimit ? pattern.AsSpan(0, DescriptionLimit) : pattern);
            }

            DynamicMethod tryfindNextPossibleStartPositionMethod = DefineDynamicMethod($"Regex{regexNum}_TryFindNextPossibleStartingPosition{description}", typeof(bool), typeof(CompiledRegexRunner), s_paramTypes);
            EmitTryFindNextPossibleStartingPosition();

            DynamicMethod tryMatchAtCurrentPositionMethod = DefineDynamicMethod($"Regex{regexNum}_TryMatchAtCurrentPosition{description}", typeof(bool), typeof(CompiledRegexRunner), s_paramTypes);
            EmitTryMatchAtCurrentPosition();

            DynamicMethod scanMethod = DefineDynamicMethod($"Regex{regexNum}_Scan{description}", null, typeof(CompiledRegexRunner), new[] { typeof(RegexRunner), typeof(ReadOnlySpan<char>) });
            EmitScan(options, tryfindNextPossibleStartPositionMethod, tryMatchAtCurrentPositionMethod);

            return new CompiledRegexRunnerFactory(scanMethod, _searchValues?.ToArray(), regexTree.Culture);
        }

        /// <summary>Begins the definition of a new method (no args) with a specified return value.</summary>
        [RequiresDynamicCode("Compiling a RegEx requires dynamic code.")]
        private DynamicMethod DefineDynamicMethod(string methname, Type? returntype, Type hostType, Type[] paramTypes)
        {
            // We're claiming that these are static methods, but really they are instance methods.
            // By giving them a parameter which represents "this", we're tricking them into
            // being instance methods.

            const MethodAttributes Attribs = MethodAttributes.Public | MethodAttributes.Static;
            const CallingConventions Conventions = CallingConventions.Standard;

            var dm = new DynamicMethod(methname, Attribs, Conventions, returntype, paramTypes, hostType, skipVisibility: false);
            _ilg = dm.GetILGenerator();
            return dm;
        }
    }
}
