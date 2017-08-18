// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Data.Odbc.Tests
{
    public class CommandBuilderTests : IntegrationTestBase
    {
        [Fact(Skip = "Native dependencies missing in CI. See https://github.com/dotnet/corefx/issues/15776.")]
        public void QuoteIdentifier_UseConnection()
        {
            var commandBuilder = new OdbcCommandBuilder();

            // Get quote string
            var quotedIdentifier = commandBuilder.QuoteIdentifier("Test", connection);
            var qs = quotedIdentifier.Remove(quotedIdentifier.IndexOf("Test"));
            Assert.NotEmpty(qs);

            // Test -> 'Test'
            var quotedTestString = commandBuilder.QuoteIdentifier("Source", connection);
            Assert.Equal($"{qs}Source{qs}", quotedTestString);
            // 'Test' -> Test
            Assert.Equal("Source", commandBuilder.UnquoteIdentifier(quotedTestString, connection));

            // Test' -> 'Test'''
            quotedTestString = commandBuilder.QuoteIdentifier($"Test identifier{qs}", connection);
            Assert.Equal($"{qs}Test identifier{qs}{qs}{qs}", quotedTestString);
            // 'Test''' -> Test'
            Assert.Equal($"Test identifier{qs}", commandBuilder.UnquoteIdentifier(quotedTestString, connection));

            // Needs an active connection
            Assert.Throws<InvalidOperationException>(() => commandBuilder.QuoteIdentifier("Test", null));
            Assert.Throws<InvalidOperationException>(() => commandBuilder.QuoteIdentifier("Test"));
            Assert.Throws<InvalidOperationException>(() => commandBuilder.UnquoteIdentifier("Test", null));
            Assert.Throws<InvalidOperationException>(() => commandBuilder.UnquoteIdentifier("Test"));
        }

        [Fact(Skip = "Native dependencies missing in CI. See https://github.com/dotnet/corefx/issues/15776.")]
        public void QuoteIdentifier_CustomPrefixSuffix()
        {
            var commandBuilder = new OdbcCommandBuilder();

            // Custom prefix & suffix
            commandBuilder.QuotePrefix = "'";
            commandBuilder.QuoteSuffix = "'";

            Assert.Equal("'Test'", commandBuilder.QuoteIdentifier("Test", connection));
            Assert.Equal("'Te''st'", commandBuilder.QuoteIdentifier("Te'st", connection));
            Assert.Equal("Test", commandBuilder.UnquoteIdentifier("'Test'", connection));
            Assert.Equal("Te'st", commandBuilder.UnquoteIdentifier("'Te''st'", connection));
            
            // Ensure we don't need active connection:
            Assert.Equal("'Test'", commandBuilder.QuoteIdentifier("Test", null));
            Assert.Equal("Test", commandBuilder.UnquoteIdentifier("'Test'", null));
        }
    }
}
