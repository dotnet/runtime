// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.UnitTesting;
using Tests.Integration;
using Xunit;

namespace System.ComponentModel.Composition
{
    public class CompositionServicesTests
    {
        private static readonly Type NonGenericType = typeof(int);

        [Fact]
        public void GetContractName_NullAsTypeArgument_ThrowsArgumentNull()
        {
            Assert.Throws<ArgumentNullException>("type", () =>
                {
                    AttributedModelServices.GetContractName((Type)null);
                });
        }        

        [Fact]
        [Description("Verifies adding custom modifiers to contract name.")]
        public void ContractNameServicesAddCustomModifiersTest()
        {
            Type[] modifiers = new Type[] { typeof(int), typeof(List<int>), typeof(double) };
            StringBuilder typeName = new StringBuilder();
            ContractNameServices.WriteCustomModifiers(typeName, "test", modifiers, false);
            Assert.Equal(
                string.Format(" {0}(System.Int32,System.Collections.Generic.List(System.Int32),System.Double)", "test"),
                typeName.ToString());
        }

        [Fact]
        [Description("Verifies CompositionServices.GetDefaultContractName method.")]
        public void GetDefaultContractNameTest()
        {
            ExpectationCollection<Type, string> expectations = new ExpectationCollection<Type, string>();

            expectations.Add(typeof(string), "System.String");
            expectations.Add(typeof(int?), "System.Nullable(System.Int32)");
            expectations.Add(typeof(NestedParent.NestedChild), "System.ComponentModel.Composition.NestedParent+NestedChild");
            expectations.Add(typeof(int[,]), "System.Int32[,]");
            expectations.Add(typeof(int[,][][]), "System.Int32[,][][]");
            expectations.Add(typeof(Array[,][][]).MakePointerType(), "System.Array[,][][]*");
            expectations.Add(typeof(int).MakePointerType(), "System.Int32*");
            expectations.Add(typeof(int).MakePointerType().MakeArrayType(3).MakeArrayType().MakePointerType(), "System.Int32*[][,,]*");
            expectations.Add(typeof(int).MakeByRefType(), "System.Int32&");
            expectations.Add(typeof(int).MakePointerType().MakeArrayType(4).MakeArrayType().MakeByRefType(), "System.Int32*[][,,,]&");
            expectations.Add(typeof(List<>), "System.Collections.Generic.List({0})");
            expectations.Add(typeof(Dictionary<int,double>), "System.Collections.Generic.Dictionary(System.Int32,System.Double)");
            expectations.Add(typeof(Dictionary<Dictionary<int,double>,double>), "System.Collections.Generic.Dictionary(System.Collections.Generic.Dictionary(System.Int32,System.Double),System.Double)");
            expectations.Add(typeof(GenericContract1<int>.GenericContract2.GenericContract3<double>), "System.ComponentModel.Composition.GenericContract1(System.Int32)+GenericContract2+GenericContract3(System.Double)");
            expectations.Add(typeof(GenericContract4<int,double>.GenericContract5<double,int>.GenericContract6<int,double>), "System.ComponentModel.Composition.GenericContract4(System.Int32,System.Double)+GenericContract5(System.Double,System.Int32)+GenericContract6(System.Int32,System.Double)");
            expectations.Add(typeof(GenericContract4<,>.GenericContract5<,>.GenericContract6<,>), "System.ComponentModel.Composition.GenericContract4({0},{1})+GenericContract5({2},{3})+GenericContract6({4},{5})");
            expectations.Add(typeof(GenericContract7), "System.ComponentModel.Composition.GenericContract7");
            expectations.Add(typeof(GenericContract8<int>), "System.ComponentModel.Composition.GenericContract8(System.Int32)");
            expectations.Add(typeof(GenericContract4<int,double>.GenericContract5<double,int>[][,,,,]), "System.ComponentModel.Composition.GenericContract4(System.Int32,System.Double)+GenericContract5(System.Double,System.Int32)[][,,,,]");
            expectations.Add(typeof(GenericContract4<int,double>.GenericContract5<double,int>).MakePointerType(), "System.ComponentModel.Composition.GenericContract4(System.Int32,System.Double)+GenericContract5(System.Double,System.Int32)*");
            expectations.Add(typeof(GenericContract4<int,double>.GenericContract5<double,int>[][,]).MakePointerType().MakeArrayType(4), "System.ComponentModel.Composition.GenericContract4(System.Int32,System.Double)+GenericContract5(System.Double,System.Int32)[][,]*[,,,]");            
            expectations.Add(typeof(OuterClassWithGenericNested.GenericNested<int>), "System.ComponentModel.Composition.OuterClassWithGenericNested+GenericNested(System.Int32)");
            expectations.Add(typeof(Delegate), "System.Delegate");
            expectations.Add(typeof(MulticastDelegate), "System.MulticastDelegate");
            expectations.Add(typeof(Action), "System.Void()");
            expectations.Add(typeof(Func<Func<string>>), "System.Func(System.String)()");
            expectations.Add(typeof(DelegateCompositionTests.DoWorkDelegate), "System.Object(System.Int32,System.Object&,System.String&)");
            expectations.Add(typeof(DelegateCompositionTests.DelegateOneArg), "System.Int32(System.Int32)");
            expectations.Add(typeof(DelegateCompositionTests.DelegateTwoArgs), "System.Int32(System.Int32,System.Int32)");
            expectations.Add(typeof(DelegateCompositionTests.SimpleDelegate), "System.Int32()");
            expectations.Add(EmitGenericType("MyGeneratedType", typeof(int)), "MyGeneratedType(System.Int32)");
            expectations.Add(EmitGenericType("MyGeneratedType`1", typeof(string)), "MyGeneratedType(System.String)");
            expectations.Add(EmitGenericType("_name_", typeof(List<>)), "_name_(System.Collections.Generic.List({0}))");

            // This particular case can clash with the name generation for methods because we use () for both generics and methods
            // we may want to fix this clash by changing the generic markers but it is likely to be very rare.
            expectations.Add(EmitGenericType("System.Void", typeof(int)), "System.Void(System.Int32)");

            foreach (var e in expectations)
            {
                string typeIdentity = AttributedModelServices.GetTypeIdentity(e.Input);
                Assert.Equal(e.Output, typeIdentity);
            }

            // Do it again to excerise the cache.
            foreach (var e in expectations)
            {
                string typeIdentity = AttributedModelServices.GetTypeIdentity(e.Input);
                Assert.Equal(e.Output, typeIdentity);
            }
        }

        public Type EmitGenericType(string typeName, Type genericArgumentType)
        {
            var assemblyName = new AssemblyName("EmittedTestAssembly");
            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.Run);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule("EmittedTestModule");
            var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Public);

            GenericTypeParameterBuilder T = typeBuilder.DefineGenericParameters(new string[] { "T" })[0];
            typeBuilder.SetCustomAttribute(new CustomAttributeBuilder(typeof(ExportAttribute).GetConstructor(Type.EmptyTypes), new object[0]));

            return typeBuilder.CreateType().MakeGenericType(genericArgumentType);
        }
    }
}
