// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.CSharp.RuntimeBinder.Tests
{
    public class CSharpArgumentInfoTests
    {
        private static readonly IEnumerable<CSharpArgumentInfoFlags> AllPossibleFlags =
            Enumerable.Range(0, ((int[])Enum.GetValues(typeof(CSharpArgumentInfoFlags))).Max() * 2)
                .Select(i => (CSharpArgumentInfoFlags)i);

        private static readonly string[] Names =
        {
            "arg", "ARG", "Arg", "Argument name that isn’t a valid C♯ name 👿🤢",
            "horrid name with" + (char)0xD800 + "a half surrogate", "new", "break", null
        };

        private static IEnumerable<object[]> FlagsAndNames() =>
            AllPossibleFlags.Select((f, i) => new object[] {f, Names[i % Names.Length]});

        [Theory, MemberData(nameof(FlagsAndNames))]
        public void Create_ResultNotNull(CSharpArgumentInfoFlags flag, string name)
        {
            Assert.NotNull(CSharpArgumentInfo.Create(flag, name));
        }
    }
}
