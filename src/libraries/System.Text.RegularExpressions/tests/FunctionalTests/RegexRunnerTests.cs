// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexRunnerTests
    {
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task EnginesThrowNotImplementedForGoAndFFC(RegexEngine engine)
        {
            Regex re = await RegexHelpers.GetRegexAsync(engine, @"abc");

            // Use reflection to ensure the runner is created so it can be fetched.
            MethodInfo createRunnerMethod = typeof(Regex).GetMethod("CreateRunner", BindingFlags.Instance | BindingFlags.NonPublic);
            RegexRunner runner = createRunnerMethod.Invoke(re, []) as RegexRunner;

            // Use reflection to call Go and FFC and ensure it throws NotImplementedException
            MethodInfo goMethod = typeof(RegexRunner).GetMethod("Go", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo ffcMethod = typeof(RegexRunner).GetMethod("FindFirstChar", BindingFlags.Instance | BindingFlags.NonPublic);

            // FindFirstChar and Go methods should not be implemented since built-in engines should be overriding and using Scan instead.
            TargetInvocationException goInvocationException = Assert.Throws<TargetInvocationException>(() => goMethod.Invoke(runner, []));
            Assert.Equal(typeof(NotImplementedException), goInvocationException.InnerException.GetType());
            TargetInvocationException ffcInvocationException = Assert.Throws<TargetInvocationException>(() => ffcMethod.Invoke(runner, []));
            Assert.Equal(typeof(NotImplementedException), ffcInvocationException.InnerException.GetType());
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task EnsureRunmatchValueIsNulledAfterIsMatch(RegexEngine engine)
        {
            Regex re = await RegexHelpers.GetRegexAsync(engine, @"abc");

            // First call IsMatch which should initialize runmatch on the runner.
            Assert.True(re.IsMatch("abcabcabc"));

            // Ensure runmatch wasn't nulled out, since after calling IsMatch it should be reused.
            FieldInfo runnerField = typeof(Regex).GetField("_runner", BindingFlags.Instance | BindingFlags.NonPublic);
            RegexRunner runner = runnerField.GetValue(re) as RegexRunner;
            FieldInfo runmatchField = typeof(RegexRunner).GetField("runmatch", BindingFlags.Instance | BindingFlags.NonPublic);
            Match runmatch = runmatchField.GetValue(runner) as Match;
            Assert.NotNull(runmatch);

            // Ensure that the Value of runmatch was nulled out, so as to not keep a reference to it in a cache.
            MethodInfo getTextMethod = typeof(Match).GetMethod("get_Text", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.Null(getTextMethod.Invoke(runmatch, []));
            Assert.Equal(string.Empty, runmatch.Value);
#if NET7_0_OR_GREATER
            Assert.True(runmatch.ValueSpan == ReadOnlySpan<char>.Empty);
#endif
        }
    }
}
