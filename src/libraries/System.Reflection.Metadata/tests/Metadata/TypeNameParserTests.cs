// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class TypeNameParserTests
    {
        [Theory]
        [InlineData("  System.Int32", "System.Int32", "Int32")]
        [InlineData("  MyNamespace.MyType+NestedType", "MyNamespace.MyType+NestedType", "NestedType")]
        public void SpacesAtTheBeginningAreOK(string input, string expectedFullName, string expectedName)
        {
            TypeName parsed = TypeName.Parse(input.AsSpan());

            Assert.Equal(expectedName, parsed.Name);
            Assert.Equal(expectedFullName, parsed.FullName);
            Assert.Equal(expectedFullName, parsed.AssemblyQualifiedName);
        }

        [Theory]
        [InlineData("")]
        [InlineData(" ")]
        [InlineData("    ")]
        public void EmptyStringsAreNotAllowed(string input)
        {
            Assert.Throws<ArgumentException>(() => TypeName.Parse(input.AsSpan()));

            Assert.False(TypeName.TryParse(input.AsSpan(), out _));
        }

        [Theory]
        [InlineData("Namespace.Containing++Nested")] // a pair of '++'
        [InlineData("TypeNameFollowedBySome[] crap")] // unconsumed characters
        [InlineData("MissingAssemblyName, ")]
        [InlineData("ExtraComma, ,")]
        [InlineData("ExtraComma, , System.Runtime")]
        public void InvalidTypeNamesAreNotAllowed(string input)
        {
            Assert.Throws<ArgumentException>(() => TypeName.Parse(input.AsSpan()));

            Assert.False(TypeName.TryParse(input.AsSpan(), out _));
        }

        [Theory]
        [InlineData("Namespace.Kość", "Namespace.Kość")]
        public void UnicodeCharactersAreAllowedByDefault(string input, string expectedFullName)
            => Assert.Equal(expectedFullName, TypeName.Parse(input.AsSpan()).FullName);

        [Theory]
        [InlineData("Namespace.Kość")]
        public void UsersCanCustomizeIdentifierValidation(string input)
        {
            Assert.Throws<ArgumentException>(() => TypeName.Parse(input.AsSpan(), new NonAsciiNotAllowed()));

            Assert.False(TypeName.TryParse(input.AsSpan(), out _, new NonAsciiNotAllowed()));
        }

        public static IEnumerable<object[]> TypeNamesWithAssemblyNames()
        {
            yield return new object[]
            {
                "System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089",
                "System.Int32",
                "Int32",
                "mscorlib",
                new Version(4, 0, 0, 0),
                "",
                "b77a5c561934e089"
            };
        }

        [Theory]
        [MemberData(nameof(TypeNamesWithAssemblyNames))]
        public void TypeNameCanContainAssemblyName(string assemblyQualifiedName, string fullName, string name, string assemblyName,
            Version assemblyVersion, string assemblyCulture, string assemblyPublicKeyToken)
        {
            TypeName parsed = TypeName.Parse(assemblyQualifiedName.AsSpan());

            Assert.Equal(assemblyQualifiedName, parsed.AssemblyQualifiedName);
            Assert.Equal(fullName, parsed.FullName);
            Assert.Equal(name, parsed.Name);
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
                "Generic`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Boolean, mscorlib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]",
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
        public void GenericArgumentsAreSupported(string input, string name, string[] genericTypesFullNames, AssemblyName[]? assemblyNames)
        {
            TypeName parsed = TypeName.Parse(input.AsSpan());

            Assert.Equal(name, parsed.Name);
            Assert.Equal(input, parsed.FullName);
            Assert.True(parsed.IsConstructedGenericType);
            Assert.False(parsed.IsElementalType);

            for (int i = 0; i < genericTypesFullNames.Length; i++)
            {
                TypeName genericArg = parsed.GetGenericArguments()[i];
                Assert.Equal(genericTypesFullNames[i], genericArg.FullName);
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
            TypeName parsed = TypeName.Parse(input.AsSpan());

            Assert.Equal(input, parsed.FullName);
            Assert.Equal(isArray, parsed.IsArray);
            Assert.Equal(isSzArray, parsed.IsSzArrayType);
            if (isArray) Assert.Equal(arrayRank, parsed.GetArrayRank());
            Assert.Equal(isByRef, parsed.IsManagedPointerType);
            Assert.Equal(isPointer, parsed.IsUnmanagedPointerType);
            Assert.False(parsed.IsElementalType);

            TypeName underlyingType = parsed.UnderlyingType;
            Assert.NotNull(underlyingType);
            Assert.Equal(typeNameWithoutDecorators, underlyingType.FullName);
            Assert.True(underlyingType.IsElementalType);
            Assert.False(underlyingType.IsArray);
            Assert.False(underlyingType.IsSzArrayType);
            Assert.False(underlyingType.IsManagedPointerType);
            Assert.False(underlyingType.IsUnmanagedPointerType);
            Assert.Null(underlyingType.UnderlyingType);
        }

        public static IEnumerable<object[]> GetAdditionalConstructedTypeData()
        {
            yield return new object[] { typeof(Dictionary<List<int[]>[,], List<int?[][][,]>>[]), 16 };

            // "Dictionary<List<int[]>[,], List<int?[][][,]>>[]" breaks down to complexity 16 like so:
            //
            // 01: Dictionary<List<int[]>[,], List<int?[][][,]>>[]
            // 02: `- Dictionary<List<int[]>[,], List<int?[][][,]>>
            // 03:    +- Dictionary`2
            // 04:    +- List<int[]>[,]
            // 05:    |  `- List<int[]>
            // 06:    |     +- List`1
            // 07:    |     `- int[]
            // 08:    |        `- int
            // 09:    `- List<int?[][][,]>
            // 10:       +- List`1
            // 11:       `- int?[][][,]
            // 12:          `- int?[][]
            // 13:             `- int?[]
            // 14:                `- int?
            // 15:                   +- Nullable`1
            // 16:                   `- int

            yield return new object[] { typeof(int[]).MakePointerType().MakeByRefType(), 4 }; // int[]*&
            yield return new object[] { typeof(long).MakeArrayType(31), 2 }; // long[,,,,,,,...]
            yield return new object[] { typeof(long).Assembly.GetType("System.Int64[*]"), 2 }; // long[*]
        }

        [Theory]
        [InlineData(typeof(TypeName), 1)]
        [InlineData(typeof(TypeNameParserTests), 1)]
        [InlineData(typeof(object), 1)]
        [InlineData(typeof(Assert), 1)] // xunit
        [InlineData(typeof(int[]), 2)]
        [InlineData(typeof(int[,][]), 3)]
        [InlineData(typeof(Nullable<>), 1)] // open generic type treated as elemental
        [MemberData(nameof(GetAdditionalConstructedTypeData))]
        public void TotalComplexityReturnsExpectedValue(Type type, int expectedComplexity)
        {
            TypeName parsed = TypeName.Parse(type.AssemblyQualifiedName.AsSpan());

            Assert.Equal(expectedComplexity, parsed.TotalComplexity);

            Assert.Equal(type.Name, parsed.Name);
            Assert.Equal(type.FullName, parsed.FullName);
            Assert.Equal(type.AssemblyQualifiedName, parsed.AssemblyQualifiedName);
        }

        [Theory]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(List<List<int>>))]
        [InlineData(typeof(Dictionary<int, string>))]
        [InlineData(typeof(Dictionary<string, List<string>>))]
        [InlineData(typeof(NestedGeneric_0<int>.NestedGeneric_1<string, bool>))]
        [InlineData(typeof(NestedGeneric_0<int>.NestedGeneric_1<string, bool>.NestedGeneric_2<short, byte, sbyte>))]
        [InlineData(typeof(NestedGeneric_0<int>.NestedGeneric_1<string, bool>.NestedGeneric_2<short, byte, sbyte>.NestedNonGeneric_3))]
        public void ParsedNamesMatchSystemTypeNames(Type type)
        {
            TypeName parsed = TypeName.Parse(type.AssemblyQualifiedName.AsSpan());

            Assert.Equal(type.Name, parsed.Name);
            Assert.Equal(type.FullName, parsed.FullName);
            Assert.Equal(type.AssemblyQualifiedName, parsed.AssemblyQualifiedName);

            Type genericType = type.GetGenericTypeDefinition();
            Assert.Equal(genericType.Name, parsed.UnderlyingType.Name);
            Assert.Equal(genericType.FullName, parsed.UnderlyingType.FullName);
            Assert.Equal(genericType.AssemblyQualifiedName, parsed.UnderlyingType.AssemblyQualifiedName);
        }

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(int?))]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(int[,]))]
        [InlineData(typeof(int[,,,]))]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(List<List<int>>))]
        [InlineData(typeof(Dictionary<int, string>))]
        [InlineData(typeof(Dictionary<string, List<string>>))]
        [InlineData(typeof(NestedNonGeneric_0))]
        [InlineData(typeof(NestedNonGeneric_0.NestedNonGeneric_1))]
        [InlineData(typeof(NestedGeneric_0<int>))]
        [InlineData(typeof(NestedGeneric_0<int>.NestedGeneric_1<string, bool>))]
        [InlineData(typeof(NestedGeneric_0<int>.NestedGeneric_1<string, bool>.NestedGeneric_2<short, byte, sbyte>))]
        [InlineData(typeof(NestedGeneric_0<int>.NestedGeneric_1<string, bool>.NestedGeneric_2<short, byte, sbyte>.NestedNonGeneric_3))]
        public void CanImplementGetTypeUsingPublicAPIs_Roundtrip(Type type)
        {
            Test(type);
            Test(type.MakePointerType());
            Test(type.MakePointerType().MakePointerType());
            Test(type.MakeByRefType());

            if (!type.IsArray)
            {
                Test(type.MakeArrayType());  // []
                Test(type.MakeArrayType(1)); // [*]
                Test(type.MakeArrayType(2)); // [,]
            }

            static void Test(Type type)
            {
                TypeName parsed = TypeName.Parse(type.AssemblyQualifiedName.AsSpan());

                // ensure that Name, FullName and AssemblyQualifiedName match reflection APIs!!
                Assert.Equal(type.Name, parsed.Name);
                Assert.Equal(type.FullName, parsed.FullName);
                Assert.Equal(type.AssemblyQualifiedName, parsed.AssemblyQualifiedName);
                // now load load the type from name
                Verify(type, parsed, ignoreCase: false);
#if NETCOREAPP  // something weird is going on here
                // load using lowercase name
                Verify(type, TypeName.Parse(type.AssemblyQualifiedName.ToLower().AsSpan()), ignoreCase: true);
                // load using uppercase name
                Verify(type, TypeName.Parse(type.AssemblyQualifiedName.ToUpper().AsSpan()), ignoreCase: true);
#endif

                static void Verify(Type type, TypeName typeName, bool ignoreCase)
                {
                    Type afterRoundtrip = GetType(typeName, throwOnError: true, ignoreCase: ignoreCase);

                    Assert.NotNull(afterRoundtrip);
                    Assert.Equal(type, afterRoundtrip);
                }
            }

#if NET8_0_OR_GREATER
            [RequiresUnreferencedCode("The type might be removed")]
            [RequiresDynamicCode("Required by MakeArrayType")]
#else
#pragma warning disable IL2055, IL2057, IL2075, IL2096
#endif
            static Type? GetType(TypeName typeName, bool throwOnError = true, bool ignoreCase = false)
            {
                if (typeName.ContainingType is not null) // nested type
                {
                    BindingFlags flagsCopiedFromClr = BindingFlags.NonPublic | BindingFlags.Public;
                    if (ignoreCase)
                    {
                        flagsCopiedFromClr |= BindingFlags.IgnoreCase;
                    }
                    return Make(GetType(typeName.ContainingType, throwOnError, ignoreCase)?.GetNestedType(typeName.Name, flagsCopiedFromClr));
                }
                else if (typeName.UnderlyingType is null) // elemental
                {
                    Type? type = typeName.AssemblyName is null
                        ? Type.GetType(typeName.FullName, throwOnError, ignoreCase)
                        : Assembly.Load(typeName.AssemblyName).GetType(typeName.FullName, throwOnError, ignoreCase);

                    return Make(type);
                }

                return Make(GetType(typeName.UnderlyingType, throwOnError, ignoreCase));

                Type? Make(Type? type)
                {
                    if (type is null || typeName.IsElementalType)
                    {
                        return type;
                    }
                    else if (typeName.IsConstructedGenericType)
                    {
                        TypeName[] genericArgs = typeName.GetGenericArguments();
                        Type[] genericTypes = new Type[genericArgs.Length];
                        for (int i = 0; i < genericArgs.Length; i++)
                        {
                            Type? genericArg = GetType(genericArgs[i], throwOnError, ignoreCase);
                            if (genericArg is null)
                            {
                                return null;
                            }
                            genericTypes[i] = genericArg;
                        }

                        return type.MakeGenericType(genericTypes);
                    }
                    else if (typeName.IsManagedPointerType)
                    {
                        return type.MakeByRefType();
                    }
                    else if (typeName.IsUnmanagedPointerType)
                    {
                        return type.MakePointerType();
                    }
                    else if (typeName.IsSzArrayType)
                    {
                        return type.MakeArrayType();
                    }
                    else
                    {
                        return type.MakeArrayType(rank: typeName.GetArrayRank());
                    }
                }
            }
#pragma warning restore IL2055, IL2057, IL2075, IL2096
        }

        public class NestedNonGeneric_0
        {
            public class NestedNonGeneric_1 { }
        }

        public class NestedGeneric_0<T1>
        {
            public class NestedGeneric_1<T2, T3>
            {
                public class NestedGeneric_2<T4, T5, T6>
                {
                    public class NestedNonGeneric_3 { }
                }
            }
        }

        internal sealed class NonAsciiNotAllowed : TypeNameParserOptions
        {
            public override bool ValidateIdentifier(ReadOnlySpan<char> candidate, bool throwOnError)
            {
                if (!base.ValidateIdentifier(candidate, throwOnError))
                {
                    return false;
                }

#if NET8_0_OR_GREATER
                if (!Ascii.IsValid(candidate))
#else
                if (candidate.ToArray().Any(c => c >= 128))
#endif
                {
                    if (throwOnError)
                    {
                        throw new ArgumentException("Non ASCII char found");
                    }
                    return false;
                }
                return true;
            }
        }

    }
}
