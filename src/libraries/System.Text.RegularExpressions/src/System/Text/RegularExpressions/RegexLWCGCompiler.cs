// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Reflection;
using System.Reflection.Emit;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexLWCGCompiler : RegexCompiler
    {
        private static readonly Type[] s_paramTypes = new Type[] { typeof(RegexRunner) };
        private static int s_regexCount = 0;

        /// <summary>The top-level driver. Initializes everything then calls the Generate* methods.</summary>
        public RegexRunnerFactory FactoryInstanceFromCode(RegexCode code, RegexOptions options, bool hasTimeout)
        {
            _code = code;
            _codes = code.Codes;
            _strings = code.Strings;
            _fcPrefix = code.FCPrefix;
            _bmPrefix = code.BMPrefix;
            _anchors = code.Anchors;
            _trackcount = code.TrackCount;
            _options = options;
            _hasTimeout = hasTimeout;

            // pick a unique number for the methods we generate
            string regexnumString = ((uint)Interlocked.Increment(ref s_regexCount)).ToString();

            DynamicMethod goMethod = DefineDynamicMethod("Go" + regexnumString, null, typeof(CompiledRegexRunner));
            GenerateGo();

            DynamicMethod findFirstCharMethod = DefineDynamicMethod("FindFirstChar" + regexnumString, typeof(bool), typeof(CompiledRegexRunner));
            GenerateFindFirstChar();

            return new CompiledRegexRunnerFactory(goMethod, findFirstCharMethod, _trackcount);
        }

        /// <summary>Begins the definition of a new method (no args) with a specified return value.</summary>
        public DynamicMethod DefineDynamicMethod(string methname, Type? returntype, Type hostType)
        {
            // We're claiming that these are static methods, but really they are instance methods.
            // By giving them a parameter which represents "this", we're tricking them into
            // being instance methods.

            const MethodAttributes Attribs = MethodAttributes.Public | MethodAttributes.Static;
            const CallingConventions Conventions = CallingConventions.Standard;

            var dm = new DynamicMethod(methname, Attribs, Conventions, returntype, s_paramTypes, hostType, skipVisibility: false);
            _ilg = dm.GetILGenerator();
            return dm;
        }
    }
}
