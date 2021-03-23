// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class TypeNameParsingTests
    {
        TestTypeSystemContext _context;
        ModuleDesc _testModule;

        string _coreAssemblyQualifier;

        MetadataType _simpleType;
        MetadataType _nestedType;
        MetadataType _nestedTwiceType;

        MetadataType _genericType;
        MetadataType _nestedNongenericType;
        MetadataType _nestedGenericType;

        MetadataType _veryGenericType;

        MetadataType _structType;

        public TypeNameParsingTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.X64);

            // TODO-NICE: split test types into a separate, non-core, module
            _testModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(_testModule);

            _simpleType = _testModule.GetType("TypeNameParsing", "Simple");
            _nestedType = _simpleType.GetNestedType("Nested");
            _nestedTwiceType = _nestedType.GetNestedType("NestedTwice");

            _genericType = _testModule.GetType("TypeNameParsing", "Generic`1");
            _nestedGenericType = _genericType.GetNestedType("NestedGeneric`1");
            _nestedNongenericType = _genericType.GetNestedType("NestedNongeneric");

            _veryGenericType = _testModule.GetType("TypeNameParsing", "VeryGeneric`3");

            _structType = _testModule.GetType("TypeNameParsing", "Struct");

            _coreAssemblyQualifier = ((IAssemblyDesc)_testModule).GetName().FullName;
        }

        [Fact]
        public void TestSimpleNames()
        {
            {
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Simple");
                Assert.Equal(_simpleType, result);
            }

            {
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Simple+Nested");
                Assert.Equal(_nestedType, result);
            }

            {
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Simple+Nested+NestedTwice");
                Assert.Equal(_nestedTwiceType, result);
            }

            {
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("System.Int32, " + _coreAssemblyQualifier);
                Assert.Equal(_context.GetWellKnownType(WellKnownType.Int32), result);
            }

            {
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.VeryGeneric`3");
                Assert.Equal(_veryGenericType, result);
            }
        }

        [Fact]
        public void TestArrayTypes()
        {
            {
                TypeDesc expected = _simpleType.MakeArrayType();
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Simple[]");
                Assert.Equal(expected, result);
            }

            {
                TypeDesc expected = _simpleType.MakeArrayType().MakeArrayType().MakeArrayType();
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Simple[][][]");
                Assert.Equal(expected, result);
            }

            {
                TypeDesc expected = _simpleType.MakeArrayType(2).MakeArrayType(3);
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Simple[,][,,]");
                Assert.Equal(expected, result);
            }

            {
                TypeDesc expected = _context.GetWellKnownType(WellKnownType.Int32).MakeArrayType();
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("System.Int32[], " + _coreAssemblyQualifier);
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public void TestPointerTypes()
        {
            {
                TypeDesc expected = _structType.MakePointerType();
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Struct*");
                Assert.Equal(expected, result);
            }

            {
                TypeDesc expected = _structType.MakePointerType().MakePointerType();
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Struct**");
                Assert.Equal(expected, result);
            }

            {
                TypeDesc expected = _context.GetWellKnownType(WellKnownType.Int32).MakePointerType();
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("System.Int32*, " + _coreAssemblyQualifier);
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public void TestInstantiatedTypes()
        {
            var nullableType = (MetadataType)_context.GetWellKnownType(WellKnownType.Nullable);

            {
                TypeDesc expected = _genericType.MakeInstantiatedType(_simpleType);
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Generic`1[TypeNameParsing.Simple]");
                Assert.Equal(expected, result);
            }

            {
                TypeDesc expected = _veryGenericType.MakeInstantiatedType(
                    _simpleType,
                    _genericType.MakeInstantiatedType(_simpleType),
                    _structType
                );
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.VeryGeneric`3[TypeNameParsing.Simple,TypeNameParsing.Generic`1[TypeNameParsing.Simple],TypeNameParsing.Struct]");
                Assert.Equal(expected, result);
            }

            {
                TypeDesc expected = _genericType.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Object));
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Generic`1[[System.Object, " + _coreAssemblyQualifier + "]]");
                Assert.Equal(expected, result);
            }

            {
                TypeDesc expected = _veryGenericType.MakeInstantiatedType(
                    _context.GetWellKnownType(WellKnownType.Object),
                    _simpleType,
                    _context.GetWellKnownType(WellKnownType.Int32)
                );
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName(String.Format(
                    "TypeNameParsing.VeryGeneric`3[[System.Object, {0}],TypeNameParsing.Simple,[System.Int32, {0}]]", _coreAssemblyQualifier));
                Assert.Equal(expected, result);
            }

            {
                TypeDesc expected = nullableType.MakeInstantiatedType(_structType);
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName(String.Format(
                    "System.Nullable`1[TypeNameParsing.Struct], {0}", _coreAssemblyQualifier));
                Assert.Equal(expected, result);
            }

            {
                TypeDesc expected = nullableType.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32));
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName(String.Format(
                    "System.Nullable`1[[System.Int32, {0}]], {0}", _coreAssemblyQualifier));
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public void TestMixed()
        {
            var nullableType = (MetadataType)_context.GetWellKnownType(WellKnownType.Nullable);

            {
                TypeDesc expected = _genericType.MakeInstantiatedType(_structType.MakePointerType().MakeArrayType());
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Generic`1[TypeNameParsing.Struct*[]]");
                Assert.Equal(expected, result);
            }

            {
                TypeDesc expected = _genericType.MakeInstantiatedType(_structType.MakePointerType().MakePointerType().MakeArrayType().MakeArrayType(2));
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Generic`1[TypeNameParsing.Struct**[][,]]");
                Assert.Equal(expected, result);
            }

            {
                TypeDesc expected = _nestedNongenericType.MakeInstantiatedType(
                    nullableType.MakeInstantiatedType(_structType)
                );
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName(String.Format(
                    "TypeNameParsing.Generic`1+NestedNongeneric[[System.Nullable`1[TypeNameParsing.Struct], {0}]]", _coreAssemblyQualifier));
                Assert.Equal(expected, result);
            }

            {
                TypeDesc expected = _nestedGenericType.MakeInstantiatedType(
                    nullableType.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32)),
                    _nestedType.MakeArrayType()
                );
                TypeDesc result = _testModule.GetTypeByCustomAttributeTypeName(String.Format(
                    "TypeNameParsing.Generic`1+NestedGeneric`1[[System.Nullable`1[[System.Int32, {0}]], {0}],TypeNameParsing.Simple+Nested[]]", _coreAssemblyQualifier));
                Assert.Equal(expected, result);
            }
        }

        [Fact]
        public void TestFailureWhenTypeIsMissing()
        {
            // Test throwing behavior
            Assert.Throws<TypeSystemException.TypeLoadException>(() => _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.SimpleButNotThere"));
            Assert.Throws<TypeSystemException.TypeLoadException>(() => _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.SimpleButNotThere+NonNamespaceQualifiedType"));
            Assert.Throws<TypeSystemException.TypeLoadException>(() => _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Simple+NestedNotThere"));
            Assert.Throws<TypeSystemException.TypeLoadException>(() => _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Simple+Nested+NestedTwiceNotThere"));
            Assert.Throws<TypeSystemException.TypeLoadException>(() => _testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Generic`1[TypeNameParsing.SimpleButNotThere]"));

            // Test returning null behavior
            Assert.Null(_testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.SimpleButNotThere", throwIfNotFound: false));
            Assert.Null(_testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.SimpleButNotThere+NonNamespaceQualifiedType", throwIfNotFound: false));
            Assert.Null(_testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Simple+NestedNotThere", throwIfNotFound: false));
            Assert.Null(_testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Simple+NestedNotThere+NonNamespaceQualifiedType", throwIfNotFound: false));
            Assert.Null(_testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Simple+Nested+NestedTwiceNotThere", throwIfNotFound: false));
            Assert.Null(_testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Simple+Nested+NonNamespaceQualifiedType", throwIfNotFound: false));
            Assert.Null(_testModule.GetTypeByCustomAttributeTypeName("TypeNameParsing.Generic`1[TypeNameParsing.SimpleButNotThere]", throwIfNotFound: false));
        }

        public IEnumerable<TypeDesc> GetTypesForRoundtripTest()
        {
            yield return _simpleType;
            yield return _nestedType;
            yield return _nestedTwiceType;
            yield return _context.GetWellKnownType(WellKnownType.Int32);
            yield return _veryGenericType;
            yield return _simpleType.MakeArrayType();
            yield return _simpleType.MakeArrayType().MakeArrayType();
            yield return _simpleType.MakeArrayType(2).MakeArrayType(3);
            yield return _context.GetWellKnownType(WellKnownType.Int32).MakeArrayType();
            yield return _structType.MakePointerType();
            yield return _context.GetWellKnownType(WellKnownType.Int32).MakePointerType().MakePointerType();
            yield return _genericType.MakeInstantiatedType(_simpleType);
            yield return _veryGenericType.MakeInstantiatedType(
                    _simpleType,
                    _genericType.MakeInstantiatedType(_simpleType),
                    _structType
                );
            yield return _genericType.MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Object));
            yield return _veryGenericType.MakeInstantiatedType(
                    _context.GetWellKnownType(WellKnownType.Object),
                    _simpleType,
                    _context.GetWellKnownType(WellKnownType.Int32)
                );
            yield return ((MetadataType)_context.GetWellKnownType(WellKnownType.Nullable)).MakeInstantiatedType(_structType);
            yield return _genericType.MakeInstantiatedType(_structType.MakePointerType().MakeArrayType());
            yield return _nestedGenericType.MakeInstantiatedType(
                    ((MetadataType)_context.GetWellKnownType(WellKnownType.Nullable)).MakeInstantiatedType(_context.GetWellKnownType(WellKnownType.Int32)),
                    _nestedType.MakeArrayType()
                );
        }

        [Fact]
        public void TestRoundtripping()
        {
            foreach (TypeDesc type in GetTypesForRoundtripTest())
            {
                {
                    var fmt = new CustomAttributeTypeNameFormatter((IAssemblyDesc)_testModule);
                    string formatted = fmt.FormatName(type, true);
                    TypeDesc roundTripped = _testModule.GetTypeByCustomAttributeTypeName(formatted);
                    Assert.Equal(type, roundTripped);
                }

                {
                    var fmt = new CustomAttributeTypeNameFormatter();
                    string formatted = fmt.FormatName(type, true);
                    TypeDesc roundTripped = _testModule.GetTypeByCustomAttributeTypeName(formatted);
                    Assert.Equal(type, roundTripped);
                }
            }
        }
    }
}
