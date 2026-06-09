// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Text;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;
using Xunit.Abstractions;

namespace TypeSystemTests
{
    public class SignatureTests
    {
        private readonly ITestOutputHelper _output;
        private TestTypeSystemContext _context;
        private ModuleDesc _testModule;

        private const byte ElementTypeValueType = 0x11;
        private const byte ElementTypeClass = 0x12;

        public SignatureTests(ITestOutputHelper output)
        {
            _output = output;
            _context = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = _context.GetModuleForSimpleName("ILTestAssembly");
        }

        private static string GetModOptMethodSignatureInfo(MethodSignature signature)
        {
            if (!signature.HasEmbeddedSignatureData)
                return string.Empty;

            StringBuilder sb = new StringBuilder();
            foreach (EmbeddedSignatureData data in signature.GetEmbeddedSignatureData())
            {
                sb.Append(data.kind.ToString());
                sb.Append(data.index);
                if (data.type != null)
                    sb.Append(((MetadataType)data.type).GetName());
                else
                    sb.Append("<null>");
            }
            return sb.ToString();
        }

        private static EcmaModule CreateModuleWithTypeSpecification(Action<MetadataBuilder, BlobBuilder> buildSignature)
        {
            // Create a minimal in-memory assembly with exactly one TypeSpec row.
            MetadataBuilder metadataBuilder = new MetadataBuilder();
            StringHandle assemblyName = metadataBuilder.GetOrAddString("TypeSpecTest");

            metadataBuilder.AddModule(0, metadataBuilder.GetOrAddString("TypeSpecTest.dll"), metadataBuilder.GetOrAddGuid(new Guid("F3C03C57-397E-4A33-B670-CB4EE2C88AF7")), default(GuidHandle), default(GuidHandle));
            metadataBuilder.AddAssembly(assemblyName, new Version(1, 0, 0, 0), default(StringHandle), default(BlobHandle), default(AssemblyFlags), AssemblyHashAlgorithm.None);
            metadataBuilder.AddTypeDefinition(
                default(TypeAttributes),
                default(StringHandle),
                metadataBuilder.GetOrAddString("<Module>"),
                default(EntityHandle),
                MetadataTokens.FieldDefinitionHandle(1),
                MetadataTokens.MethodDefinitionHandle(1));

            BlobBuilder signature = new BlobBuilder();
            buildSignature(metadataBuilder, signature);
            metadataBuilder.AddTypeSpecification(metadataBuilder.GetOrAddBlob(signature));

            BlobBuilder peBlob = new BlobBuilder();
            ManagedPEBuilder peBuilder = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadataBuilder), new BlobBuilder());
            peBuilder.Serialize(peBlob);

            MemoryStream peStream = new MemoryStream();
            peBlob.WriteContentTo(peStream);
            peStream.Position = 0;

            TestTypeSystemContext context = new TestTypeSystemContext(TargetArchitecture.X64);
            ModuleDesc systemModule = context.CreateModuleForSimpleName("CoreTestAssembly");
            context.SetSystemModule(systemModule);
            return (EcmaModule)context.CreateModuleForSimpleName("TypeSpecTest", peStream);
        }

        private static AssemblyReferenceHandle AddCoreTestAssemblyReference(MetadataBuilder metadataBuilder)
        {
            return metadataBuilder.AddAssemblyReference(
                metadataBuilder.GetOrAddString("CoreTestAssembly"),
                new Version(0, 0, 0, 0),
                default(StringHandle),
                default(BlobHandle),
                default(AssemblyFlags),
                default(BlobHandle));
        }

        private static TypeReferenceHandle AddCoreTestAssemblyTypeReference(MetadataBuilder metadataBuilder, string ns, string name)
        {
            AssemblyReferenceHandle coreTestAssembly = AddCoreTestAssemblyReference(metadataBuilder);
            return metadataBuilder.AddTypeReference(coreTestAssembly, metadataBuilder.GetOrAddString(ns), metadataBuilder.GetOrAddString(name));
        }

        private static void WriteTypeDefOrRefEncoded(BlobBuilder signature, EntityHandle handle)
        {
            int tag = handle.Kind switch
            {
                HandleKind.TypeDefinition => 0,
                HandleKind.TypeReference => 1,
                HandleKind.TypeSpecification => 2,
                _ => throw new BadImageFormatException()
            };

            signature.WriteCompressedInteger((MetadataTokens.GetRowNumber(handle) << 2) | tag);
        }

        [Theory]
        [InlineData(SignatureTypeCode.Void)]
        [InlineData(SignatureTypeCode.Boolean)]
        [InlineData(SignatureTypeCode.Char)]
        [InlineData(SignatureTypeCode.SByte)]
        [InlineData(SignatureTypeCode.Byte)]
        [InlineData(SignatureTypeCode.Int16)]
        [InlineData(SignatureTypeCode.UInt16)]
        [InlineData(SignatureTypeCode.Int32)]
        [InlineData(SignatureTypeCode.UInt32)]
        [InlineData(SignatureTypeCode.Int64)]
        [InlineData(SignatureTypeCode.UInt64)]
        [InlineData(SignatureTypeCode.Single)]
        [InlineData(SignatureTypeCode.Double)]
        [InlineData(SignatureTypeCode.String)]
        [InlineData(SignatureTypeCode.ByReference)]
        [InlineData(SignatureTypeCode.TypedReference)]
        [InlineData(SignatureTypeCode.IntPtr)]
        [InlineData(SignatureTypeCode.UIntPtr)]
        [InlineData(SignatureTypeCode.Object)]
        public void TestInvalidTopLevelTypeSpecification(SignatureTypeCode typeCode)
        {
            // Primitive and special element types cannot be used as
            // the top-level signature in a TypeSpec row.
            EcmaModule module = CreateModuleWithTypeSpecification((_, signature) =>
            {
                signature.WriteByte((byte)typeCode);
                if (typeCode == SignatureTypeCode.ByReference)
                    signature.WriteByte((byte)SignatureTypeCode.Int32);
            });

            Assert.Throws<TypeSystemException.BadImageFormatException>(() => module.GetType(MetadataTokens.TypeSpecificationHandle(1)));
        }

        [Theory]
        [InlineData(ElementTypeClass, "Object")]
        [InlineData(ElementTypeValueType, "Int32")]
        public void TestInvalidTopLevelTypeHandleTypeSpecification(byte elementType, string typeName)
        {
            // CLASS and VALUETYPE cannot be used as tokens in a top-level TypeSpec signature.
            EcmaModule module = CreateModuleWithTypeSpecification((metadataBuilder, signature) =>
            {
                TypeReferenceHandle typeRef = AddCoreTestAssemblyTypeReference(metadataBuilder, "System", typeName);
                signature.WriteByte(elementType);
                WriteTypeDefOrRefEncoded(signature, typeRef);
            });

            Assert.Throws<TypeSystemException.BadImageFormatException>(() => module.GetType(MetadataTokens.TypeSpecificationHandle(1)));
        }

        [Theory]
        [InlineData(SignatureTypeCode.GenericTypeParameter)]
        [InlineData(SignatureTypeCode.GenericMethodParameter)]
        public void TestTopLevelGenericVariableTypeSpecificationResolves(SignatureTypeCode typeCode)
        {
            EcmaModule module = CreateModuleWithTypeSpecification((_, signature) =>
            {
                signature.WriteByte((byte)typeCode);
                signature.WriteCompressedInteger(0);
            });

            Assert.NotNull(module.GetType(MetadataTokens.TypeSpecificationHandle(1)));
        }

        [Theory]
        [InlineData(SignatureTypeCode.Pointer)]
        [InlineData(SignatureTypeCode.FunctionPointer)]
        [InlineData(SignatureTypeCode.Array)]
        [InlineData(SignatureTypeCode.SZArray)]
        [InlineData(SignatureTypeCode.GenericTypeInstance)]
        public void TestValidTopLevelTypeSpecification(SignatureTypeCode typeCode)
        {
            EcmaModule module = CreateModuleWithTypeSpecification((metadataBuilder, signature) =>
            {
                signature.WriteByte((byte)typeCode);

                // Use the smallest valid payload for each root so this test only exercises
                // top-level TypeSpec validation.
                switch (typeCode)
                {
                    case SignatureTypeCode.Pointer:
                        signature.WriteByte((byte)SignatureTypeCode.Void);
                        break;

                    case SignatureTypeCode.FunctionPointer:
                        signature.WriteByte((byte)SignatureCallingConvention.Default);
                        signature.WriteCompressedInteger(0);
                        signature.WriteByte((byte)SignatureTypeCode.Void);
                        break;

                    case SignatureTypeCode.Array:
                        signature.WriteByte((byte)SignatureTypeCode.Object);
                        signature.WriteCompressedInteger(1);
                        signature.WriteCompressedInteger(0);
                        signature.WriteCompressedInteger(0);
                        break;

                    case SignatureTypeCode.SZArray:
                        signature.WriteByte((byte)SignatureTypeCode.Object);
                        break;

                    case SignatureTypeCode.GenericTypeInstance:
                        TypeReferenceHandle genericTypeRef = AddCoreTestAssemblyTypeReference(metadataBuilder, "GenericTypes", "GenericClass`1");
                        signature.WriteByte(ElementTypeClass);
                        WriteTypeDefOrRefEncoded(signature, genericTypeRef);
                        signature.WriteCompressedInteger(1);
                        signature.WriteByte((byte)SignatureTypeCode.Object);
                        break;
                }
            });

            Assert.NotNull(module.GetType(MetadataTokens.TypeSpecificationHandle(1)));
        }

        [Fact]
        public void TestSignatureMatches2ModOptsAtStartOfSig()
        {
            MetadataType modOptTester = _testModule.GetType(""u8, "ModOptTester"u8);
            MethodSignature methodWith2ModOptsAtStartOfSig = modOptTester.GetMethods().Single(m => string.Equals(m.GetName(), "Method")).Signature;

            // All modopts that are at the very beginning of the signature are given index 0.1.1.1
            // Both the index and the order in the modopt array are significant for signature comparison
            Assert.Equal(MethodSignature.IndexOfCustomModifiersOnReturnType, methodWith2ModOptsAtStartOfSig.GetEmbeddedSignatureData()[0].index);
            Assert.Equal(MethodSignature.IndexOfCustomModifiersOnReturnType, methodWith2ModOptsAtStartOfSig.GetEmbeddedSignatureData()[1].index);
            Assert.NotEqual(MethodSignature.IndexOfCustomModifiersOnReturnType, methodWith2ModOptsAtStartOfSig.GetEmbeddedSignatureData()[2].index);
            Assert.Equal("OptionalCustomModifier0.1.1.1CharOptionalCustomModifier0.1.1.1VoidOptionalCustomModifier0.1.2.1FooModifier", GetModOptMethodSignatureInfo(methodWith2ModOptsAtStartOfSig));
        }

        [Fact]
        public void TestSignatureMatchesModOptAtStartOfSigAndAfterByRef()
        {
            MetadataType modOptTester = _testModule.GetType(""u8, "ModOptTester"u8);
            MethodSignature methodWithModOptAtStartOfSigAndAfterByRef = modOptTester.GetMethods().Single(m => string.Equals(m.GetName(), "Method2")).Signature;

            // A modopts after an E_T_BYREF will look like 0.1.1.2.1.1
            Assert.Equal(MethodSignature.IndexOfCustomModifiersOnReturnType, methodWithModOptAtStartOfSigAndAfterByRef.GetEmbeddedSignatureData()[0].index);
            Assert.NotEqual(MethodSignature.IndexOfCustomModifiersOnReturnType, methodWithModOptAtStartOfSigAndAfterByRef.GetEmbeddedSignatureData()[1].index);
            Assert.NotEqual(MethodSignature.IndexOfCustomModifiersOnReturnType, methodWithModOptAtStartOfSigAndAfterByRef.GetEmbeddedSignatureData()[2].index);
            Assert.Equal("OptionalCustomModifier0.1.1.1CharOptionalCustomModifier0.1.1.2.1.1VoidOptionalCustomModifier0.1.2.1FooModifier", GetModOptMethodSignatureInfo(methodWithModOptAtStartOfSigAndAfterByRef));
        }

        [Fact]
        public void TestSignatureMatchesModoptOnPointerOrRefModifiedType()
        {
            MetadataType modOptTester = _testModule.GetType(""u8, "ModOptTester"u8);
            MethodSignature methodWithModOpt = modOptTester.GetMethods().Single(m => string.Equals(m.GetName(), "Method3")).Signature;
            Assert.Equal(MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(0), methodWithModOpt.GetEmbeddedSignatureData()[0].index);
            Assert.Equal(MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(1), methodWithModOpt.GetEmbeddedSignatureData()[1].index);
            Assert.Equal(MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(2), methodWithModOpt.GetEmbeddedSignatureData()[2].index);
        }

        [Fact]
        public void TestSignatureMatchesForArrayShapeDetails()
        {
            MetadataType modOptTester = _testModule.GetType(""u8, "ModOptTester"u8);
            MethodSignature methodWithModOpt = modOptTester.GetMethods().Single(m => string.Equals(m.GetName(), "Method4")).Signature;

            _output.WriteLine($"Found ModOptData '{GetModOptMethodSignatureInfo(methodWithModOpt)}'");
            Assert.Equal(7, methodWithModOpt.GetEmbeddedSignatureData().Length);
            Assert.Equal(MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(0), methodWithModOpt.GetEmbeddedSignatureData()[0].index);
            Assert.Equal(MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(1), methodWithModOpt.GetEmbeddedSignatureData()[2].index);
            Assert.Equal(MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(2), methodWithModOpt.GetEmbeddedSignatureData()[4].index);
            Assert.Equal("OptionalCustomModifier0.1.1.2.1.1VoidArrayShape1.2.1.1|3,4|0,1<null>OptionalCustomModifier0.1.1.2.2.1FooModifierArrayShape1.2.2.1|0,9|2,0<null>OptionalCustomModifier0.1.1.2.3.1FooModifierArrayShape1.2.3.1||0<null>ArrayShape1.2.4.1||<null>", GetModOptMethodSignatureInfo(methodWithModOpt));
        }

        [Fact]
        public void TestSignatureMatchesForArrayShapeDetails_HandlingOfCasesWhichDoNotNeedEmbeddeSignatureData()
        {
            // Test that ensure the typical case (where the loBounds is 0, and the hibounds is unspecified) doesn't produce an
            // EmbeddedSignatureData, but that other cases do. This isn't a complete test as ilasm won't actually properly generate the metadata for many of these cases
            MetadataType modOptTester = _testModule.GetType(""u8, "ModOptTester"u8);
            MethodSignature methodWithModOpt = modOptTester.GetMethods().Single(m => string.Equals(m.GetName(), "Method5")).Signature;

            _output.WriteLine($"Found ModOptData '{GetModOptMethodSignatureInfo(methodWithModOpt)}'");
            Assert.Equal(2, methodWithModOpt.GetEmbeddedSignatureData().Length);
            Assert.EndsWith(methodWithModOpt.GetEmbeddedSignatureData()[0].index.Split('|')[0], MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(1));
            Assert.EndsWith(methodWithModOpt.GetEmbeddedSignatureData()[1].index.Split('|')[0], MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(2));
            Assert.Equal("ArrayShape1.2.2.1||0<null>ArrayShape1.2.3.1||<null>", GetModOptMethodSignatureInfo(methodWithModOpt));
        }

        [Fact]
        public void TestSignatureMatches()
        {
            MetadataType atomType = _testModule.GetType(""u8, "Atom"u8);
            MetadataType aType = _testModule.GetType(""u8, "A`1"u8);
            MetadataType aOfAtomType = aType.MakeInstantiatedType(new Instantiation(atomType));


            MetadataType baseClassType = _testModule.GetType(""u8, "BaseClass`2"u8);
            MethodDesc baseClassMethod = baseClassType.GetMethods().Single(m => string.Equals(m.GetName(), "Method"));
            MethodSignature baseClassMethodSignature = baseClassMethod.Signature;
            MethodSignatureBuilder matchingSignatureBuilder = new MethodSignatureBuilder(baseClassMethodSignature);
            matchingSignatureBuilder[0] = aOfAtomType;
            matchingSignatureBuilder[1] = atomType;
            MethodSignature matchingSignature = matchingSignatureBuilder.ToSignature();

            MetadataType derivedClassType = _testModule.GetType(""u8, "DerivedClass"u8);
            IEnumerable<MethodDesc> derivedClassMethods = derivedClassType.GetMethods().Where(m => string.Equals(m.GetName(), "Method"));
            IEnumerable<bool> matches = derivedClassMethods.Select(m => matchingSignature.Equals(m.Signature));
            int matchCount = matches.Select(b => b ? 1 : 0).Sum();
            Assert.Equal(1, matchCount);
        }

        [Fact]
        public void TestSerializedSignatureWithArrayShapes()
        {
            MetadataType modOptTester = _testModule.GetType(""u8, "ModOptTester"u8);
            MethodDesc methodWithInterestingShapes = modOptTester.GetMethods().Single(m => string.Equals(m.GetName(), "Method4"));

            // Create assembly with reference to interesting method
            TypeSystemMetadataEmitter metadataEmitter = new TypeSystemMetadataEmitter(new AssemblyNameInfo("Lookup"), _context);
            var token = metadataEmitter.GetMethodRef(methodWithInterestingShapes);
            Stream peStream = new MemoryStream();
            metadataEmitter.SerializeToStream(peStream);
            peStream.Seek(0, SeekOrigin.Begin);


            // Create new TypeSystemContext with just created assembly inside
            var lookupContext = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = lookupContext.CreateModuleForSimpleName("CoreTestAssembly");
            lookupContext.SetSystemModule(systemModule);

            lookupContext.CreateModuleForSimpleName("Lookup", peStream);

            // Use generated assembly to trigger a load through the token created above and verify that it loads correctly
            var ilLookupModule = (EcmaModule)lookupContext.GetModuleForSimpleName("Lookup");
            MethodDesc method4 = ilLookupModule.GetMethod(token);

            Assert.Equal("Method4", method4.GetName());
        }


        [Fact]
        public void TestSerializedSignatureWithReferenceToMDIntArray()
        {
            var typeInInitialContext = _context.GetWellKnownType(WellKnownType.Int32).MakeArrayType(3);

            // Create assembly with reference to interesting type
            TypeSystemMetadataEmitter metadataEmitter = new TypeSystemMetadataEmitter(new AssemblyNameInfo("Lookup"), _context);
            var token = metadataEmitter.GetTypeRef(typeInInitialContext);
            Stream peStream = new MemoryStream();
            metadataEmitter.SerializeToStream(peStream);
            peStream.Seek(0, SeekOrigin.Begin);


            // Create new TypeSystemContext with just created assembly inside
            var lookupContext = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = lookupContext.CreateModuleForSimpleName("CoreTestAssembly");
            lookupContext.SetSystemModule(systemModule);
            lookupContext.CreateModuleForSimpleName("Lookup", peStream);

            // Use generated assembly to trigger a load through the token created above and verify that it loads correctly
            var ilLookupModule = (EcmaModule)lookupContext.GetModuleForSimpleName("Lookup");
            TypeDesc int32ArrayFromLookup = ilLookupModule.GetType(token);
            var typeInLookupContext = lookupContext.GetWellKnownType(WellKnownType.Int32).MakeArrayType(3);

            Assert.Equal(typeInLookupContext, int32ArrayFromLookup);
        }

        [Fact]
        public void TestSerializedSignatureWithReferenceToFieldWithModOpt()
        {

            MetadataType modOptTester = _testModule.GetType(""u8, "ModOptTester"u8);
            FieldDesc fieldWithModOpt = modOptTester.GetFields().Single(m => string.Equals(m.GetName(), "fieldWithModOpt"));

            // Create assembly with reference to interesting method
            TypeSystemMetadataEmitter metadataEmitter = new TypeSystemMetadataEmitter(new AssemblyNameInfo("Lookup"), _context);
            var token = metadataEmitter.GetFieldRef(fieldWithModOpt);
            MemoryStream peStream = new MemoryStream();
            metadataEmitter.SerializeToStream(peStream);

            peStream.Seek(0, SeekOrigin.Begin);

            // Create new TypeSystemContext with just created assembly inside
            var lookupContext = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = lookupContext.CreateModuleForSimpleName("CoreTestAssembly");
            lookupContext.SetSystemModule(systemModule);

            lookupContext.CreateModuleForSimpleName("Lookup", peStream);

            // Use generated assembly to trigger a load through the token created above and verify that it loads correctly
            var ilLookupModule = (EcmaModule)lookupContext.GetModuleForSimpleName("Lookup");
            FieldDesc fieldFound = ilLookupModule.GetField(token);

            Assert.Equal("fieldWithModOpt", fieldFound.GetName());
        }

        [Fact]
        public void TestMDArrayFunctionReading()
        {
            MetadataType mdArrayFunctionResolutionType = _testModule.GetType(""u8, "MDArrayFunctionResolution"u8);
            MethodDesc methodWithMDArrayUsage = mdArrayFunctionResolutionType.GetMethods().Single(m => string.Equals(m.GetName(), "MethodWithUseOfMDArrayFunctions"));
            MethodIL methodIL = EcmaMethodIL.Create((EcmaMethod)methodWithMDArrayUsage);
            ILReader ilReader = new ILReader(methodIL.GetILBytes());
            int failures = 0;
            int successes = 0;
            while (ilReader.HasNext)
            {
                ILOpcode opcode = ilReader.ReadILOpcode();
                switch(opcode)
                {
                    case ILOpcode.call:
                    case ILOpcode.newobj:
                        int token = ilReader.ReadILToken();
                        object tokenReferenceResult = methodIL.GetObject(token, NotFoundBehavior.ReturnNull);
                        if (tokenReferenceResult == null)
                        {
                            failures++;
                            tokenReferenceResult = "null";
                        }
                        else
                        {
                            successes++;
                        }
                        _output.WriteLine($"call {tokenReferenceResult}");
                        break;
                }
            }

            Assert.Equal(0, failures);
            Assert.Equal(4, successes);
        }
    }
}
