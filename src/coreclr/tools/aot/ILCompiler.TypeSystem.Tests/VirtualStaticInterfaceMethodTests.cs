// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;

using Xunit;

namespace TypeSystemTests
{
    public class VirtualStaticInterfaceMethodTests
    {
        public static IEnumerable<object[]> VariantTestData()
        {
            var context = new TestTypeSystemContext(TargetArchitecture.Unknown);
            ModuleDesc testModule = context.CreateModuleForSimpleName("CoreTestAssembly");
            context.SetSystemModule(testModule);

            MetadataType simple = testModule.GetType("VirtualStaticInterfaceMethods", "Simple");
            MetadataType iSimple = testModule.GetType("VirtualStaticInterfaceMethods", "ISimple");
            MetadataType iVariant = testModule.GetType("VirtualStaticInterfaceMethods", "IVariant`1");
            MetadataType @base = testModule.GetType("VirtualStaticInterfaceMethods", "Base");
            MetadataType mid = testModule.GetType("VirtualStaticInterfaceMethods", "Mid");
            MetadataType derived = testModule.GetType("VirtualStaticInterfaceMethods", "Derived");
            MetadataType simpleVariant = testModule.GetType("VirtualStaticInterfaceMethods", "SimpleVariant");
            MetadataType simpleVariantTwice = testModule.GetType("VirtualStaticInterfaceMethods", "SimpleVariantTwice");
            MetadataType variantWithInheritanceDerived = testModule.GetType("VirtualStaticInterfaceMethods", "VariantWithInheritanceDerived");
            MetadataType genericVariantWithInheritanceDerived = testModule.GetType("VirtualStaticInterfaceMethods", "GenericVariantWithInheritanceDerived`1");
            MetadataType genericVariantWithHiddenBase = testModule.GetType("VirtualStaticInterfaceMethods", "GenericVariantWithHiddenBase");
            MetadataType genericVariantWithHiddenDerived = testModule.GetType("VirtualStaticInterfaceMethods", "GenericVariantWithHiddenDerived`1");

            MethodDesc iSimpleMethod = iSimple.GetMethod("WhichMethod", null);
            MethodDesc iVariantBaseMethod = iVariant.MakeInstantiatedType(@base).GetMethod("WhichMethod", null);
            MethodDesc iVariantMidMethod = iVariant.MakeInstantiatedType(mid).GetMethod("WhichMethod", null);
            MethodDesc iVariantDerivedMethod = iVariant.MakeInstantiatedType(derived).GetMethod("WhichMethod", null);

            yield return new object[] { simple, iSimpleMethod, simple.GetMethod("WhichMethod", null) };

            yield return new object[] { simpleVariant, iVariantBaseMethod, simpleVariant.GetMethod("WhichMethod", null) };
            yield return new object[] { simpleVariant, iVariantDerivedMethod, simpleVariant.GetMethod("WhichMethod", null) };

            yield return new object[] { simpleVariantTwice, iVariantBaseMethod, simpleVariantTwice.GetMethod("WhichMethod", new MethodSignature(MethodSignatureFlags.Static, 0, context.GetWellKnownType(WellKnownType.String), new TypeDesc[] { @base })) };
            yield return new object[] { simpleVariantTwice, iVariantMidMethod, simpleVariantTwice.GetMethod("WhichMethod", new MethodSignature(MethodSignatureFlags.Static, 0, context.GetWellKnownType(WellKnownType.String), new TypeDesc[] { mid })) };
            yield return new object[] { simpleVariantTwice, iVariantDerivedMethod, simpleVariantTwice.GetMethod("WhichMethod", new MethodSignature(MethodSignatureFlags.Static, 0, context.GetWellKnownType(WellKnownType.String), new TypeDesc[] { @base })) };

            yield return new object[] { variantWithInheritanceDerived, iVariantBaseMethod, variantWithInheritanceDerived.GetMethod("WhichMethod", null) };
            yield return new object[] { variantWithInheritanceDerived, iVariantMidMethod, variantWithInheritanceDerived.GetMethod("WhichMethod", null) };
            yield return new object[] { variantWithInheritanceDerived, iVariantDerivedMethod, variantWithInheritanceDerived.GetMethod("WhichMethod", null) };

            yield return new object[] { genericVariantWithInheritanceDerived.MakeInstantiatedType(@base), iVariantBaseMethod, genericVariantWithInheritanceDerived.MakeInstantiatedType(@base).GetMethod("WhichMethod", null) };
            yield return new object[] { genericVariantWithInheritanceDerived.MakeInstantiatedType(@base), iVariantMidMethod, genericVariantWithInheritanceDerived.MakeInstantiatedType(@base).GetMethod("WhichMethod", null) };
            yield return new object[] { genericVariantWithInheritanceDerived.MakeInstantiatedType(mid), iVariantMidMethod, genericVariantWithInheritanceDerived.MakeInstantiatedType(mid).GetMethod("WhichMethod", null) };

            yield return new object[] { genericVariantWithHiddenDerived.MakeInstantiatedType(@base), iVariantBaseMethod, genericVariantWithHiddenDerived.MakeInstantiatedType(@base).GetMethod("WhichMethod", null) };
            yield return new object[] { genericVariantWithHiddenDerived.MakeInstantiatedType(@base), iVariantMidMethod, genericVariantWithHiddenDerived.MakeInstantiatedType(@base).GetMethod("WhichMethod", null) };
            yield return new object[] { genericVariantWithHiddenDerived.MakeInstantiatedType(mid), iVariantMidMethod, genericVariantWithHiddenDerived.MakeInstantiatedType(mid).GetMethod("WhichMethod", null) };
            yield return new object[] { genericVariantWithHiddenDerived.MakeInstantiatedType(derived), iVariantMidMethod, genericVariantWithHiddenBase.GetMethod("WhichMethod", null) };
        }

        [Theory]
        [MemberData(nameof(VariantTestData))]
        public void Test(MetadataType theClass, MethodDesc intfMethod, MethodDesc expected)
        {
            MethodDesc result = theClass.ResolveVariantInterfaceMethodToStaticVirtualMethodOnType(intfMethod);
            Assert.Equal(expected, result);
        }
    }
}
