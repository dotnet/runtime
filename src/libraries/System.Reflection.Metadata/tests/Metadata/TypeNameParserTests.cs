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
                "",
                "b77a5c561934e089"
            };
        }

        [Theory]
        [MemberData(nameof(TypeNamesWithAssemblyNames))]
        public void TypeNameCanContainAssemblyName(string input, string typeName, string assemblyName, Version assemblyVersion, string assemblyCulture, string assemblyPublicKeyToken)
        {
            TypeName parsed = TypeNameParser.Parse(input.AsSpan(), allowFullyQualifiedName: true);

            Assert.Equal(typeName, parsed.Name);
            Assert.NotNull(parsed.AssemblyName);
            Assert.Equal(assemblyName, parsed.AssemblyName.Name);
            Assert.Equal(assemblyVersion, parsed.AssemblyName.Version);
            Assert.Equal(assemblyCulture, parsed.AssemblyName.CultureName);
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

        public static IEnumerable<object[]> GenericArgumentsAreSupported_Arguments()
        {
            yield return new object[]
            {
                "Generic`1[[A]]",
                "Generic`1",
                new string[] { "A" },
                null
            };
            yield return new object[]
            {
                "Generic`3[[A],[B],[C]]",
                "Generic`3",
                new string[] { "A", "B", "C" },
                null
            };
            yield return new object[]
            {
                "Generic`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]",
                "Generic`1",
                new string[] { "System.Int32" },
                new AssemblyName[] { new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089") }
            };
            yield return new object[]
            {
                "Generic`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089], [System.Boolean, mscorlib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]",
                "Generic`2",
                new string[] { "System.Int32", "System.Boolean" },
                new AssemblyName[]
                {
                    new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                    new AssemblyName("mscorlib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
                }
            };
        }

        [Theory]
        [MemberData(nameof(GenericArgumentsAreSupported_Arguments))]
        public void GenericArgumentsAreSupported(string input, string typeName, string[] typeNames, AssemblyName[]? assemblyNames)
        {
            TypeName parsed = TypeNameParser.Parse(input.AsSpan(), allowFullyQualifiedName: true);

            Assert.Equal(typeName, parsed.Name);
            Assert.True(parsed.IsConstructedGenericType);
            Assert.False(parsed.IsElementalType);

            for (int i = 0; i < typeNames.Length; i++)
            {
                TypeName genericArg = parsed.GetGenericArguments()[i];
                Assert.Equal(typeNames[i], genericArg.Name);
                Assert.True(genericArg.IsElementalType);
                Assert.False(genericArg.IsConstructedGenericType);

                if (assemblyNames is not null)
                {
                    Assert.Equal(assemblyNames[i].FullName, genericArg.AssemblyName.FullName);
                }
            }
        }

        public static IEnumerable<object[]> DecoratorsAreSupported_Arguments()
        {
            yield return new object[]
            {
                "TypeName*", "TypeName", false, false, -1, false, true
            };
            yield return new object[]
            {
                "TypeName&", "TypeName", false, false, -1, true, false
            };
            yield return new object[]
            {
                "TypeName[]", "TypeName", true, true, 1, false, false
            };
            yield return new object[]
            {
                "TypeName[*]", "TypeName", true, false, 1, false, false
            };
            yield return new object[]
            {
                "TypeName[,,,]", "TypeName", true, false, 4, false, false
            };
        }

        [Theory]
        [MemberData(nameof(DecoratorsAreSupported_Arguments))]
        public void DecoratorsAreSupported(string input, string typeNameWithoutDecorators, bool isArray, bool isSzArray, int arrayRank, bool isByRef, bool isPointer)
        {
            TypeName parsed = TypeNameParser.Parse(input.AsSpan(), allowFullyQualifiedName: true);

            Assert.Equal(input, parsed.Name);
            Assert.Equal(isArray, parsed.IsArray);
            Assert.Equal(isSzArray, parsed.IsSzArrayType);
            if (isArray) Assert.Equal(arrayRank, parsed.GetArrayRank());
            Assert.Equal(isByRef, parsed.IsManagedPointerType);
            Assert.Equal(isPointer, parsed.IsUnmanagedPointerType);
            Assert.False(parsed.IsElementalType);

            TypeName underlyingType = parsed.UnderlyingType;
            Assert.NotNull(underlyingType);
            Assert.Equal(typeNameWithoutDecorators, underlyingType.Name);
            Assert.True(underlyingType.IsElementalType);
            Assert.False(underlyingType.IsArray);
            Assert.False(underlyingType.IsSzArrayType);
            Assert.False(underlyingType.IsManagedPointerType);
            Assert.False(underlyingType.IsUnmanagedPointerType);
            Assert.Null(underlyingType.UnderlyingType);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(int*))]
        [InlineData(typeof(int***))]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(int[,]))]
        [InlineData(typeof(int[,,,]))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(Dictionary<int, string>))]
        public void GetType_Roundtrip(Type type)
        {
            TypeName typeName = TypeNameParser.Parse(type.FullName.AsSpan(), allowFullyQualifiedName: true);

            Type afterRoundtrip = typeName.GetType(throwOnError: true);

            Assert.NotNull(afterRoundtrip);
            Assert.Equal(type, afterRoundtrip);
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
