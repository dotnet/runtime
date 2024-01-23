// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;
using System.Text;
using Xunit;

namespace System.Reflection.Metadata.Tests.Metadata
{
    public class TypeNameParserTests
    {
        [Theory]
        [InlineData("  System.Int32", "System.Int32")]
        public void SpacesAtTheBeginningAreOK(string input, string expectedName)
            => Assert.Equal(expectedName, TypeNameParser.Parse(input.AsSpan()).FullName);

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("    ")]
        public void EmptyStringsAreNotAllowed(string input)
            => Assert.Throws<ArgumentException>(() => TypeNameParser.Parse(input.AsSpan()));

        [Theory]
        [InlineData("Namespace.Kość", "Namespace.Kość")]
        public void UnicodeCharactersAreAllowedByDefault(string input, string expectedName)
            => Assert.Equal(expectedName, TypeNameParser.Parse(input.AsSpan()).FullName);

        [Theory]
        [InlineData("Namespace.Kość")]
        public void UsersCanCustomizeIdentifierValidation(string input)
            => Assert.Throws<ArgumentException>(() => TypeNameParser.Parse(input.AsSpan(), new NonAsciiNotAllowed()));

        internal sealed class NonAsciiNotAllowed : TypeNameParserOptions
        {
            public override void ValidateIdentifier(string candidate)
            {
                base.ValidateIdentifier(candidate);

#if NET8_0_OR_GREATER
                if (!Ascii.IsValid(candidate))
#else
                if (candidate.Any(c => c >= 128))
#endif
                {
                    throw new ArgumentException("Non ASCII char found");
                }
            }
        }

    }
}
