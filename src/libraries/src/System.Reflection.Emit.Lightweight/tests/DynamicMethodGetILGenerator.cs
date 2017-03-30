// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class DynamicMethodGetILGenerator1
    {
        private const string FieldName = "_id";

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetILGenerator_Int_Owner(bool skipVisibility)
        {
            IDClass target = new IDClass();
            FieldInfo field = typeof(IDClass).GetField(FieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Type[] paramTypes = new Type[] { typeof(IDClass), typeof(int) };
            DynamicMethod method = new DynamicMethod("Method", typeof(int), paramTypes, typeof(IDClass), skipVisibility);
            
            ILGenerator ilGenerator = method.GetILGenerator(8);
            Helpers.EmitMethodBody(ilGenerator, field);

            IntDelegate instanceCallBack = (IntDelegate)method.CreateDelegate(typeof(IntDelegate), target);
            VerifyILGenerator(instanceCallBack, target, 0);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The coreclr ignores the skipVisibility value of DynamicMethod.")]
        public void GetILGenerator_Int_Module_CoreclrIgnoresSkipVisibility()
        {
            GetILGenerator_Int_Module(skipVisibility: false);
        }

        [Theory]
        [InlineData(true)]
        public void GetILGenerator_Int_Module(bool skipVisibility)
        {
            Module module = typeof(IDClass).GetTypeInfo().Module;
            IDClass target = new IDClass();
            FieldInfo field = typeof(IDClass).GetField(FieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Type[] paramTypes = new Type[] { typeof(IDClass), typeof(int) };
            DynamicMethod method = new DynamicMethod("Method", typeof(int), paramTypes, module, skipVisibility);

            ILGenerator ilGenerator = method.GetILGenerator(8);
            Helpers.EmitMethodBody(ilGenerator, field);

            IntDelegate instanceCallBack = (IntDelegate)method.CreateDelegate(typeof(IntDelegate), target);
            VerifyILGenerator(instanceCallBack, target, 0);
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public void GetILGenerator_Owner(bool skipVisibility)
        {
            IDClass target = new IDClass();
            FieldInfo field = typeof(IDClass).GetField(FieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Type[] paramTypes = new Type[] { typeof(IDClass), typeof(int) };

            DynamicMethod method = new DynamicMethod("MethodName", typeof(int), paramTypes, typeof(IDClass), skipVisibility);

            ILGenerator ilGenerator = method.GetILGenerator();
            Helpers.EmitMethodBody(ilGenerator, field);

            IntDelegate instanceCallBack = (IntDelegate)method.CreateDelegate(typeof(IntDelegate), target);
            VerifyILGenerator(instanceCallBack, target, 0);
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "The coreclr ignores the skipVisibility value of DynamicMethod.")]
        public void GetILGenerator_Module_CoreclrIgnoresSkipVisibility()
        {
            GetILGenerator_Module(skipVisibility: false);
        }

        [Theory]
        [InlineData(true)]
        public void GetILGenerator_Module(bool skipVisibility)
        {
            Module module = typeof(TestClass).GetTypeInfo().Module;
            IDClass target = new IDClass();
            FieldInfo field = typeof(IDClass).GetField(FieldName, BindingFlags.Instance | BindingFlags.NonPublic);

            Type[] paramTypes = new Type[] { typeof(IDClass), typeof(int) };
            DynamicMethod method = new DynamicMethod("Method", typeof(int), paramTypes, module, skipVisibility);

            ILGenerator ilGenerator = method.GetILGenerator();
            Helpers.EmitMethodBody(ilGenerator, field);

            IntDelegate instanceCallBack = (IntDelegate)method.CreateDelegate(typeof(IntDelegate), target);
            VerifyILGenerator(instanceCallBack, target, 0);
        }

        private void VerifyILGenerator(IntDelegate instanceCallBack, IDClass target, int newId)
        {
            Assert.Equal(instanceCallBack(newId), target.ID);
            Assert.Equal(newId, target.ID);
        }
    }
}
