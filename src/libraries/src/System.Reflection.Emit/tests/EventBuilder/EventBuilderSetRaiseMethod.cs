// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EventBuilderSetRaiseMethod
    {
        public delegate void TestEventHandler(object sender, object arg);
        
        [Fact]
        public void SetRaiseMethod_AbstractMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Abstract | MethodAttributes.Virtual);

            eventBuilder.SetRaiseMethod(method);
            eventBuilder.SetRaiseMethod(method);
        }

        [Fact]
        public void SetRaiseMethod_InstanceMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Public);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            eventBuilder.SetRaiseMethod(method);
            eventBuilder.SetRaiseMethod(method);
        }

        [Fact]
        public void SetRaiseMethod_StaticMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Static);
            ILGenerator ilGenerator = method.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            eventBuilder.SetRaiseMethod(method);
            eventBuilder.SetRaiseMethod(method);
        }

        [Fact]
        public void SetRaiseMethod_PInvokeImplMethod_Twice()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.PinvokeImpl);

            eventBuilder.SetRaiseMethod(method);
            eventBuilder.SetRaiseMethod(method);
        }

        [Fact]
        public void SetRaiseMethod_MultipleDifferentMethods()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method1 = type.DefineMethod("PInvokeMethod", MethodAttributes.PinvokeImpl);
            MethodBuilder method2 = type.DefineMethod("InstanceMethod", MethodAttributes.Public);
            ILGenerator ilGenerator = method2.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);
            MethodBuilder method3 = type.DefineMethod("StaticMethod", MethodAttributes.Static);
            MethodBuilder method4 = type.DefineMethod("AbstractMethod", MethodAttributes.Abstract | MethodAttributes.Virtual);

            eventBuilder.SetRaiseMethod(method1);
            eventBuilder.SetRaiseMethod(method2);
            eventBuilder.SetRaiseMethod(method3);
            eventBuilder.SetRaiseMethod(method4);
        }

        [Fact]
        public void SetRaiseMethod_NullMethod_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            AssertExtensions.Throws<ArgumentNullException>("mdBuilder", () => eventBuilder.SetRaiseMethod(null));
        }

        [Fact]
        public void SetRaiseMethod_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            MethodBuilder method = type.DefineMethod("TestMethod", MethodAttributes.Abstract | MethodAttributes.Virtual);
            type.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => eventBuilder.SetRaiseMethod(method));
        }
    }
}
