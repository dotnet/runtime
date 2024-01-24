// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
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
            => Assert.Equal(expectedName, TypeNameParser.Parse(input.AsSpan()).Name);

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("    ")]
        public void EmptyStringsAreNotAllowed(string input)
            => Assert.Throws<ArgumentException>(() => TypeNameParser.Parse(input.AsSpan()));

        [Theory]
        [InlineData("Namespace.Kość", "Namespace.Kość")]
        public void UnicodeCharactersAreAllowedByDefault(string input, string expectedName)
            => Assert.Equal(expectedName, TypeNameParser.Parse(input.AsSpan()).Name);

        [Theory]
        [InlineData("Namespace.Kość")]
        public void UsersCanCustomizeIdentifierValidation(string input)
            => Assert.Throws<ArgumentException>(() => TypeNameParser.Parse(input.AsSpan(), true, new NonAsciiNotAllowed()));

        public static IEnumerable<object[]> TypeNamesWithAssemblyNames()
        {
            yield return new object[]
            {
                "System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Int32",
                "mscorlib",
                new Version(4, 0, 0, 0),
                "b77a5c561934e089"
            };
        }

        [Theory]
        [MemberData(nameof(TypeNamesWithAssemblyNames))]
        public void TypeNameCanContainAssemblyName(string input, string typeName, string assemblyName, Version assemblyVersion, string assemblyPublicKeyToken)
        {
            TypeName parsed = TypeNameParser.Parse(input.AsSpan(), allowFullyQualifiedName: true);

            Assert.Equal(typeName, parsed.Name);
            Assert.NotNull(parsed.AssemblyName);
            Assert.Equal(assemblyName, parsed.AssemblyName.Name);
            Assert.Equal(assemblyVersion, parsed.AssemblyName.Version);
            Assert.Equal(GetPublicKeyToken(assemblyPublicKeyToken), parsed.AssemblyName.GetPublicKeyToken());

            static byte[] GetPublicKeyToken(string assemblyPublicKeyToken)
            {
                byte[] pkt = new byte[assemblyPublicKeyToken.Length / 2];
                int srcIndex = 0;
                for (int i = 0; i < pkt.Length; i++)
                {
                    char hi = assemblyPublicKeyToken[srcIndex++];
                    char lo = assemblyPublicKeyToken[srcIndex++];
                    pkt[i] = (byte)((FromHexChar(hi) << 4) | FromHexChar(lo));
                }
                return pkt;
            }

            static byte FromHexChar(char hex)
            {
                if (hex >= '0' && hex <= '9') return (byte)(hex - '0');
                else return (byte)(hex - 'a' + 10);
            }
        }

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
