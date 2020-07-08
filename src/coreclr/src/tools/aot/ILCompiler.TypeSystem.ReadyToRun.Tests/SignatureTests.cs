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
