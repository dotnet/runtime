// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class DynamicMethodCreateDelegateTests
    {
        private const string FieldName = "_id";

        public static IEnumerable<object[]> Targets_TestData()
        {
            yield return new object[] { new IDClass() };
            yield return new object[] { new IDSubClass() };
        }

        [Theory]
        [MemberData(nameof(Targets_TestData))]
        public void CreateDelegate_Target_Type(IDClass target)
        {
            int newId = 0;

            FieldInfo field = typeof(IDClass).GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            DynamicMethod method = new DynamicMethod("Method", typeof(int), new Type[] { typeof(IDClass), typeof(int) }, typeof(IDClass));

            ILGenerator ilGenerator = method.GetILGenerator();
            Helpers.EmitMethodBody(ilGenerator, field);

            IntDelegate instanceCallBack = (IntDelegate)method.CreateDelegate(typeof(IntDelegate), target);
            Assert.Equal(instanceCallBack(newId), target.ID);
            Assert.Equal(newId, target.ID);
        }

        [Theory]
        [MemberData(nameof(Targets_TestData))]
        public void CreateDelegate_Target_Module(IDClass target)
        {
            Module module = typeof(TestClass).GetTypeInfo().Module;
            int newId = 0;

            FieldInfo field = typeof(IDClass).GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            DynamicMethod method = new DynamicMethod("Method", typeof(int), new Type[] { typeof(IDClass), typeof(int) }, module, true);

            ILGenerator ilGenerator = method.GetILGenerator();
            Helpers.EmitMethodBody(ilGenerator, field);

            IntDelegate instanceCallBack = (IntDelegate)method.CreateDelegate(typeof(IntDelegate), target);
            Assert.Equal(instanceCallBack(newId), target.ID);
            Assert.Equal(newId, target.ID);
        }

        [Theory]
        [MemberData(nameof(Targets_TestData))]
        public void CreateDelegate_Type(IDClass target)
        {
            int newId = 0;

            FieldInfo field = typeof(IDClass).GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            DynamicMethod method = new DynamicMethod("Method", typeof(int), new Type[] { typeof(IDClass), typeof(int) }, typeof(IDClass));
            ILGenerator ilGenerator = method.GetILGenerator();
            Helpers.EmitMethodBody(ilGenerator, field);

            IDClassDelegate staticCallBack = (IDClassDelegate)method.CreateDelegate(typeof(IDClassDelegate));
            Assert.Equal(staticCallBack(target, newId), target.ID);
            Assert.Equal(newId, target.ID);
        }

        [Theory]
        [MemberData(nameof(Targets_TestData))]
        public void CreateDelegate_Module(IDClass target)
        {
            Module module = typeof(TestClass).GetTypeInfo().Module;
            int newId = 0;

            FieldInfo field = typeof(IDClass).GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance);

            DynamicMethod method = new DynamicMethod("Method", typeof(int), new Type[] { typeof(IDClass), typeof(int) }, module, true);
            ILGenerator ilGenerator = method.GetILGenerator();
            Helpers.EmitMethodBody(ilGenerator, field);

            IDClassDelegate staticCallBack = (IDClassDelegate)method.CreateDelegate(typeof(IDClassDelegate));
            Assert.Equal(staticCallBack(target, newId), target.ID);
            Assert.Equal(newId, target.ID);
        }

        [Fact]
        public void CreateDelegate_NoMethodBody_ThrowsInvalidOperationException()
        {
            IDClass target = new IDClass();
            DynamicMethod method = new DynamicMethod("Method", typeof(int), new Type[] { typeof(IDClass), typeof(int) }, typeof(IDClass));

            Assert.Throws<InvalidOperationException>(() => method.CreateDelegate(typeof(IntDelegate)));
            Assert.Throws<InvalidOperationException>(() => method.CreateDelegate(typeof(IntDelegate), target));
        }

        [Fact]
        public void CreateDelegate_InvalidTarget_ThrowsArgumentException()
        {
            FieldInfo field = typeof(IDClass).GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            DynamicMethod method = new DynamicMethod("Method", typeof(int), new Type[] { typeof(IDClass), typeof(int) }, typeof(IDClass));

            ILGenerator ilGenerator = method.GetILGenerator();
            Helpers.EmitMethodBody(ilGenerator, field);

            AssertExtensions.Throws<ArgumentException>(null, () => method.CreateDelegate(typeof(IntDelegate), "foo"));
        }

        [Theory]
        [InlineData(typeof(InvalidRetType))]
        [InlineData(typeof(WrongParamNumber))]
        [InlineData(typeof(InvalidParamType))]
        public void CreateDelegate_DelegateTypeInvalid_ThrowsArgumentException(Type delegateType)
        {
            FieldInfo field = typeof(IDClass).GetField(FieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            DynamicMethod method = new DynamicMethod("Method", typeof(int), new Type[] { typeof(IDClass), typeof(int) }, typeof(IDClass));

            ILGenerator ilGenerator = method.GetILGenerator();
            Helpers.EmitMethodBody(ilGenerator, field);

            AssertExtensions.Throws<ArgumentException>(null, () => method.CreateDelegate(delegateType));
            AssertExtensions.Throws<ArgumentException>(null, () => method.CreateDelegate(delegateType, new IDClass()));
        }

        /// <summary>
        /// Reproduces https://github.com/dotnet/runtime/issues/78365
        /// </summary>
        [Fact]
        public void CreateDelegate_CanBeConvertedToAnotherDelegateType()
        {
            DynamicMethod dynamicMethod = new("GetLength", typeof(int), new[] { typeof(string) });
            ILGenerator il = dynamicMethod.GetILGenerator();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Callvirt, typeof(string).GetProperty(nameof(string.Length))!.GetMethod);
            il.Emit(OpCodes.Ret);

            Func<string, int> getLength = dynamicMethod.CreateDelegate<Func<string, int>>();
            Assert.Equal(2, getLength("bb"));

            Func<int> getTargetLength = getLength.Method.CreateDelegate<Func<int>>("ccc");
            Assert.Equal(3, getTargetLength());

            Assert.Equal(getLength, getTargetLength.Method.CreateDelegate<Func<string, int>>());
        }
    }

    public class IDSubClass : IDClass
    {
        public IDSubClass(int id) : base(id) { }
        public IDSubClass() : base() { }
    }

    public delegate int IDClassDelegate(IDClass owner, int id);
    public delegate IDClass InvalidRetType(int id);
    public delegate int WrongParamNumber(int id, int m);
    public delegate int InvalidParamType(IDClass owner);
}
