// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace System.Text.RegularExpressions.Tests
{
    public static class RegexHelpers
    {
        public const string DefaultMatchTimeout_ConfigKeyName = "REGEX_DEFAULT_MATCH_TIMEOUT";

        public static bool IsDefaultCount(string input, RegexOptions options, int count)
        {
            if ((options & RegexOptions.RightToLeft) != 0)
            {
                return count == input.Length || count == -1;
            }
            return count == input.Length;
        }

        public static bool IsDefaultStart(string input, RegexOptions options, int start)
        {
            if ((options & RegexOptions.RightToLeft) != 0)
            {
                return start == input.Length;
            }
            return start == 0;
        }

        public static IEnumerable<object[]> AvailableEngines_MemberData =>
            from engine in AvailableEngines
            select new object[] { engine };

        public static IEnumerable<object[]> PrependEngines(IEnumerable<object[]> cases)
        {
            foreach (RegexEngine engine in AvailableEngines)
            {
                foreach (object[] additionalParameters in cases)
                {
                    var parameters = new object[additionalParameters.Length + 1];
                    additionalParameters.CopyTo(parameters, 1);
                    parameters[0] = engine;
                    yield return parameters;
                }
            }
        }

        public static IEnumerable<RegexEngine> AvailableEngines
        {
            get
            {
                yield return RegexEngine.Interpreter;
                yield return RegexEngine.Compiled;
                if (PlatformDetection.IsNetCore &&
                    PlatformDetection.IsReflectionEmitSupported && // the source generator doesn't use reflection emit, but it does use Roslyn for the equivalent
                    PlatformDetection.IsNotBrowser)
                {
                    yield return RegexEngine.SourceGenerated;
                }
            }
        }

        public static async Task<Regex> GetRegex(RegexEngine engine, string pattern, RegexOptions options = RegexOptions.None, int matchTimeout = -1)
        {
            switch (engine)
            {
                case RegexEngine.Interpreter:
                    return new Regex(pattern, options, TimeSpan.FromMilliseconds(matchTimeout));

                case RegexEngine.Compiled:
                    return new Regex(pattern, options | RegexOptions.Compiled, TimeSpan.FromMilliseconds(matchTimeout));

                case RegexEngine.SourceGenerated:
                    return await RegexGeneratorHelper.SourceGenRegex(pattern, options, matchTimeout);
            }

            throw new ArgumentException($"Unknown engine: {engine}");
        }

        public static Task<Regex> GetRegex(RegexEngine engine, string pattern, RegexOptions options, TimeSpan timeout) =>
            GetRegex(engine, pattern, options, (int)timeout.TotalMilliseconds);
    }

    public enum RegexEngine
    {
        Interpreter,
        Compiled,
        SourceGenerated
    }

    public class CaptureData
    {
        private CaptureData(string value, int index, int length, bool createCaptures)
        {
            Value = value;
            Index = index;
            Length = length;

            // Prevent a StackOverflow recursion in the constructor
            if (createCaptures)
            {
                Captures = new CaptureData[] { new CaptureData(value, index, length, false) };
            }
        }

        public CaptureData(string value, int index, int length) : this(value, index, length, true)
        {
        }

        public CaptureData(string value, int index, int length, CaptureData[] captures) : this(value, index, length, false)
        {
            Captures = captures;
        }

        public string Value { get; }
        public int Index { get; }
        public int Length { get; }
        public CaptureData[] Captures { get; }
    }
}
