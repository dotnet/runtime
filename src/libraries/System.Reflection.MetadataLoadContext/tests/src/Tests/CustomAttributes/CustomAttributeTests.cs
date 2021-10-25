// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using SampleMetadata;
using Xunit;

namespace System.Reflection.Tests
{
    public static partial class CustomAttributeTests
    {
        [Fact]
        public static void CustomAttributeTest1()
        {
            Type t = typeof(AttributeHolder1);  // Intentionally not projected. We're reflecting on this (and Invoking it) to get the validation baseline data.
            foreach (Type nt in t.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
            {
                SampleCustomAttribute attr = nt.GetCustomAttribute<SampleCustomAttribute>(inherit: false);
                CustomAttributeData cad = nt.CustomAttributes.Single(c => c.AttributeType == typeof(SampleCustomAttribute));
                object value = attr.Argument;  // Capture the actual value passed to the SampleCustomAttribute constructor.
                Type parameterType = cad.Constructor.GetParameters()[0].ParameterType;  // Capture the formal parameter type of the constructor.

                Type ntProjected = nt.Project();
                CustomAttributeData cadProjected = ntProjected.CustomAttributes.Single(c => c.AttributeType == typeof(SampleCustomAttribute).Project());
                Assert.Equal(typeof(SampleCustomAttribute).Project(), cadProjected.AttributeType);
                Assert.Equal(1, cadProjected.ConstructorArguments.Count);
                cadProjected.ConstructorArguments[0].Validate(parameterType, value);
            }
        }

        [Fact]
        public static void CustomAttributeInAnotherAssembly()
        {
            Type t = typeof(HoldsAttributeDefinedInAnotherAssembly).Project();
            CustomAttributeData cad = t.CustomAttributes.Single();
            Assert.Equal(typeof(GuidAttribute).Project(), cad.AttributeType);
        }

        [Fact]
        public static void CustomAttributeNamedArguments_Field()
        {
            Type t = typeof(HoldsCaWithNamedArguments.N1).Project();
            CustomAttributeData cad = t.CustomAttributes.Single();
            Assert.Equal(typeof(CaWithNamedArguments).Project(), cad.AttributeType);
            Assert.Equal(0, cad.ConstructorArguments.Count);
            Assert.Equal(1, cad.NamedArguments.Count);
            CustomAttributeNamedArgument can = cad.NamedArguments[0];
            Assert.True(can.IsField);
            Assert.Equal(typeof(CaWithNamedArguments).Project().GetField("MyField"), can.MemberInfo);
            can.TypedValue.Validate(typeof(int).Project(), 4);
        }

        [Fact]
        public static void CustomAttributeNamedArguments_Property()
        {
            Type t = typeof(HoldsCaWithNamedArguments.N2).Project();
            CustomAttributeData cad = t.CustomAttributes.Single();
            Assert.Equal(typeof(CaWithNamedArguments).Project(), cad.AttributeType);
            Assert.Equal(0, cad.ConstructorArguments.Count);
            Assert.Equal(1, cad.NamedArguments.Count);
            CustomAttributeNamedArgument can = cad.NamedArguments[0];
            Assert.False(can.IsField);
            Assert.Equal(typeof(CaWithNamedArguments).Project().GetProperty("MyProperty"), can.MemberInfo);
            can.TypedValue.Validate(typeof(int).Project(), 8);
        }

        private static object UnwrapEnum(this Enum e)
        {
            FieldInfo f = e.GetType().GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly).First();
            return f.GetValue(e);
        }

        private static void Validate(this CustomAttributeTypedArgument cat, Type parameterType, object value)
        {
            if (value == null)
            {
                Assert.Null(cat.Value);
                if (parameterType == typeof(object))
                {
                    // Why "string?" That's what .NET Framework has always put here for this corner case.
                    Assert.Equal(typeof(string).Project(), cat.ArgumentType);
                }
                else
                {
                    Assert.Equal(parameterType.Project(), cat.ArgumentType);
                }
            }
            else if (value is Enum)
            {
                Assert.Equal(value.GetType().Project(), cat.ArgumentType);
                Assert.Equal(((Enum)value).UnwrapEnum(), cat.Value);
            }
            else if (value is Type valueAsType)
            {
                Assert.Equal(typeof(Type).Project(), cat.ArgumentType);
                Assert.Equal(valueAsType.Project(), cat.Value);
            }
            else if (value is Array valueAsArray)
            {
                Assert.Equal(value.GetType().Project(), cat.ArgumentType);
                Assert.True(cat.Value is ReadOnlyCollection<CustomAttributeTypedArgument>);
                IList<CustomAttributeTypedArgument> cats = (IList<CustomAttributeTypedArgument>)(cat.Value);
                Assert.Equal(valueAsArray.Length, cats.Count);
                for (int i = 0; i < cats.Count; i++)
                {
                    cats[i].Validate(valueAsArray.GetType().GetElementType(), valueAsArray.GetValue(i));
                }
            }
            else
            {
                Assert.Equal(value.GetType().Project(), cat.ArgumentType);
                Assert.Equal(value, cat.Value);
            }
        }

        internal static void ValidateCustomAttributesAllocatesFreshObjectsEachTime(Func<IEnumerable<CustomAttributeData>> action)
        {
            IEnumerable<CustomAttributeData> cads1 = action();
            IEnumerable<CustomAttributeData> cads2 = action();
            cads1.ValidateEqualButFreshlyAllocated(cads2);
        }

        internal static void ValidateEqualButFreshlyAllocated(this IEnumerable<CustomAttributeData> cads1, IEnumerable<CustomAttributeData> cads2)
        {
            CustomAttributeData[] acads1 = cads1.ToArray();
            CustomAttributeData[] acads2 = cads2.ToArray();
            Assert.Equal(acads1.Length, acads2.Length);
            if (acads1.Length != 0)
            {
                Assert.NotSame(cads1, cads2);
            }
            for (int i = 0; i < acads1.Length; i++)
            {
                CustomAttributeData cad1 = acads1[i];
                CustomAttributeData cad2 = acads2[i];
                Assert.Equal(cad1.AttributeType, cad2.AttributeType);
                Assert.Equal(cad1.Constructor, cad2.Constructor);

                Assert.Equal(cad1.ConstructorArguments.Count, cad2.ConstructorArguments.Count);
                if (cad1.ConstructorArguments.Count != 0)
                {
                    Assert.NotSame(cad1.ConstructorArguments, cad2.ConstructorArguments);
                }
                Assert.True(cad1.ConstructorArguments is ReadOnlyCollection<CustomAttributeTypedArgument>);
                Assert.True(cad2.ConstructorArguments is ReadOnlyCollection<CustomAttributeTypedArgument>);
                for (int j = 0; j < cad1.ConstructorArguments.Count; j++)
                {
                    cad1.ConstructorArguments[j].ValidateEqualButFreshlyAllocated(cad2.ConstructorArguments[j]);
                }

                Assert.Equal(cad1.NamedArguments.Count, cad2.NamedArguments.Count);
                if (cad1.NamedArguments.Count != 0)
                {
                    Assert.NotSame(cad1.NamedArguments, cad2.NamedArguments);
                }
                Assert.True(cad1.NamedArguments is ReadOnlyCollection<CustomAttributeNamedArgument>);
                Assert.True(cad2.NamedArguments is ReadOnlyCollection<CustomAttributeNamedArgument>);
                for (int j = 0; j < cad1.NamedArguments.Count; j++)
                {
                    cad1.NamedArguments[j].TypedValue.ValidateEqualButFreshlyAllocated(cad2.NamedArguments[j].TypedValue);
                }
            }
        }

        private static void ValidateEqualButFreshlyAllocated(this CustomAttributeTypedArgument cat1, CustomAttributeTypedArgument cat2)
        {
            Assert.Equal(cat1.ArgumentType, cat2.ArgumentType);
            if (cat1.Value == null && cat2.Value == null)
                return;

            if (!cat1.ArgumentType.IsArray)
            {
                Assert.Equal(cat1.Value, cat2.Value);
                return;
            }

            Assert.True(cat1.Value is ReadOnlyCollection<CustomAttributeTypedArgument>);
            Assert.True(cat2.Value is ReadOnlyCollection<CustomAttributeTypedArgument>);
            IList<CustomAttributeTypedArgument> cats1 = (IList<CustomAttributeTypedArgument>)cat1.Value;
            IList<CustomAttributeTypedArgument> cats2 = (IList<CustomAttributeTypedArgument>)cat2.Value;
            Assert.NotSame(cats1, cats2);
            Assert.Equal(cats1.Count, cats2.Count);
            for (int i = 0; i < cats1.Count; i++)
            {
                cats1[i].ValidateEqualButFreshlyAllocated(cats2[i]);
            }
        }

        // This test only exists to provide code coverage for the fast-path AttributeType implementation while we're stuck in a netstandard-only build
        // configuration. It should be removed once both of these conditions are true:
        //
        //  -  We have an official on-going build and CI of a netcore configuration of System.Reflection.MetadataLoadContext.
        //  -  That build is consuming corefx contracts where CustomAttributeData.AttributeType is virtual (see https://github.com/dotnet/runtime/issues/27071)
        //
        // Once these conditions are satisfied, it is no longer necessary to resort to Reflection to invoke the fast-path AttributeType code.
        // Invoking CustomAttributeData.AttributeType the normal way will do the trick.
        //
        [Fact]
        public static void TestVirtualAttributeTypeProperty()
        {
            {
                Type t = typeof(AttributeHolder1.N1).Project();
                CustomAttributeData[] cads = t.CustomAttributes.ToArray();
                Assert.Equal(1, cads.Length);
                CustomAttributeData cad = cads[0];
                Type attributeType = cad.CallUsingReflection<Type>("get_AttributeType");
                Assert.Equal(typeof(SampleCustomAttribute).Project(), attributeType);
            }

            {
                Type t = typeof(HoldsAttributeDefinedInAnotherAssembly).Project();
                CustomAttributeData[] cads = t.CustomAttributes.ToArray();
                Assert.Equal(1, cads.Length);
                CustomAttributeData cad = cads[0];
                Type attributeType = cad.CallUsingReflection<Type>("get_AttributeType");
                Assert.Equal(typeof(GuidAttribute).Project(), attributeType);
            }
        }
    }
}
