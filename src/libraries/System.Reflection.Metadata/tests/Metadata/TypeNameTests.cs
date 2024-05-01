// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection.Emit;
using Xunit;

namespace System.Reflection.Metadata.Tests
{
    public class TypeNameTests
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

        [Fact]
        public void LeadingDotIsNotConsumedForFullTypeNamesWithoutNamespace()
        {
            // This is true only for the public API.
            // The internal CoreLib implementation consumes the leading dot for backward compat.
            TypeName parsed = TypeName.Parse(".NoNamespace".AsSpan());

            Assert.Equal("NoNamespace", parsed.Name);
            Assert.Equal(".NoNamespace", parsed.FullName);
            Assert.Equal(".NoNamespace", parsed.AssemblyQualifiedName);
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
        [InlineData("TypeNameFollowedBySome[] unconsumedCharacters")]
        [InlineData("MissingAssemblyName, ")]
        [InlineData("ExtraComma, ,")]
        [InlineData("ExtraComma, , System.Runtime")]
        [InlineData("UsingGenericSyntaxButNotProvidingGenericArgs[[]]")]
        [InlineData("ExtraCommaAfterFirstGenericArg`1[[type1, assembly1],]")]
        [InlineData("MissingClosingSquareBrackets`1[[type1, assembly1")] // missing ]]
        [InlineData("MissingClosingSquareBracket`1[[type1, assembly1]")] // missing ]
        [InlineData("MissingClosingSquareBracketsMixedMode`2[[type1, assembly1], type2")] // missing ]
        [InlineData("MissingClosingSquareBrackets`2[[type1, assembly1], [type2, assembly2")] // missing ]
        [InlineData("MissingClosingSquareBracketsMixedMode`2[type1, [type2, assembly2")] // missing ]]
        [InlineData("MissingClosingSquareBracketsMixedMode`2[type1, [type2, assembly2]")] // missing ]
        [InlineData("EscapeCharacterAtTheEnd\\")]
        [InlineData("EscapeNonSpecialChar\\a")]
        [InlineData("EscapeNonSpecialChar\\0")]
        [InlineData("DoubleNestingChar++Bla")]
        public void InvalidTypeNamesAreNotAllowed(string input)
        {
            Assert.Throws<ArgumentException>(() => TypeName.Parse(input.AsSpan()));

            Assert.False(TypeName.TryParse(input.AsSpan(), out _));
        }

        [Theory]
        [InlineData("System.Int32&&")] // by-ref to by-ref is currently not supported by CLR
        [InlineData("System.Int32[,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,,]")] // more than max array rank (32)
        public void ParserIsNotEnforcingRuntimeSpecificRules(string input)
        {
            Assert.True(TypeName.TryParse(input.AsSpan(), out _));

            if (PlatformDetection.IsNotMonoRuntime) // https://github.com/dotnet/runtime/issues/45033
            {
#if NET
                Assert.Throws<TypeLoadException>(() => Type.GetType(input));
#endif
            }
        }

        [Theory]
        [InlineData("Namespace.Kość", "Namespace.Kość")]
        public void UnicodeCharactersAreAllowedByDefault(string input, string expectedFullName)
            => Assert.Equal(expectedFullName, TypeName.Parse(input.AsSpan()).FullName);

        [Theory]
        [InlineData(typeof(int))]
        [InlineData(typeof(Dictionary<string, bool>))]
        [InlineData(typeof(int[][]))]
        [InlineData(typeof(Assert))] // xUnit assembly
        [InlineData(typeof(TypeNameTests))] // test assembly
        [InlineData(typeof(NestedGeneric_0<int>.NestedGeneric_1<string, bool>.NestedGeneric_2<short, byte, sbyte>.NestedNonGeneric_3))]
        public void TypeNameCanContainAssemblyName(Type type)
        {
            AssemblyName expectedAssemblyName = new(type.Assembly.FullName);

            Verify(type, expectedAssemblyName, TypeName.Parse(type.AssemblyQualifiedName.AsSpan()));

            static void Verify(Type type, AssemblyName expectedAssemblyName, TypeName parsed)
            {
                Assert.Equal(type.AssemblyQualifiedName, parsed.AssemblyQualifiedName);
                Assert.Equal(type.FullName, parsed.FullName);
                Assert.Equal(type.Name, parsed.Name);

                AssemblyNameInfo parsedAssemblyName = parsed.AssemblyName;
                Assert.NotNull(parsedAssemblyName);

                Assert.Equal(expectedAssemblyName.Name, parsedAssemblyName.Name);
                Assert.Equal(expectedAssemblyName.Version, parsedAssemblyName.Version);
                Assert.Equal(expectedAssemblyName.CultureName, parsedAssemblyName.CultureName);
                Assert.Equal(expectedAssemblyName.FullName, parsedAssemblyName.FullName);

                Assert.Equal(default, parsedAssemblyName.Flags);
            }
        }

        [Theory]
        [InlineData(10, "*")] // pointer to pointer
        [InlineData(10, "[]")] // array of arrays
        [InlineData(100, "*")]
        [InlineData(100, "[]")]
        public void MaxNodesIsRespected_TooManyDecorators(int maxDepth, string decorator)
        {
            TypeNameParseOptions options = new()
            {
                MaxNodes = maxDepth
            };

            string notTooMany = $"System.Int32{string.Join("", Enumerable.Repeat(decorator, maxDepth - 1))}";
            string tooMany = $"System.Int32{string.Join("", Enumerable.Repeat(decorator, maxDepth))}";

            Assert.Throws<InvalidOperationException>(() => TypeName.Parse(tooMany.AsSpan(), options));
            Assert.False(TypeName.TryParse(tooMany.AsSpan(), out _, options));

            TypeName parsed = TypeName.Parse(notTooMany.AsSpan(), options);
            ValidateElementType(maxDepth, parsed, decorator);

            Assert.True(TypeName.TryParse(notTooMany.AsSpan(), out parsed, options));
            ValidateElementType(maxDepth, parsed, decorator);

            static void ValidateElementType(int maxDepth, TypeName parsed, string decorator)
            {
                for (int i = 0; i < maxDepth - 1; i++)
                {
                    Assert.Equal(decorator == "*", parsed.IsPointer);
                    Assert.Equal(decorator == "[]", parsed.IsSZArray);
                    Assert.False(parsed.IsConstructedGenericType);

                    parsed = parsed.GetElementType();
                }
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        public void MaxNodesIsRespected_TooDeepGenerics(int maxDepth)
        {
            TypeNameParseOptions options = new()
            {
                MaxNodes = maxDepth
            };

            string tooDeep = GetName(maxDepth);
            string notTooDeep = GetName(maxDepth - 1);

            Assert.Throws<InvalidOperationException>(() => TypeName.Parse(tooDeep.AsSpan(), options));
            Assert.False(TypeName.TryParse(tooDeep.AsSpan(), out _, options));

            TypeName parsed = TypeName.Parse(notTooDeep.AsSpan(), options);
            Validate(maxDepth, parsed);

            Assert.True(TypeName.TryParse(notTooDeep.AsSpan(), out parsed, options));
            Validate(maxDepth, parsed);

            static string GetName(int depth)
            {
                // MakeGenericType is not used here, as it crashes for larger depths
                string coreLibName = typeof(object).Assembly.FullName;
                string fullName = typeof(int).AssemblyQualifiedName!;
                for (int i = 0; i < depth; i++)
                {
                    fullName = $"System.Collections.Generic.List`1[[{fullName}]], {coreLibName}";
                }
                return fullName;
            }

            static void Validate(int maxDepth, TypeName parsed)
            {
                for (int i = 0; i < maxDepth - 1; i++)
                {
                    Assert.True(parsed.IsConstructedGenericType);
                    parsed = parsed.GetGenericArguments()[0];
                }
            }
        }

        [Theory]
        [InlineData(10)]
        [InlineData(100)]
        public void MaxNodesIsRespected_TooManyGenericArguments(int maxDepth)
        {
            TypeNameParseOptions options = new()
            {
                MaxNodes = maxDepth
            };

            string tooMany = GetName(maxDepth);
            string notTooMany = GetName(maxDepth - 1);

            Assert.Throws<InvalidOperationException>(() => TypeName.Parse(tooMany.AsSpan(), options));
            Assert.False(TypeName.TryParse(tooMany.AsSpan(), out _, options));

            TypeName parsed = TypeName.Parse(notTooMany.AsSpan(), options);
            Validate(parsed, maxDepth);

            Assert.True(TypeName.TryParse(notTooMany.AsSpan(), out parsed, options));
            Validate(parsed, maxDepth);

            static string GetName(int depth)
                => $"Some.GenericType`{depth}[{string.Join(",", Enumerable.Repeat("System.Int32", depth))}]";

            static void Validate(TypeName parsed, int maxDepth)
            {
                Assert.True(parsed.IsConstructedGenericType);
                ImmutableArray<TypeName> genericArgs = parsed.GetGenericArguments();
                Assert.Equal(maxDepth - 1, genericArgs.Length);
                Assert.All(genericArgs, arg => Assert.False(arg.IsConstructedGenericType));
            }
        }

        public static IEnumerable<object[]> GenericArgumentsAreSupported_Arguments()
        {
            yield return new object[]
            {
                "Generic`1[[A]]",
                "Generic`1",
                "Generic`1[[A]]",
                new string[] { "A" },
                null
            };
            yield return new object[]
            {
                "Generic`1[A]",
                "Generic`1",
                "Generic`1[[A]]",
                new string[] { "A" },
                null
            };
            yield return new object[]
            {
                "Generic`3[[A],[B],[C]]",
                "Generic`3",
                "Generic`3[[A],[B],[C]]",
                new string[] { "A", "B", "C" },
                null
            };
            yield return new object[]
            {
                "Generic`3[A,B,C]",
                "Generic`3",
                "Generic`3[[A],[B],[C]]",
                new string[] { "A", "B", "C" },
                null
            };
            yield return new object[]
            {
                "Generic`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]",
                "Generic`1",
                "Generic`1[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]",
                new string[] { "System.Int32" },
                new AssemblyName[] { new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089") }
            };
            yield return new object[]
            {
                "Generic`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Boolean, mscorlib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]",
                "Generic`2",
                "Generic`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Boolean, mscorlib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]",
                new string[] { "System.Int32", "System.Boolean" },
                new AssemblyName[]
                {
                    new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                    new AssemblyName("mscorlib, Version=5.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
                }
            };
            yield return new object[]
            {
                "Generic`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089], System.Boolean]",
                "Generic`2",
                "Generic`2[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Boolean]]",
                new string[] { "System.Int32", "System.Boolean" },
                new AssemblyName[]
                {
                    new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                    null
                }
            };
            yield return new object[]
            {
                "Generic`2[System.Boolean, [System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]",
                "Generic`2",
                "Generic`2[[System.Boolean],[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]",
                new string[] { "System.Boolean", "System.Int32" },
                new AssemblyName[]
                {
                    null,
                    new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
                }
            };
            yield return new object[]
            {
                "Generic`3[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089], System.Boolean, [System.Byte, other, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]",
                "Generic`3",
                "Generic`3[[System.Int32, mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Boolean],[System.Byte, other, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089]]",
                new string[] { "System.Int32", "System.Boolean", "System.Byte" },
                new AssemblyName[]
                {
                    new AssemblyName("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                    null,
                    new AssemblyName("other, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")
                }
            };
            yield return new object[]
            {
                "Generic`3[System.Boolean, [System.Byte, other, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089], System.Int32]",
                "Generic`3",
                "Generic`3[[System.Boolean],[System.Byte, other, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089],[System.Int32]]",
                new string[] { "System.Boolean", "System.Byte", "System.Int32" },
                new AssemblyName[]
                {
                    null,
                    new AssemblyName("other, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089"),
                    null
                }
            };
        }

        [Theory]
        [MemberData(nameof(GenericArgumentsAreSupported_Arguments))]
        public void GenericArgumentsAreSupported(string input, string name, string fullName, string[] genericTypesFullNames, AssemblyName[]? assemblyNames)
        {
            TypeName parsed = TypeName.Parse(input.AsSpan());

            Assert.Equal(name, parsed.Name);
            Assert.Equal(fullName, parsed.FullName);
            Assert.True(parsed.IsConstructedGenericType);
            Assert.NotNull(parsed.GetGenericTypeDefinition());
            Assert.False(parsed.IsSimple);

            ImmutableArray<TypeName> typeNames = parsed.GetGenericArguments();
            for (int i = 0; i < genericTypesFullNames.Length; i++)
            {
                TypeName genericArg = typeNames[i];
                Assert.Equal(genericTypesFullNames[i], genericArg.FullName);
                Assert.True(genericArg.IsSimple);
                Assert.False(genericArg.IsConstructedGenericType);
                Assert.Throws<InvalidOperationException>(genericArg.GetGenericTypeDefinition);

                if (assemblyNames is not null)
                {
                    if (assemblyNames[i] is null)
                    {
                        Assert.Null(genericArg.AssemblyName);
                    }
                    else
                    {
                        Assert.Equal(assemblyNames[i].FullName, genericArg.AssemblyName.FullName);
                        Assert.Equal(assemblyNames[i].Name, genericArg.AssemblyName.Name);
                    }
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
            Assert.Equal(isSzArray, parsed.IsSZArray);
            if (isArray)
            {
                Assert.Equal(arrayRank, parsed.GetArrayRank());
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => parsed.GetArrayRank());
            }
            Assert.Equal(isByRef, parsed.IsByRef);
            Assert.Equal(isPointer, parsed.IsPointer);
            Assert.False(parsed.IsSimple);

            TypeName elementType = parsed.GetElementType();
            Assert.NotNull(elementType);
            Assert.Equal(typeNameWithoutDecorators, elementType.FullName);
            Assert.True(elementType.IsSimple);
            Assert.False(elementType.IsArray);
            Assert.False(elementType.IsSZArray);
            Assert.False(elementType.IsByRef);
            Assert.False(elementType.IsPointer);
            Assert.Throws<InvalidOperationException>(elementType.GetElementType);
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
        [InlineData(typeof(TypeNameTests), 1)]
        [InlineData(typeof(object), 1)]
        [InlineData(typeof(Assert), 1)] // xunit
        [InlineData(typeof(int[]), 2)]
        [InlineData(typeof(int[,][]), 3)]
        [InlineData(typeof(Nullable<>), 1)] // open generic type treated as elemental
        [InlineData(typeof(NestedNonGeneric_0), 2)] // declaring and nested
        [InlineData(typeof(NestedGeneric_0<int>), 3)] // declaring, nested and generic arg
        [InlineData(typeof(NestedNonGeneric_0.NestedNonGeneric_1), 3)] // declaring, nested 0 and nested 1
        // TypeNameTests+NestedGeneric_0`1+NestedGeneric_1`2[[Int32],[String],[Boolean]] (simplified for brevity)
        [InlineData(typeof(NestedGeneric_0<int>.NestedGeneric_1<string, bool>), 6)] // declaring, nested 0 and nested 1 and 3 generic args
        [MemberData(nameof(GetAdditionalConstructedTypeData))]
        public void GetNodeCountReturnsExpectedValue(Type type, int expected)
        {
            TypeName parsed = TypeName.Parse(type.AssemblyQualifiedName.AsSpan());

            Assert.Equal(expected, parsed.GetNodeCount());

            Assert.Equal(type.Name, parsed.Name);
            Assert.Equal(type.FullName, parsed.FullName);
            Assert.Equal(type.AssemblyQualifiedName, parsed.AssemblyQualifiedName);
        }

        [Fact]
        public void IsSimpleReturnsTrueForNestedNonGenericTypes()
        {
            Assert.True(TypeName.Parse("Containing+Nested".AsSpan()).IsSimple);
            Assert.False(TypeName.Parse(typeof(NestedGeneric_0<int>).FullName.AsSpan()).IsSimple);
        }

        [Fact]
        public void DeclaringTypeThrowsForNonNestedTypes()
        {
            TypeName nested = TypeName.Parse("Containing+Nested".AsSpan());
            Assert.True(nested.IsNested);
            Assert.Equal("Containing", nested.DeclaringType.Name);

            TypeName notNested = TypeName.Parse("NotNested".AsSpan());
            Assert.False(notNested.IsNested);
            Assert.Throws<InvalidOperationException>(() => notNested.DeclaringType);
        }

        [Theory]
        [InlineData("SingleDimensionNonZeroIndexed[*]", true)]
        [InlineData("SingleDimensionZeroIndexed[]", false)]
        [InlineData("MultiDimensional[,,,,,,]", true)]
        public void IsVariableBoundArrayTypeReturnsTrueForNonSZArrays(string typeName, bool expected)
        {
            TypeName parsed = TypeName.Parse(typeName.AsSpan());

            Assert.True(parsed.IsArray);
            Assert.Equal(expected, parsed.IsVariableBoundArrayType);
            Assert.NotEqual(expected, parsed.IsSZArray);
            Assert.InRange(parsed.GetArrayRank(), 1, 32);
        }

        public static IEnumerable<object[]> GetTypesThatRequireEscaping()
        {
            if (PlatformDetection.IsReflectionEmitSupported
                && !PlatformDetection.IsMonoRuntime) // Mono does not escape Type.Name
            {
                AssemblyBuilder assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("TypesThatRequireEscaping"), AssemblyBuilderAccess.Run);
                ModuleBuilder module = assembly.DefineDynamicModule("TypesThatRequireEscapingModule");

                yield return new object[] { module.DefineType("TypeNameWith+ThatIsNotNestedType").CreateType() };
                yield return new object[] { module.DefineType("TypeNameWith\\TheEscapingCharacter").CreateType() };
                yield return new object[] { module.DefineType("TypeNameWith&Ampersand").CreateType() };
                yield return new object[] { module.DefineType("TypeNameWith*Asterisk").CreateType() };
                yield return new object[] { module.DefineType("TypeNameWith[OpeningSquareBracket").CreateType() };
                yield return new object[] { module.DefineType("TypeNameWith]ClosingSquareBracket").CreateType() };
                yield return new object[] { module.DefineType("TypeNameWith[]BothSquareBrackets").CreateType() };
                yield return new object[] { module.DefineType("TypeNameWith[[]]NestedSquareBrackets").CreateType() };
                yield return new object[] { module.DefineType("TypeNameWith,Comma").CreateType() };
                yield return new object[] { module.DefineType("TypeNameWith\\[]+*&,AllSpecialCharacters").CreateType() };

                TypeBuilder containingType = module.DefineType("ContainingTypeWithA+Plus");
                _ = containingType.CreateType(); // containing type must exist!
                yield return new object[] { containingType.DefineNestedType("NoSpecialCharacters").CreateType() };
                yield return new object[] { containingType.DefineNestedType("Contains+Plus").CreateType() };
            }
        }

        [Theory]
        [InlineData(typeof(List<int>))]
        [InlineData(typeof(List<List<int>>))]
        [InlineData(typeof(Dictionary<int, string>))]
        [InlineData(typeof(Dictionary<string, List<string>>))]
        [InlineData(typeof(NestedGeneric_0<int>.NestedGeneric_1<string, bool>))]
        [InlineData(typeof(NestedGeneric_0<int>.NestedGeneric_1<string, bool>.NestedGeneric_2<short, byte, sbyte>))]
        [InlineData(typeof(NestedGeneric_0<int>.NestedGeneric_1<string, bool>.NestedGeneric_2<short, byte, sbyte>.NestedNonGeneric_3))]
        [MemberData(nameof(GetTypesThatRequireEscaping))]
        public void ParsedNamesMatchSystemTypeNames(Type type)
        {
            TypeName parsed = TypeName.Parse(type.AssemblyQualifiedName.AsSpan());

            Assert.Equal(type.Name, parsed.Name);
            Assert.Equal(type.FullName, parsed.FullName);
            Assert.Equal(type.AssemblyQualifiedName, parsed.AssemblyQualifiedName);

            if (type.IsGenericType)
            {
                Type genericType = type.GetGenericTypeDefinition();
                TypeName genericTypeName = parsed.GetGenericTypeDefinition();
                Assert.Equal(genericType.Name, genericTypeName.Name);
                Assert.Equal(genericType.FullName, genericTypeName.FullName);
                Assert.Equal(genericType.AssemblyQualifiedName, genericTypeName.AssemblyQualifiedName);
            }
        }

        [Theory]
        [InlineData("Name`2[[int], [bool]]", "Name`2")] // match
        [InlineData("Name`1[[int], [bool]]", "Name`1")] // less than expected
        [InlineData("Name`3[[int], [bool]]", "Name`3")] // more than expected
        [InlineData("Name[[int], [bool]]", "Name")] // no backtick at all!
        public void TheNumberAfterBacktickDoesNotEnforceGenericArgCount(string input, string expectedName)
        {
            TypeName parsed = TypeName.Parse(input.AsSpan());

            Assert.True(parsed.IsConstructedGenericType);
            Assert.Equal(expectedName, parsed.Name);
            Assert.Equal($"{expectedName}[[int],[bool]]", parsed.FullName);
            Assert.Equal("int", parsed.GetGenericArguments()[0].Name);
            Assert.Equal("bool", parsed.GetGenericArguments()[1].Name);
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
#if NET  // something weird is going on here
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
                if (typeName.IsNested)
                {
                    BindingFlags flagsCopiedFromClr = BindingFlags.NonPublic | BindingFlags.Public;
                    if (ignoreCase)
                    {
                        flagsCopiedFromClr |= BindingFlags.IgnoreCase;
                    }
                    return Make(GetType(typeName.DeclaringType, throwOnError, ignoreCase)?.GetNestedType(typeName.Name, flagsCopiedFromClr));
                }
                else if (typeName.IsConstructedGenericType)
                {
                    return Make(GetType(typeName.GetGenericTypeDefinition(), throwOnError, ignoreCase));
                }
                else if(typeName.IsArray || typeName.IsPointer || typeName.IsByRef)
                {
                    return Make(GetType(typeName.GetElementType(), throwOnError, ignoreCase));
                }
                else
                {
                    Assert.True(typeName.IsSimple);

                    AssemblyName? assemblyName = typeName.AssemblyName.ToAssemblyName();
                    Type? type = assemblyName is null
                        ? Type.GetType(typeName.FullName, throwOnError, ignoreCase)
                        : Assembly.Load(assemblyName).GetType(typeName.FullName, throwOnError, ignoreCase);

                    return Make(type);
                }

                Type? Make(Type? type)
                {
                    if (type is null || typeName.IsSimple)
                    {
                        return type;
                    }
                    else if (typeName.IsConstructedGenericType)
                    {
                        ImmutableArray<TypeName> genericArgs = typeName.GetGenericArguments();
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
                    else if (typeName.IsByRef)
                    {
                        return type.MakeByRefType();
                    }
                    else if (typeName.IsPointer)
                    {
                        return type.MakePointerType();
                    }
                    else if (typeName.IsSZArray)
                    {
                        return type.MakeArrayType();
                    }
                    else
                    {
                        Assert.True(typeName.IsVariableBoundArrayType);

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
    }
}
