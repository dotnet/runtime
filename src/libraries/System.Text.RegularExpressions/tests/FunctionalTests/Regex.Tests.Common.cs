// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Tests;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public static class RegexHelpers
    {
        public const string DefaultMatchTimeout_ConfigKeyName = "REGEX_DEFAULT_MATCH_TIMEOUT";

        public const int StressTestNestingDepth = 1000;

        /// <summary>RegexOptions.NonBacktracking.</summary>
        /// <remarks>Defined here to be able to reference the value by name even on .NET Framework test builds.</remarks>
        public const RegexOptions RegexOptionNonBacktracking = (RegexOptions)0x400;

        /// <summary>RegexOptions.NonBacktracking.</summary>
        /// <remarks>Defined here to be able to reference the value even in release builds.</remarks>
        public const RegexOptions RegexOptionDebug = (RegexOptions)0x80;

        static RegexHelpers()
        {
            if (PlatformDetection.IsNetCore)
            {
                Assert.Equal(RegexOptionNonBacktracking, Enum.Parse(typeof(RegexOptions), "NonBacktracking"));
            }
        }

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

        public static async Task<Regex> GetRegexAsync(RegexEngine engine, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, RegexOptions options, Globalization.CultureInfo culture)
        {
            if (engine == RegexEngine.SourceGenerated)
            {
                return await RegexGeneratorHelper.SourceGenRegexAsync(pattern, culture, options);
            }

            using (new System.Tests.ThreadCultureChange(culture))
            {
                return await GetRegexAsync(engine, pattern, options);
            }
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
                if (PlatformDetection.IsNetCore)
                {
                    yield return RegexEngine.NonBacktracking;

                    if (PlatformDetection.IsReflectionEmitSupported && // the source generator doesn't use reflection emit, but it does use Roslyn for the equivalent
                        PlatformDetection.IsNotMobile &&
                        PlatformDetection.IsNotBrowser)
                    {
                        yield return RegexEngine.SourceGenerated;
                    }
                }
            }
        }

        public static bool IsNonBacktracking(RegexEngine engine) =>
            engine is RegexEngine.NonBacktracking;

        public static async Task<Regex> GetRegexAsync(RegexEngine engine, [StringSyntax(StringSyntaxAttribute.Regex)] string pattern, RegexOptions? options = null, TimeSpan? matchTimeout = null)
        {
            if (options is null)
            {
                Assert.Null(matchTimeout);
            }

            if (engine == RegexEngine.SourceGenerated)
            {
                return await RegexGeneratorHelper.SourceGenRegexAsync(pattern, null, options, matchTimeout);
            }

            return
                options is null ? new Regex(pattern, OptionsFromEngine(engine)) :
                matchTimeout is null ? new Regex(pattern, options.Value | OptionsFromEngine(engine)) :
                new Regex(pattern, options.Value | OptionsFromEngine(engine), matchTimeout.Value);
        }

        public static async Task<Regex[]> GetRegexesAsync(RegexEngine engine, params (string pattern, CultureInfo? culture, RegexOptions? options, TimeSpan? matchTimeout)[] regexes)
        {
            if (engine == RegexEngine.SourceGenerated)
            {
                return await RegexGeneratorHelper.SourceGenRegexAsync(regexes);
            }

            var results = new Regex[regexes.Length];
            for (int i = 0; i < regexes.Length; i++)
            {
                (string pattern, CultureInfo? culture, RegexOptions? options, TimeSpan? matchTimeout) = regexes[i];

                using (new ThreadCultureChange(culture))
                {
                    try
                    {
                        results[i] =
                            options is null ? new Regex(pattern, OptionsFromEngine(engine)) :
                            matchTimeout is null ? new Regex(pattern, options.Value | OptionsFromEngine(engine)) :
                            new Regex(pattern, options.Value | OptionsFromEngine(engine), matchTimeout.Value);
                    }
                    catch (ArgumentOutOfRangeException aoore)
                    {
                        throw new ArgumentOutOfRangeException($"{engine}, {pattern}, {options}", aoore);
                    }
                    catch (NotSupportedException nse)
                    {
                        throw new NotSupportedException($"{engine}, {pattern}, {options}", nse);
                    }
                }
            }

            return results;
        }

        public static RegexOptions OptionsFromEngine(RegexEngine engine) => engine switch
        {
            RegexEngine.Interpreter => RegexOptions.None,
            RegexEngine.Compiled => RegexOptions.Compiled,
            RegexEngine.SourceGenerated => RegexOptions.Compiled,
            RegexEngine.NonBacktracking => RegexOptionNonBacktracking,
            _ => throw new ArgumentException($"Unknown engine: {engine}"),
        };

        /// <summary>Set the AppContext variable REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE to the given max value. Only used with Nonbacktracking engine.</summary>
        public static void SetSafeSizeThreshold(int maxSize)
        {
#if NET7_0_OR_GREATER
            AppContext.SetData("REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE", maxSize);
#endif
        }

        /// <summary>Remove the AppContext variable REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE value. Only used with Nonbacktracking engine.</summary>
        public static void RestoreSafeSizeThresholdToDefault()
        {
#if NET7_0_OR_GREATER
            AppContext.SetData("REGEX_NONBACKTRACKING_MAX_AUTOMATA_SIZE", null);
#endif
        }
    }

    public enum RegexEngine
    {
        Interpreter,
        Compiled,
        NonBacktracking,
        SourceGenerated,
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
                Captures = [new CaptureData(value, index, length, false)];
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
