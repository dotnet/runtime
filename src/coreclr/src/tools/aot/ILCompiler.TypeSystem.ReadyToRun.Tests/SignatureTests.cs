// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Linq;
using System.Text;

using Internal.IL;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

using Xunit;

namespace TypeSystemTests
{
    public class SignatureTests
    {
        private TestTypeSystemContext _context;
        private ModuleDesc _testModule;

        public SignatureTests()
        {
            _context = new TestTypeSystemContext(TargetArchitecture.X64);
            var systemModule = _context.CreateModuleForSimpleName("CoreTestAssembly");
            _context.SetSystemModule(systemModule);

            _testModule = _context.GetModuleForSimpleName("ILTestAssembly");
        }

        private static string GetModOptMethodSignatureInfo(MethodSignature signature)
        {
            if (!signature.HasEmbeddedSignatureData || signature.GetEmbeddedSignatureData() == null)
                return "";

            StringBuilder sb = new StringBuilder();
            foreach (EmbeddedSignatureData data in signature.GetEmbeddedSignatureData())
            {
                sb.Append(data.kind.ToString());
                sb.Append(data.index);
                sb.Append(((MetadataType)data.type).Name);
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
    }
}
