// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class MethodBuilderToString
    {
        [Fact]
        public void ToString_AllFieldsSet()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);

            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            GenericTypeParameterBuilder[] typeParameters = method.DefineGenericParameters("T");
            GenericTypeParameterBuilder returnType = typeParameters[0];

            method.SetSignature(returnType.AsType(), null, null, null, null, null);
            Assert.Contains(ExpectedToStrin(method), method.ToString());
        }

        [Fact]
        public void ToString_NameAndAttributeSet()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);

            Assert.Contains(ExpectedToStrin(method), method.ToString());
        }

        [Fact]
        public void ToString_NameAttributeAndSignatureSetSet()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);

            method.SetSignature(typeof(void), null, null, null, null, null);
            Assert.Contains(ExpectedToStrin(method), method.ToString());
        }

        [Fact]
        public void ToString_NonGenericMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("method1", MethodAttributes.Public | MethodAttributes.Static, typeof(int), new Type[0]);

            string toString = method.ToString();
            Assert.True(toString.LastIndexOf("Name: method1") != -1 &&
                toString.LastIndexOf("Attributes: 22") != -1 &&
                toString.LastIndexOf("Method Signature: Length: 3") != -1 &&
                toString.LastIndexOf("Arguments: 0") != -1 &&
                toString.LastIndexOf("Signature:") != -1 &&
                toString.LastIndexOf("0  0  8  0") != -1);
        }

        [Fact]
        public void ToString_GenericMethod()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            MethodBuilder method = type.DefineMethod("method1", MethodAttributes.Public, typeof(int), new Type[0]);
            method.DefineGenericParameters("T", "U", "V");
            method.MakeGenericMethod(typeof(string), typeof(int), typeof(object));

            string toString = method.ToString();
            Assert.True(toString.LastIndexOf("Name: method1") != -1 &&
                toString.LastIndexOf("Attributes: 6") != -1 &&
                toString.LastIndexOf("Method Signature: Length: 4") != -1 &&
                toString.LastIndexOf("Arguments: 0") != -1 &&
                toString.LastIndexOf("Signature:") != -1 &&
                toString.LastIndexOf("48  3  0  8  0") != -1);
        }

        private static string ExpectedToStrin(MethodBuilder method)
        {
            return "Name: " + method.Name + " " + Environment.NewLine +
                "Attributes: " + ((int)method.Attributes).ToString() + Environment.NewLine +
                "Method Signature: ";
        }
    }
}
