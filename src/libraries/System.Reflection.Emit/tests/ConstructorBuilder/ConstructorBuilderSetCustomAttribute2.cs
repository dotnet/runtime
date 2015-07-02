// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Reflection;
using System.Reflection.Emit;
using Xunit;
using System.Linq;

namespace System.Reflection.Emit.Tests
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class CBMyAttribute2 : Attribute
    {
        public int m_i;

        public CBMyAttribute2(int i)
        {
            m_i = i;
        }
    }

    public class ConstructorBuilderSetCustomAttribute2
    {
        [Fact]
        public void TestSetCustomAttribute()
        {
            AssemblyName myAssemblyName = new AssemblyName("EmittedAssembly");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");
            TypeBuilder myTypeBuilder = myModuleBuilder.DefineType("HelloWorld", TypeAttributes.Public);
            ConstructorBuilder myConstructor = myTypeBuilder.DefineConstructor(
                     MethodAttributes.Public, CallingConventions.Standard, new Type[] { });
            ILGenerator myILGenerator = myConstructor.GetILGenerator();
            myILGenerator.Emit(OpCodes.Ldarg_1);

            ConstructorInfo myConstructorInfo = typeof(CBMyAttribute2).GetConstructor(new Type[1] { typeof(int) });
            CustomAttributeBuilder attributeBuilder =
               new CustomAttributeBuilder(myConstructorInfo, new object[1] { 2 });

            myConstructor.SetCustomAttribute(attributeBuilder);
            Type myHelloworld = myTypeBuilder.CreateTypeInfo().AsType();

            ConstructorInfo myReflectConstructorInfo = myHelloworld.GetConstructor(new Type[] { });
            object[] CBMyAttribute2s = myReflectConstructorInfo.GetCustomAttributes(true).Select(a => (object)a).ToArray();

            Assert.Equal(1, CBMyAttribute2s.Length);
            Assert.Equal(2, ((CBMyAttribute2)CBMyAttribute2s[0]).m_i);
        }

        [Fact]
        public void TestThrowsExceptionOnNullCustomAttributeBuilder()
        {
            AssemblyName myAssemblyName = new AssemblyName("EmittedAssembly");
            AssemblyBuilder myAssembly = AssemblyBuilder.DefineDynamicAssembly(myAssemblyName, AssemblyBuilderAccess.Run);
            ModuleBuilder myModuleBuilder = TestLibrary.Utilities.GetModuleBuilder(myAssembly, "Module1");

            TypeBuilder myTypeBuilder = myModuleBuilder.DefineType("HelloWorld", TypeAttributes.Public);
            ConstructorBuilder myConstructor = myTypeBuilder.DefineConstructor(
                     MethodAttributes.Public, CallingConventions.Standard, new Type[] { });
            ILGenerator myILGenerator = myConstructor.GetILGenerator();
            myILGenerator.Emit(OpCodes.Ldarg_1);
            Assert.Throws<ArgumentNullException>(() => { myConstructor.SetCustomAttribute(null); });
        }
    }
}
