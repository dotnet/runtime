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

            MetadataType simple = testModule.GetType("VirtualStaticInterfaceMethods"u8, "Simple"u8);
            MetadataType iSimple = testModule.GetType("VirtualStaticInterfaceMethods"u8, "ISimple"u8);
            MetadataType iVariant = testModule.GetType("VirtualStaticInterfaceMethods"u8, "IVariant`1"u8);
            MetadataType @base = testModule.GetType("VirtualStaticInterfaceMethods"u8, "Base"u8);
            MetadataType mid = testModule.GetType("VirtualStaticInterfaceMethods"u8, "Mid"u8);
            MetadataType derived = testModule.GetType("VirtualStaticInterfaceMethods"u8, "Derived"u8);
            MetadataType simpleVariant = testModule.GetType("VirtualStaticInterfaceMethods"u8, "SimpleVariant"u8);
            MetadataType simpleVariantTwice = testModule.GetType("VirtualStaticInterfaceMethods"u8, "SimpleVariantTwice"u8);
            MetadataType variantWithInheritanceDerived = testModule.GetType("VirtualStaticInterfaceMethods"u8, "VariantWithInheritanceDerived"u8);
            MetadataType genericVariantWithInheritanceDerived = testModule.GetType("VirtualStaticInterfaceMethods"u8, "GenericVariantWithInheritanceDerived`1"u8);
            MetadataType genericVariantWithHiddenBase = testModule.GetType("VirtualStaticInterfaceMethods"u8, "GenericVariantWithHiddenBase"u8);
            MetadataType genericVariantWithHiddenDerived = testModule.GetType("VirtualStaticInterfaceMethods"u8, "GenericVariantWithHiddenDerived`1"u8);

            MethodDesc iSimpleMethod = iSimple.GetMethod("WhichMethod"u8, null);
            MethodDesc iVariantBaseMethod = iVariant.MakeInstantiatedType(@base).GetMethod("WhichMethod"u8, null);
            MethodDesc iVariantMidMethod = iVariant.MakeInstantiatedType(mid).GetMethod("WhichMethod"u8, null);
            MethodDesc iVariantDerivedMethod = iVariant.MakeInstantiatedType(derived).GetMethod("WhichMethod"u8, null);

            yield return new object[] { simple, iSimpleMethod, simple.GetMethod("WhichMethod"u8, null) };

            yield return new object[] { simpleVariant, iVariantBaseMethod, simpleVariant.GetMethod("WhichMethod"u8, null) };
            yield return new object[] { simpleVariant, iVariantDerivedMethod, simpleVariant.GetMethod("WhichMethod"u8, null) };

            yield return new object[] { simpleVariantTwice, iVariantBaseMethod, simpleVariantTwice.GetMethod("WhichMethod"u8, new MethodSignature(MethodSignatureFlags.Static, 0, context.GetWellKnownType(WellKnownType.String), new TypeDesc[] { @base })) };
            yield return new object[] { simpleVariantTwice, iVariantMidMethod, simpleVariantTwice.GetMethod("WhichMethod"u8, new MethodSignature(MethodSignatureFlags.Static, 0, context.GetWellKnownType(WellKnownType.String), new TypeDesc[] { mid })) };
            yield return new object[] { simpleVariantTwice, iVariantDerivedMethod, simpleVariantTwice.GetMethod("WhichMethod"u8, new MethodSignature(MethodSignatureFlags.Static, 0, context.GetWellKnownType(WellKnownType.String), new TypeDesc[] { @base })) };

            yield return new object[] { variantWithInheritanceDerived, iVariantBaseMethod, variantWithInheritanceDerived.GetMethod("WhichMethod"u8, null) };
            yield return new object[] { variantWithInheritanceDerived, iVariantMidMethod, variantWithInheritanceDerived.GetMethod("WhichMethod"u8, null) };
            yield return new object[] { variantWithInheritanceDerived, iVariantDerivedMethod, variantWithInheritanceDerived.GetMethod("WhichMethod"u8, null) };

            yield return new object[] { genericVariantWithInheritanceDerived.MakeInstantiatedType(@base), iVariantBaseMethod, genericVariantWithInheritanceDerived.MakeInstantiatedType(@base).GetMethod("WhichMethod"u8, null) };
            yield return new object[] { genericVariantWithInheritanceDerived.MakeInstantiatedType(@base), iVariantMidMethod, genericVariantWithInheritanceDerived.MakeInstantiatedType(@base).GetMethod("WhichMethod"u8, null) };
            yield return new object[] { genericVariantWithInheritanceDerived.MakeInstantiatedType(mid), iVariantMidMethod, genericVariantWithInheritanceDerived.MakeInstantiatedType(mid).GetMethod("WhichMethod"u8, null) };

            yield return new object[] { genericVariantWithHiddenDerived.MakeInstantiatedType(@base), iVariantBaseMethod, genericVariantWithHiddenDerived.MakeInstantiatedType(@base).GetMethod("WhichMethod"u8, null) };
            yield return new object[] { genericVariantWithHiddenDerived.MakeInstantiatedType(@base), iVariantMidMethod, genericVariantWithHiddenDerived.MakeInstantiatedType(@base).GetMethod("WhichMethod"u8, null) };
            yield return new object[] { genericVariantWithHiddenDerived.MakeInstantiatedType(mid), iVariantMidMethod, genericVariantWithHiddenDerived.MakeInstantiatedType(mid).GetMethod("WhichMethod"u8, null) };
            yield return new object[] { genericVariantWithHiddenDerived.MakeInstantiatedType(derived), iVariantMidMethod, genericVariantWithHiddenBase.GetMethod("WhichMethod"u8, null) };
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
