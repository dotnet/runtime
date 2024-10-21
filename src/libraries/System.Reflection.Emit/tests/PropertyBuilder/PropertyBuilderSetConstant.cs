// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class PropertyBuilderTest11
    {
        internal enum Colors
        {
            Red = 0,
            Green = 1,
            Blue = 2
        }

        public static IEnumerable<object[]> SetConstant_TestData()
        {
            yield return new object[] { typeof(int), 10 };
            yield return new object[] { typeof(bool), true };
            yield return new object[] { typeof(sbyte), (sbyte)10 };
            yield return new object[] { typeof(short), (short)10 };
            yield return new object[] { typeof(long), (long)10 };

            yield return new object[] { typeof(byte), (byte)10 };
            yield return new object[] { typeof(ushort), (ushort)10 };
            yield return new object[] { typeof(uint), (uint)10 };
            yield return new object[] { typeof(ulong), (ulong)10 };

            yield return new object[] { typeof(float), (float)10 };
            yield return new object[] { typeof(double), (double)10 };

            yield return new object[] { typeof(DateTime), DateTime.Now };
            yield return new object[] { typeof(char), 'a' };
            yield return new object[] { typeof(string), "a" };

            yield return new object[] { typeof(Colors), Colors.Blue  };
            yield return new object[] { typeof(object), null };
            yield return new object[] { typeof(object), "a" };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(SetConstant_TestData))]
        public void SetConstant(Type returnType, object defaultValue)
        {
            MethodAttributes getMethodAttributes = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;
            BindingFlags bindingAttributes = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Static;
            Type[] paramTypes = new Type[0];

            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);

            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, returnType, null);
            property.SetConstant(defaultValue);

            MethodBuilder method = type.DefineMethod("TestMethod", getMethodAttributes, returnType, paramTypes);
            ILGenerator methodILGenerator = method.GetILGenerator();
            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ret);

            property.SetGetMethod(method);

            Type createdType = type.CreateType();
            PropertyInfo createdProperty = createdType.GetProperty("TestProperty", bindingAttributes);
            Assert.Equal(defaultValue, createdProperty.GetConstantValue());
        }

        [Fact]
        public void SetConstant_TypeAlreadyCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.None, typeof(int), new Type[0]);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public, CallingConventions.HasThis, typeof(int), new Type[] { typeof(int) });

            ILGenerator methodILGenerator = method.GetILGenerator();
            methodILGenerator.Emit(OpCodes.Ldarg_0);
            methodILGenerator.Emit(OpCodes.Ldarg_1);
            methodILGenerator.Emit(OpCodes.Ret);

            type.CreateType();
            Assert.Throws<InvalidOperationException>(() => property.SetConstant(1));
        }

        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        public void SetConstant_TypeNotConstant_ThrowsArgumentException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);

            PropertyBuilder property = type.DefineProperty("TestProperty", PropertyAttributes.HasDefault, typeof(decimal), null);
            AssertExtensions.Throws<ArgumentException>(null, () => property.SetConstant((decimal)10));
        }
    }
}
