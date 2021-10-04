// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

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
                    sb.Append(((MetadataType)data.type).Name);
                else
                    sb.Append("<null>");
            }
            return sb.ToString();
        }

        [Fact]
        public void TestSignatureMatches2ModOptsAtStartOfSig()
        {
            MetadataType modOptTester = _testModule.GetType("", "ModOptTester");
            MethodSignature methodWith2ModOptsAtStartOfSig = modOptTester.GetMethods().Single(m => string.Equals(m.Name, "Method")).Signature;

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
            MetadataType modOptTester = _testModule.GetType("", "ModOptTester");
            MethodSignature methodWithModOptAtStartOfSigAndAfterByRef = modOptTester.GetMethods().Single(m => string.Equals(m.Name, "Method2")).Signature;

            // A modopts after an E_T_BYREF will look like 0.1.1.2.1.1
            Assert.Equal(MethodSignature.IndexOfCustomModifiersOnReturnType, methodWithModOptAtStartOfSigAndAfterByRef.GetEmbeddedSignatureData()[0].index);
            Assert.NotEqual(MethodSignature.IndexOfCustomModifiersOnReturnType, methodWithModOptAtStartOfSigAndAfterByRef.GetEmbeddedSignatureData()[1].index);
            Assert.NotEqual(MethodSignature.IndexOfCustomModifiersOnReturnType, methodWithModOptAtStartOfSigAndAfterByRef.GetEmbeddedSignatureData()[2].index);
            Assert.Equal("OptionalCustomModifier0.1.1.1CharOptionalCustomModifier0.1.1.2.1.1VoidOptionalCustomModifier0.1.2.1FooModifier", GetModOptMethodSignatureInfo(methodWithModOptAtStartOfSigAndAfterByRef));
        }

        [Fact]
        public void TestSignatureMatchesModoptOnPointerOrRefModifiedType()
        {
            MetadataType modOptTester = _testModule.GetType("", "ModOptTester");
            MethodSignature methodWithModOpt = modOptTester.GetMethods().Single(m => string.Equals(m.Name, "Method3")).Signature;
            Assert.Equal(MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(0), methodWithModOpt.GetEmbeddedSignatureData()[0].index);
            Assert.Equal(MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(1), methodWithModOpt.GetEmbeddedSignatureData()[1].index);
            Assert.Equal(MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(2), methodWithModOpt.GetEmbeddedSignatureData()[2].index);
        }

        [Fact]
        public void TestSignatureMatchesForArrayShapeDetails()
        {
            MetadataType modOptTester = _testModule.GetType("", "ModOptTester");
            MethodSignature methodWithModOpt = modOptTester.GetMethods().Single(m => string.Equals(m.Name, "Method4")).Signature;

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
            MetadataType modOptTester = _testModule.GetType("", "ModOptTester");
            MethodSignature methodWithModOpt = modOptTester.GetMethods().Single(m => string.Equals(m.Name, "Method5")).Signature;

            _output.WriteLine($"Found ModOptData '{GetModOptMethodSignatureInfo(methodWithModOpt)}'");
            Assert.Equal(2, methodWithModOpt.GetEmbeddedSignatureData().Length);
            Assert.EndsWith(methodWithModOpt.GetEmbeddedSignatureData()[0].index.Split('|')[0], MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(1));
            Assert.EndsWith(methodWithModOpt.GetEmbeddedSignatureData()[1].index.Split('|')[0], MethodSignature.GetIndexOfCustomModifierOnPointedAtTypeByParameterIndex(2));
            Assert.Equal("ArrayShape1.2.2.1||0<null>ArrayShape1.2.3.1||<null>", GetModOptMethodSignatureInfo(methodWithModOpt));
        }

        [Fact]
        public void TestSignatureMatches()
        {
            MetadataType atomType = _testModule.GetType("", "Atom");
            MetadataType aType = _testModule.GetType("", "A`1");
            MetadataType aOfAtomType = aType.MakeInstantiatedType(new Instantiation(atomType));


            MetadataType baseClassType = _testModule.GetType("", "BaseClass`2");
            MethodDesc baseClassMethod = baseClassType.GetMethods().Single(m => string.Equals(m.Name, "Method"));
            MethodSignature baseClassMethodSignature = baseClassMethod.Signature;
            MethodSignatureBuilder matchingSignatureBuilder = new MethodSignatureBuilder(baseClassMethodSignature);
            matchingSignatureBuilder[0] = aOfAtomType;
            matchingSignatureBuilder[1] = atomType;
            MethodSignature matchingSignature = matchingSignatureBuilder.ToSignature();

            MetadataType derivedClassType = _testModule.GetType("", "DerivedClass");
            IEnumerable<MethodDesc> derivedClassMethods = derivedClassType.GetMethods().Where(m => string.Equals(m.Name, "Method"));
            IEnumerable<bool> matches = derivedClassMethods.Select(m => matchingSignature.Equals(m.Signature));
            int matchCount = matches.Select(b => b ? 1 : 0).Sum();
            Assert.Equal(1, matchCount);
        }

        [Fact]
        public void TestSerializedSignatureWithArrayShapes()
        {
            MetadataType modOptTester = _testModule.GetType("", "ModOptTester");
            MethodDesc methodWithInterestingShapes = modOptTester.GetMethods().Single(m => string.Equals(m.Name, "Method4"));

            // Create assembly with reference to interesting method
            TypeSystemMetadataEmitter metadataEmitter = new TypeSystemMetadataEmitter(new System.Reflection.AssemblyName("Lookup"), _context);
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

            Assert.Equal("Method4", method4.Name);
        }


        [Fact]
        public void TestSerializedSignatureWithReferenceToMDIntArray()
        {
            var typeInInitialContext = _context.GetWellKnownType(WellKnownType.Int32).MakeArrayType(3);

            // Create assembly with reference to interesting type
            TypeSystemMetadataEmitter metadataEmitter = new TypeSystemMetadataEmitter(new System.Reflection.AssemblyName("Lookup"), _context);
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
        public void TestMDArrayFunctionReading()
        {
            MetadataType mdArrayFunctionResolutionType = _testModule.GetType("", "MDArrayFunctionResolution");
            MethodDesc methodWithMDArrayUsage = mdArrayFunctionResolutionType.GetMethods().Single(m => string.Equals(m.Name, "MethodWithUseOfMDArrayFunctions"));
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
                        _output.WriteLine($"call {tokenReferenceResult.ToString()}");
                        break;
                }
            }

            Assert.Equal(0, failures);
            Assert.Equal(4, successes);
        }
    }
}
