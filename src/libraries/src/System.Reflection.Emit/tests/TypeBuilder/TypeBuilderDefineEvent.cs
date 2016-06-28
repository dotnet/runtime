// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineEvent
    {
        [Fact]
        public void DefineEvent()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.DefineGenericParameters("T");

            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(int));
            MethodBuilder addOnMethod = type.DefineMethod("addOnMethod", MethodAttributes.Public);
            ILGenerator ilGenerator = addOnMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            MethodBuilder removeOnMethod = type.DefineMethod("removeOnMethod", MethodAttributes.Public);
            ilGenerator = removeOnMethod.GetILGenerator();
            ilGenerator.Emit(OpCodes.Ret);

            eventBuilder.SetAddOnMethod(addOnMethod);
            eventBuilder.SetRemoveOnMethod(removeOnMethod);

            Type createdType = type.CreateTypeInfo().AsType();
            Assert.NotNull(createdType.GetEvent("TestEvent", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static));
        }

        [Fact]
        public void DefineEvent_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.DefineGenericParameters("T");

            type.DefineEvent("TestEvent", EventAttributes.None, typeof(int));
            type.CreateTypeInfo().AsType();
            Assert.Throws<InvalidOperationException>(() => type.DefineEvent("TestEvent2", EventAttributes.None, typeof(int)));
        }

        [Theory]
        [InlineData(null, typeof(int), typeof(ArgumentNullException))]
        [InlineData("TestEvent", null, typeof(ArgumentNullException))]
        [InlineData("", typeof(int), typeof(ArgumentException))]
        [InlineData("\0TestEvent", typeof(int), typeof(ArgumentException))]
        public void DefineEvent_Invalid(string name, Type eventType, Type exceptionType)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.DefineGenericParameters("T");

            Assert.Throws(exceptionType, () => type.DefineEvent(name, EventAttributes.None, eventType));
        }
    }
}
