// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

using Internal.TypeSystem;
using Internal.NativeFormat;

using Xunit;

namespace TypeSystemTests
{
    public class HashcodeTests
    {
        private TestTypeSystemContext _context;
        private ModuleDesc _testModule;

        public HashcodeTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = systemModule;
        }

        [Fact]
        public void TestMultidimensionalArrays()
        {
            TypeDesc objectType = _context.GetWellKnownType(WellKnownType.Object);

            ArrayType objectMDArrayRank1 = _context.GetArrayType(objectType, 1);
            ArrayType objectMDArrayRank2 = _context.GetArrayType(objectType, 2);
            ArrayType objectMDArrayRank3 = _context.GetArrayType(objectType, 3);

            Assert.Equal(TypeHashingAlgorithms.ComputeArrayTypeHashCode(objectType.GetHashCode(), 1), objectMDArrayRank1.GetHashCode());
            Assert.Equal(TypeHashingAlgorithms.ComputeArrayTypeHashCode(objectType.GetHashCode(), 2), objectMDArrayRank2.GetHashCode());
            Assert.Equal(TypeHashingAlgorithms.ComputeArrayTypeHashCode(objectType.GetHashCode(), 3), objectMDArrayRank3.GetHashCode());
        }

        [Fact]
        public void TestSingleDimensionalArrays()
        {
            TypeDesc objectType = _context.GetWellKnownType(WellKnownType.Object);

            ArrayType objectArray = _context.GetArrayType(objectType);

            Assert.Equal(TypeHashingAlgorithms.ComputeArrayTypeHashCode(objectType.GetHashCode(), -1), objectArray.GetHashCode());
        }

        [Fact]
        public void TestNonGenericTypes()
        {
            MetadataType nonNestedType = (MetadataType)_testModule.GetType("Hashcode", "NonNestedType");
            TypeDesc nestedType = nonNestedType.GetNestedType("NestedType");

            int expectedNonNestedTypeHashcode = TypeHashingAlgorithms.ComputeNameHashCode("Hashcode.NonNestedType");
            int expectedNestedTypeNameHashcode = TypeHashingAlgorithms.ComputeNameHashCode("NestedType");
            int expectedNestedTypeHashcode = TypeHashingAlgorithms.ComputeNestedTypeHashCode(expectedNonNestedTypeHashcode, expectedNestedTypeNameHashcode);

            Assert.Equal(expectedNonNestedTypeHashcode, nonNestedType.GetHashCode());
            Assert.Equal(expectedNestedTypeHashcode, nestedType.GetHashCode());
        }

        [Fact]
        public void TestGenericTypes()
        {
            MetadataType ilistType = (MetadataType)_testModule.GetType("System.Collections.Generic", "IList`1");
            DefType systemArrayType = _context.GetWellKnownType(WellKnownType.Array);
            DefType ilistOfSystemArray = ilistType.MakeInstantiatedType(systemArrayType);

            int expectedIListOfTHashcode = TypeHashingAlgorithms.ComputeNameHashCode("System.Collections.Generic.IList`1");
            int expectedSystemArrayHashcode = TypeHashingAlgorithms.ComputeNameHashCode("System.Array");
            Assert.Equal(expectedIListOfTHashcode, ilistType.GetHashCode());
            Assert.Equal(TypeHashingAlgorithms.ComputeGenericInstanceHashCode(expectedIListOfTHashcode, new int[] { expectedSystemArrayHashcode }), ilistOfSystemArray.GetHashCode());
        }

        [Fact]
        public void TestInstantiatedMethods()
        {
            MetadataType nonNestedType = (MetadataType)_testModule.GetType("Hashcode", "NonNestedType");
            MetadataType genericType = (MetadataType)_testModule.GetType("Hashcode", "GenericType`2");
            DefType intType = _context.GetWellKnownType(WellKnownType.Int32);
            DefType stringType = _context.GetWellKnownType(WellKnownType.String);

            MetadataType genericTypeOfIntString = genericType.MakeInstantiatedType(intType, stringType);
            MetadataType genericTypeOfStringInt = genericType.MakeInstantiatedType(stringType, intType);

            // build up expected hash codes for the above
            int expHashNonNestedType = TypeHashingAlgorithms.ComputeNameHashCode("Hashcode.NonNestedType");
            Assert.Equal(expHashNonNestedType, nonNestedType.GetHashCode());
            int expHashGenType = TypeHashingAlgorithms.ComputeNameHashCode("Hashcode.GenericType`2");
            Assert.Equal(expHashGenType, genericType.GetHashCode());
            int expHashInt = TypeHashingAlgorithms.ComputeNameHashCode("System.Int32");
            Assert.Equal(expHashInt, intType.GetHashCode());
            int expHashString = TypeHashingAlgorithms.ComputeNameHashCode("System.String");
            Assert.Equal(expHashString, stringType.GetHashCode());
            int expHashGenTypeOfIS = TypeHashingAlgorithms.ComputeGenericInstanceHashCode(expHashGenType, new int[] { expHashInt, expHashString });
            Assert.Equal(expHashGenTypeOfIS, genericTypeOfIntString.GetHashCode());
            int expHashGenTypeOfSI = TypeHashingAlgorithms.ComputeGenericInstanceHashCode(expHashGenType, new int[] { expHashString, expHashInt });
            Assert.Equal(expHashGenTypeOfSI, genericTypeOfStringInt.GetHashCode());

            // Test that instantiated method's have the right hashes

            int genMethodNameHash = TypeHashingAlgorithms.ComputeNameHashCode("GenericMethod");
            int genMethodNameAndIHash = TypeHashingAlgorithms.ComputeGenericInstanceHashCode(genMethodNameHash, new int[] { expHashInt });
            int genMethodNameAndSHash = TypeHashingAlgorithms.ComputeGenericInstanceHashCode(genMethodNameHash, new int[] { expHashString });


            Action<MetadataType, int> testSequence = (MetadataType typeWithGenericMethod, int expectedTypeHash) =>
            {
                // Uninstantiated Generic method
                MethodDesc genMethod = typeWithGenericMethod.GetMethod("GenericMethod", null);
                Assert.Equal(TypeHashingAlgorithms.ComputeMethodHashCode(expectedTypeHash, genMethodNameHash), genMethod.GetHashCode());

                // Instantiated over int
                MethodDesc genMethodI = genMethod.MakeInstantiatedMethod(intType);
                Assert.Equal(TypeHashingAlgorithms.ComputeMethodHashCode(expectedTypeHash, genMethodNameAndIHash), genMethodI.GetHashCode());

                // Instantiated over string
                MethodDesc genMethodS = genMethod.MakeInstantiatedMethod(stringType);
                Assert.Equal(TypeHashingAlgorithms.ComputeMethodHashCode(expectedTypeHash, genMethodNameAndSHash), genMethodS.GetHashCode());

                // Assert they aren't the same as the other hashes
                Assert.NotEqual(genMethodI.GetHashCode(), genMethodS.GetHashCode());
                Assert.NotEqual(genMethodI.GetHashCode(), genMethod.GetHashCode());
                Assert.NotEqual(genMethodS.GetHashCode(), genMethod.GetHashCode());
            };

            // Test cases on non-generic type
            testSequence(nonNestedType, expHashNonNestedType);

            // Test cases on generic type
            testSequence(genericType, expHashGenType);

            // Test cases on instantiated generic type
            testSequence(genericTypeOfIntString, expHashGenTypeOfIS);
            testSequence(genericTypeOfStringInt, expHashGenTypeOfSI);
        }

        [Fact]
        public void TestPointerTypes()
        {
            DefType intType = _context.GetWellKnownType(WellKnownType.Int32);

            int expHashInt = TypeHashingAlgorithms.ComputeNameHashCode("System.Int32");
            Assert.Equal(expHashInt, intType.GetHashCode());

            int expHashIntPointer = TypeHashingAlgorithms.ComputePointerTypeHashCode(expHashInt);
            TypeDesc intPointerType = _context.GetPointerType(intType);
            Assert.Equal(expHashIntPointer, intPointerType.GetHashCode());
        }

        [Fact]
        public void TestFunctionPointerTypes()
        {
            DefType intType = _context.GetWellKnownType(WellKnownType.Int32);
            DefType objectType = _context.GetWellKnownType(WellKnownType.Object);

            int expHashInt = TypeHashingAlgorithms.ComputeNameHashCode("System.Int32");
            int expHashObject = TypeHashingAlgorithms.ComputeNameHashCode("System.Object");

            int expHashFnPtr = TypeHashingAlgorithms.ComputeMethodSignatureHashCode(expHashInt, new[] { expHashObject });

            MethodSignature fnPtrSig = new MethodSignature(MethodSignatureFlags.None, 0, intType, new TypeDesc[] { objectType });
            var fnPtrType = _context.GetFunctionPointerType(fnPtrSig);
            Assert.Equal(expHashFnPtr, fnPtrType.GetHashCode());
        }

        [Fact]
        public void TestByRefTypes()
        {
            DefType intType = _context.GetWellKnownType(WellKnownType.Int32);

            int expHashInt = TypeHashingAlgorithms.ComputeNameHashCode("System.Int32");
            Assert.Equal(expHashInt, intType.GetHashCode());

            int expHashIntByRef = TypeHashingAlgorithms.ComputeByrefTypeHashCode(expHashInt);
            TypeDesc intByRefType = _context.GetByRefType(intType);
            Assert.Equal(expHashIntByRef, intByRefType.GetHashCode());
        }

        [Fact]
        public void TestHashCodeBuilder()
        {
            {
                var builder = new TypeHashingAlgorithms.HashCodeBuilder("Xy");
                Assert.Equal(TypeHashingAlgorithms.ComputeNameHashCode("Xy"), builder.ToHashCode());
            }

            {
                var builder = new TypeHashingAlgorithms.HashCodeBuilder("Xyz");
                Assert.Equal(TypeHashingAlgorithms.ComputeNameHashCode("Xyz"), builder.ToHashCode());
            }

            {
                var builder = new TypeHashingAlgorithms.HashCodeBuilder("Xy");
                builder.Append("");
                Assert.Equal(TypeHashingAlgorithms.ComputeNameHashCode("Xy"), builder.ToHashCode());
            }

            {
                var builder = new TypeHashingAlgorithms.HashCodeBuilder("Xy");
                builder.Append(".");
                Assert.Equal(TypeHashingAlgorithms.ComputeNameHashCode("Xy."), builder.ToHashCode());
            }

            {
                var builder = new TypeHashingAlgorithms.HashCodeBuilder("Xyz");
                builder.Append(".");
                Assert.Equal(TypeHashingAlgorithms.ComputeNameHashCode("Xyz."), builder.ToHashCode());
            }

            {
                var builder = new TypeHashingAlgorithms.HashCodeBuilder("Xy");
                builder.Append("..");
                Assert.Equal(TypeHashingAlgorithms.ComputeNameHashCode("Xy.."), builder.ToHashCode());
            }

            {
                var builder = new TypeHashingAlgorithms.HashCodeBuilder("Xyz");
                builder.Append("..");
                Assert.Equal(TypeHashingAlgorithms.ComputeNameHashCode("Xyz.."), builder.ToHashCode());
            }

            {
                var builder = new TypeHashingAlgorithms.HashCodeBuilder("Xy");
                builder.Append(".");
                builder.Append("Ab");
                Assert.Equal(TypeHashingAlgorithms.ComputeNameHashCode("Xy.Ab"), builder.ToHashCode());
            }

            {
                var builder = new TypeHashingAlgorithms.HashCodeBuilder("Xy");
                builder.Append(".");
                builder.Append("Abc");
                Assert.Equal(TypeHashingAlgorithms.ComputeNameHashCode("Xy.Abc"), builder.ToHashCode());
            }

            {
                var builder = new TypeHashingAlgorithms.HashCodeBuilder("Xyz");
                builder.Append(".");
                builder.Append("Abc");
                Assert.Equal(TypeHashingAlgorithms.ComputeNameHashCode("Xyz.Abc"), builder.ToHashCode());
            }
        }
    }
}
