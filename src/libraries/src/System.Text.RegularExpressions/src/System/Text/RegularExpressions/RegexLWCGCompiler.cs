// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using System.Globalization;

namespace System.Text.RegularExpressions
{
    internal sealed class RegexLWCGCompiler : RegexCompiler
    {
        private static int s_regexCount = 0;
        private static readonly Type[] s_paramTypes = new Type[] { typeof(RegexRunner) };

        /*
         * The top-level driver. Initializes everything then calls the Generate* methods.
         */
        public RegexRunnerFactory FactoryInstanceFromCode(RegexCode code, RegexOptions options)
        {
            _code = code;
            _codes = code.Codes;
            _strings = code.Strings;
            _fcPrefix = code.FCPrefix;
            _bmPrefix = code.BMPrefix;
            _anchors = code.Anchors;
            _trackcount = code.TrackCount;
            _options = options;

            // pick a unique number for the methods we generate
            int regexnum = Interlocked.Increment(ref s_regexCount);
            string regexnumString = regexnum.ToString(CultureInfo.InvariantCulture);

            DynamicMethod goMethod = DefineDynamicMethod("Go" + regexnumString, null, typeof(CompiledRegexRunner));
            GenerateGo();

            DynamicMethod firstCharMethod = DefineDynamicMethod("FindFirstChar" + regexnumString, typeof(bool), typeof(CompiledRegexRunner));
            GenerateFindFirstChar();

            DynamicMethod trackCountMethod = DefineDynamicMethod("InitTrackCount" + regexnumString, null, typeof(CompiledRegexRunner));
            GenerateInitTrackCount();

            return new CompiledRegexRunnerFactory(goMethod, firstCharMethod, trackCountMethod);
        }

        /*
         * Begins the definition of a new method (no args) with a specified return value
         */
        public DynamicMethod DefineDynamicMethod(string methname, Type returntype, Type hostType)
        {
            // We're claiming that these are static methods, but really they are instance methods.
            // By giving them a parameter which represents "this", we're tricking them into
            // being instance methods.

            MethodAttributes attribs = MethodAttributes.Public | MethodAttributes.Static;
            CallingConventions conventions = CallingConventions.Standard;

            DynamicMethod dm = new DynamicMethod(methname, attribs, conventions, returntype, s_paramTypes, hostType, false /*skipVisibility*/);
            _ilg = dm.GetILGenerator();
            return dm;
        }
    }
}
