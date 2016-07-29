// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class DynamicMethodctor1
    {
        [Theory]
        [InlineData("Method", typeof(void), null)]
        [InlineData("Method", typeof(void), new Type[] { typeof(int), typeof(string) })]
        [InlineData("Method", typeof(string), null)]
        [InlineData("Method", typeof(string), new Type[] { typeof(int), typeof(string) })]
        [InlineData("", typeof(string), new Type[] { typeof(int), typeof(string) })]
        [InlineData("Method", typeof(char?), null)]
        [InlineData("Method", typeof(TestClass), null)]
        [InlineData("Method", typeof(GenericClass<>), null)]
        [InlineData("Method", typeof(TestInterface), null)]
        [InlineData("Method", null, null)]
        [InlineData("Method", typeof(int), new Type[] { typeof(int) })]
        [InlineData("method", typeof(string), new Type[] { typeof(char?) })]
        [InlineData("Method", typeof(string), new Type[] { typeof(GenericClass2<,>), typeof(GenericClass2<,>) })]
        [InlineData("Method", typeof(string), new Type[] { typeof(TestInterface) })]
        public void String_Type_TypeArray_Module(string name, Type returnType, Type[] parameterTypes)
        {
            Module module = typeof(TestClass).GetTypeInfo().Module;

            DynamicMethod method1 = new DynamicMethod(name, returnType, parameterTypes, module);
            Helpers.VerifyMethod(method1, name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, module);

            DynamicMethod method2 = new DynamicMethod(name, returnType, parameterTypes, module, true);
            Helpers.VerifyMethod(method2, name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, module);

            DynamicMethod method3 = new DynamicMethod(name, returnType, parameterTypes, module, false);
            Helpers.VerifyMethod(method3, name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, module);

            DynamicMethod method4 = new DynamicMethod(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, module, true);
            Helpers.VerifyMethod(method4, name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, module);

            DynamicMethod method5 = new DynamicMethod(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, module, false);
            Helpers.VerifyMethod(method5, name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, module);
        }

        [Theory]
        [InlineData("Method", typeof(void), null, typeof(TestClass))]
        [InlineData("Method", typeof(void), new Type[] { typeof(int), typeof(string) }, typeof(TestClass))]
        [InlineData("Method", typeof(string), null, typeof(TestClass))]
        [InlineData("Method", typeof(string), new Type[] { typeof(int), typeof(string) }, typeof(TestClass))]
        [InlineData("", typeof(string), new Type[] { typeof(int), typeof(string) }, typeof(TestClass))]
        public void String_Type_TypeArray_Type(string name, Type returnType, Type[] parameterTypes, Type owner)
        {
            DynamicMethod method1 = new DynamicMethod(name, returnType, parameterTypes, owner);
            Helpers.VerifyMethod(method1, name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner.GetTypeInfo().Module);

            DynamicMethod method2 = new DynamicMethod(name, returnType, parameterTypes, owner, true);
            Helpers.VerifyMethod(method2, name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner.GetTypeInfo().Module);

            DynamicMethod method3 = new DynamicMethod(name, returnType, parameterTypes, owner, false);
            Helpers.VerifyMethod(method3, name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner.GetTypeInfo().Module);

            DynamicMethod method4 = new DynamicMethod(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner, true);
            Helpers.VerifyMethod(method4, name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner.GetTypeInfo().Module);

            DynamicMethod method5 = new DynamicMethod(name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner, false);
            Helpers.VerifyMethod(method5, name, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, returnType, parameterTypes, owner.GetTypeInfo().Module);
        }

        [Fact]
        public void NullObjectInParameterTypes_ThrowsArgumentException()
        {
            Module module = typeof(TestClass).GetTypeInfo().Module;

            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", typeof(void), new Type[] { null, typeof(string) }, module));

            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", typeof(void), new Type[] { null, typeof(string) }, module, true));
            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", typeof(void), new Type[] { null, typeof(string) }, module, false));

            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[] { null, typeof(string) }, module, true));
            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[] { null, typeof(string) }, module, false));

            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", typeof(void), new Type[] { null, typeof(string) }, typeof(TestClass)));

            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", typeof(void), new Type[] { null, typeof(string) }, typeof(TestClass), true));
            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", typeof(void), new Type[] { null, typeof(string) }, typeof(TestClass), false));

            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[] { null, typeof(string) }, typeof(TestClass), true));
            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[] { null, typeof(string) }, typeof(TestClass), false));
        }

        [Fact]
        public void NullName_ThrowsArgumentNullException()
        {
            Module module = typeof(TestClass).GetTypeInfo().Module;

            Assert.Throws<ArgumentNullException>("name", () => new DynamicMethod(null, typeof(void), new Type[0], module));

            Assert.Throws<ArgumentNullException>("name", () => new DynamicMethod(null, typeof(void), new Type[0], module, true));
            Assert.Throws<ArgumentNullException>("name", () => new DynamicMethod(null, typeof(void), new Type[0], module, false));

            Assert.Throws<ArgumentNullException>("name", () => new DynamicMethod(null, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[0], module, true));
            Assert.Throws<ArgumentNullException>("name", () => new DynamicMethod(null, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[0], module, false));

            Assert.Throws<ArgumentNullException>("name", () => new DynamicMethod(null, typeof(void), new Type[0], typeof(TestClass)));

            Assert.Throws<ArgumentNullException>("name", () => new DynamicMethod(null, typeof(void), new Type[0], typeof(TestClass), true));
            Assert.Throws<ArgumentNullException>("name", () => new DynamicMethod(null, typeof(void), new Type[0], typeof(TestClass), false));

            Assert.Throws<ArgumentNullException>("name", () => new DynamicMethod(null, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[0], typeof(TestClass), true));
            Assert.Throws<ArgumentNullException>("name", () => new DynamicMethod(null, MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[0], typeof(TestClass), false));
        }

        [Fact]
        public void ByRefReturnType_ThrowsNotSupportedException()
        {
            Module module = typeof(TestClass).GetTypeInfo().Module;
            Assert.Throws<NotSupportedException>(() => new DynamicMethod("Method", typeof(int).MakeByRefType(), new Type[0], module));

            Assert.Throws<NotSupportedException>(() => new DynamicMethod("Method", typeof(int).MakeByRefType(), new Type[0], module, true));
            Assert.Throws<NotSupportedException>(() => new DynamicMethod("Method", typeof(int).MakeByRefType(), new Type[0], module, false));

            Assert.Throws<NotSupportedException>(() => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(int).MakeByRefType(), new Type[0], module, true));
            Assert.Throws<NotSupportedException>(() => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(int).MakeByRefType(), new Type[0], module, false));

            Assert.Throws<NotSupportedException>(() => new DynamicMethod("Method", typeof(int).MakeByRefType(), new Type[0], typeof(TestClass)));

            Assert.Throws<NotSupportedException>(() => new DynamicMethod("Method", typeof(int).MakeByRefType(), new Type[0], typeof(TestClass), true));
            Assert.Throws<NotSupportedException>(() => new DynamicMethod("Method", typeof(int).MakeByRefType(), new Type[0], typeof(TestClass), false));

            Assert.Throws<NotSupportedException>(() => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(int).MakeByRefType(), new Type[0], typeof(TestClass), true));
            Assert.Throws<NotSupportedException>(() => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(int).MakeByRefType(), new Type[0], typeof(TestClass), false));
        }

        [Fact]
        public void NullModule_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("m", () => new DynamicMethod("Method", typeof(void), new Type[0], (Module)null));

            Assert.Throws<ArgumentNullException>("m", () => new DynamicMethod("Method", typeof(void), new Type[0], (Module)null, true));
            Assert.Throws<ArgumentNullException>("m", () => new DynamicMethod("Method", typeof(void), new Type[0], (Module)null, false));

            Assert.Throws<ArgumentNullException>("m", () => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[0], (Module)null, true));
            Assert.Throws<ArgumentNullException>("m", () => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[0], (Module)null, false));
        }

        [Fact]
        public void NullOwner_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>("owner", () => new DynamicMethod("Method", typeof(void), new Type[0], (Type)null));

            Assert.Throws<ArgumentNullException>("owner", () => new DynamicMethod("Method", typeof(void), new Type[0], (Type)null, true));
            Assert.Throws<ArgumentNullException>("owner", () => new DynamicMethod("Method", typeof(void), new Type[0], (Type)null, false));

            Assert.Throws<ArgumentNullException>("owner", () => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[0], (Type)null, true));
            Assert.Throws<ArgumentNullException>("owner", () => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[0], (Type)null, false));
        }

        [Theory]
        [InlineData(typeof(int[]))]
        [InlineData(typeof(TestInterface))]
        [InlineData(typeof(GenericClass<>))]
        [InlineData(typeof(int*))]
        public void InvalidOwner_ThrowsArgumentException(Type owner)
        {
            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", typeof(void), new Type[0], owner));

            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", typeof(void), new Type[0], owner, true));
            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", typeof(void), new Type[0], owner, false));

            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[0], owner, true));
            Assert.Throws<ArgumentException>(null, () => new DynamicMethod("Method", MethodAttributes.Public | MethodAttributes.Static, CallingConventions.Standard, typeof(void), new Type[0], owner, false));
        }
    }
}
