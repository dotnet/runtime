// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace Microsoft.NETCore.Platforms.BuildTasks.Tests
{
    public class RidTests
    {
        public static IEnumerable<object[]> ValidRIDData()
        {
            yield return new object[] { "win10-x64", new RID() { BaseRID = "win", OmitVersionDelimiter = true, Version = new RuntimeVersion("10"), Architecture = "x64" } };
            yield return new object[] { "win10", new RID() { BaseRID = "win", OmitVersionDelimiter = true, Version = new RuntimeVersion("10")} };
            yield return new object[] { "linux", new RID() { BaseRID = "linux" } };
            yield return new object[] { "linux-x64", new RID() { BaseRID = "linux", Architecture = "x64" } };
            yield return new object[] { "linux-x64", new RID() { BaseRID = "linux", Architecture = "x64" } };
            yield return new object[] { "debian.10-x64", new RID() { BaseRID = "debian", Version = new RuntimeVersion("10"), Architecture = "x64" } };
            yield return new object[] { "linuxmint.19.2-x64", new RID() { BaseRID = "linuxmint", Version = new RuntimeVersion("19.2"), Architecture = "x64" } };
            yield return new object[] { "ubuntu.14.04-x64", new RID() { BaseRID = "ubuntu", Version = new RuntimeVersion("14.04"), Architecture = "x64" } };
            yield return new object[] { "foo-bar.42-arm", new RID() { BaseRID = "foo-bar", Version = new RuntimeVersion("42"), Architecture = "arm" } };
            yield return new object[] { "foo-bar-arm", new RID() { BaseRID = "foo", Architecture = "bar", Qualifier = "arm" } };  // demonstrates ambiguity, avoid using `-` in base
            yield return new object[] { "linux-musl-x64", new RID() { BaseRID = "linux", Architecture = "musl", Qualifier = "x64" } };  // yes, we already have ambiguous RIDs
        }

        [Theory]
        [MemberData(nameof(ValidRIDData))]
        internal void ParseCorrectly(string input, RID expected)
        {
            RID actual = RID.Parse(input);

            Assert.Equal(expected, actual);
        }

        [Theory]
        [MemberData(nameof(ValidRIDData))]
        internal void ToStringAsExpected(string expected, RID rid)
        {
            string actual = rid.ToString();

            Assert.Equal(expected, actual);
        }
    }
}
