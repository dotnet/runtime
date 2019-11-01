// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EventBuilderAddOtherMethod
    {
        public delegate void TestEventHandler(object sender, object arg);

        [Fact]
        public void AddOtherMethod_AbstractVirtualMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Abstract | MethodAttributes.Virtual);

            eventBuilder.AddOtherMethod(method);
            eventBuilder.AddOtherMethod(method);
        }

        [Fact]
        public void AddOtherMethod_InstanceMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            eventBuilder.AddOtherMethod(method);
            eventBuilder.AddOtherMethod(method);
        }

        [Fact]
        public void AddOtherMethod_StaticMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Static);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            eventBuilder.AddOtherMethod(method);
            eventBuilder.AddOtherMethod(method);
        }

        [Fact]
        public void AddOtherMethod_PInvokeImpl_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.PinvokeImpl);

            eventBuilder.AddOtherMethod(method);
            eventBuilder.AddOtherMethod(method);
        }

        [Fact]
        public void AddOtherMethod_MultipleDifferentMethods()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method1 = type.DefineMethod("PInvokeMethod", MethodAttributes.PinvokeImpl);
            MethodBuilder method2 = type.DefineMethod("PublicMethod", MethodAttributes.Public);
            ILGenerator ilGenerator = method2.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            MethodBuilder method3 = type.DefineMethod("StaticMethod", MethodAttributes.Static);
            MethodBuilder method4 = type.DefineMethod("AbstractMethod", MethodAttributes.Abstract | MethodAttributes.Virtual);

            eventBuilder.AddOtherMethod(method1);
            eventBuilder.AddOtherMethod(method2);
            eventBuilder.AddOtherMethod(method3);
            eventBuilder.AddOtherMethod(method4);
        }

        [Fact]
        public void AddOtherMethod_NullMethod_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => eventBuilder.AddOtherMethod(null));
        }

        [Fact]
        public void AddOtherMethod_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Abstract | MethodAttributes.Virtual);
            type.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => eventBuilder.AddOtherMethod(method));
        }
    }
}
