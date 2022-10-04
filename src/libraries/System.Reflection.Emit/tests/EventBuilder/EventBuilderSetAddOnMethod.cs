// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EventBuilderSetAddOnMethod
    {
        public delegate void TestEventHandler(object sender, object arg);

        [Fact]
        public void SetAddOnMethod_AbstractMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Abstract | MethodAttributes.Virtual);

            eventBuilder.SetAddOnMethod(method);
            eventBuilder.SetAddOnMethod(method);
        }


        [Fact]
        public void SetAddOnMethod_InstanceMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            eventBuilder.SetAddOnMethod(method);
            eventBuilder.SetAddOnMethod(method);
        }

        [Fact]
        public void SetAddOnMethod_StaticMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Static);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            eventBuilder.SetAddOnMethod(method);
            eventBuilder.SetAddOnMethod(method);
        }

        [Fact]
        public void SetAddOnMethod_PInvokeImplMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.PinvokeImpl);

            eventBuilder.SetAddOnMethod(method);
            eventBuilder.SetAddOnMethod(method);
        }

        [Fact]
        public void SetAddOnMethod_MultipleDifferentMethods()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method1 = type.DefineMethod("PInvokeMethod", MethodAttributes.PinvokeImpl);
            MethodBuilder method2 = type.DefineMethod("InstanceMethod", MethodAttributes.Public);
            ILGenerator ilGenerator = method2.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            MethodBuilder method3 = type.DefineMethod("StaticMethod", MethodAttributes.Static);
            MethodBuilder method4 = type.DefineMethod("AbstractMethod", MethodAttributes.Abstract | MethodAttributes.Virtual);

            eventBuilder.SetAddOnMethod(method1);
            eventBuilder.SetAddOnMethod(method2);
            eventBuilder.SetAddOnMethod(method3);
            eventBuilder.SetAddOnMethod(method4);
        }

        [Fact]
        public void SetAddOnMethod_NullMethod_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => eventBuilder.SetAddOnMethod(null));
        }

        [Fact]
        public void SetAddOnMethod_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Abstract | MethodAttributes.Virtual);
            type.CreateType();

            Assert.Throws<InvalidOperationException>(() => eventBuilder.SetAddOnMethod(method));
        }
    }
}
