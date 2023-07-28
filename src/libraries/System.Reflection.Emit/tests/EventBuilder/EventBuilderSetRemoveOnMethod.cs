// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EventBuilderSetRemoveOnMethod
    {
        public delegate void TestEventHandler(object sender, object arg);

        [Fact]
        public void SetRemoveOnMethod_AbstractMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Abstract | MethodAttributes.Virtual);

            eventBuilder.SetRemoveOnMethod(method);
            eventBuilder.SetRemoveOnMethod(method);
        }

        [Fact]
        public void SetRemoveOnMethod_InstanceMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            eventBuilder.SetRemoveOnMethod(method);
            eventBuilder.SetRemoveOnMethod(method);
        }


        [Fact]
        public void SetRemoveOnMethod_StaticMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Static);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            eventBuilder.SetRemoveOnMethod(method);
            eventBuilder.SetRemoveOnMethod(method);
        }

        [Fact]
        public void SetRemoveOnMethod_PInvokeImplMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.PinvokeImpl);

            eventBuilder.SetRemoveOnMethod(method);
            eventBuilder.SetRemoveOnMethod(method);
        }

        [Fact]
        public void SetRemoveOnMethod_MultipleDifferentMethods()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method1 = type.DefineMethod("PInvokeMethod", MethodAttributes.PinvokeImpl);
            MethodBuilder method2 = type.DefineMethod("InstanceMethod", MethodAttributes.Public);
            ILGenerator ilGenerator = method2.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            MethodBuilder method3 = type.DefineMethod("StaticMethod", MethodAttributes.Static);
            MethodBuilder method4 = type.DefineMethod("AbstractMethod", MethodAttributes.Abstract | MethodAttributes.Virtual);

            eventBuilder.SetRemoveOnMethod(method1);
            eventBuilder.SetRemoveOnMethod(method2);
            eventBuilder.SetRemoveOnMethod(method3);
            eventBuilder.SetRemoveOnMethod(method4);
        }

        [Fact]
        public void SetRemoveOnMethod_NullMethod_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => eventBuilder.SetRemoveOnMethod(null));
        }

        [Fact]
        public void SetRemoveOnMethod_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Abstract | MethodAttributes.Virtual);
            type.CreateType();

            Assert.Throws<InvalidOperationException>(() => { eventBuilder.SetRemoveOnMethod(method); });
        }
    }
}
