// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class TypeBuilderDefineEvent
    {
        public static IEnumerable<object[]> TestData()
        {
            yield return new object[] { "TestEvent", EventAttributes.None, typeof(int), "TestEvent", EventAttributes.None };
            yield return new object[] { "a\0b\0c", EventAttributes.RTSpecialName, typeof(void), "a", EventAttributes.None };
            yield return new object[] { "\uD800\uDC00", EventAttributes.SpecialName, typeof(Delegate), "\uD800\uDC00", EventAttributes.SpecialName };
            yield return new object[] { "\u043F\u0440\u0438\u0432\u0435\u0442", EventAttributes.SpecialName | EventAttributes.RTSpecialName, typeof(EmptyGenericStruct<>), "\u043F\u0440\u0438\u0432\u0435\u0442", EventAttributes.SpecialName };
            yield return new object[] { "class", (EventAttributes)(-1), typeof(string), "class", EventAttributes.None };
            yield return new object[] { "Test Name With Spaces", EventAttributes.None, typeof(BasicDelegate), "Test Name With Spaces", EventAttributes.None };
            yield return new object[] { "TestEvent", EventAttributes.None, typeof(EmptyGenericStruct<int>), "TestEvent", EventAttributes.None };
            yield return new object[] { "TestEvent", EventAttributes.None, typeof(EmptyGenericStruct<int>).GetGenericArguments()[0], "TestEvent", EventAttributes.None };
            yield return new object[] { "TestEvent", EventAttributes.None, typeof(EmptyGenericStruct<>).GetGenericArguments()[0], "TestEvent", EventAttributes.None };
            yield return new object[] { "TestEvent", EventAttributes.None, Helpers.DynamicType(TypeAttributes.Public).AsType(), "TestEvent", EventAttributes.None };
            yield return new object[] { "\uDC00", (EventAttributes)0x8000, typeof(int), "\uFFFD", (EventAttributes)0x8000 };
            yield return new object[] { "\uD800", EventAttributes.None, typeof(int), "\uFFFD", EventAttributes.None };
            yield return new object[] { "1A\0\t\v\r\n\n\uDC81\uDC91", EventAttributes.None, typeof(int).MakePointerType(), "1A", EventAttributes.None };
        }

        [Theory]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/2389", TestRuntimes.Mono)]
        [MemberData(nameof(TestData))]
        public void DefineEvent(string name, EventAttributes attributes, Type eventType, string expectedName, EventAttributes expectedAttributes)
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.DefineGenericParameters("T");

            EventBuilder eventBuilder = type.DefineEvent(name, attributes, eventType);
            MethodBuilder addOnMethod = type.DefineMethod("addOnMethod", MethodAttributes.Public);
            addOnMethod.GetILGenerator().Emit(OpCodes.Ret);

            MethodBuilder removeOnMethod = type.DefineMethod("removeOnMethod", MethodAttributes.Public);
            removeOnMethod.GetILGenerator().Emit(OpCodes.Ret);

            eventBuilder.SetAddOnMethod(addOnMethod);
            eventBuilder.SetRemoveOnMethod(removeOnMethod);

            Type createdType = type.CreateType();
            Assert.Equal(type.AsType().GetEvents(Helpers.AllFlags), createdType.GetEvents(Helpers.AllFlags));
            Assert.Equal(type.AsType().GetEvent(expectedName, Helpers.AllFlags), createdType.GetEvent(expectedName, Helpers.AllFlags));

            EventInfo eventInfo = createdType.GetEvent(expectedName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            Assert.Equal(expectedName, eventInfo.Name);
            Assert.Equal(createdType, eventInfo.DeclaringType);
            Assert.Equal(expectedAttributes, eventInfo.Attributes);
            Assert.Equal((expectedAttributes & EventAttributes.SpecialName) != 0, eventInfo.IsSpecialName);
            Assert.Null(eventInfo.EventHandlerType);
        }

        [Fact]
        public void DefineEvent_CalledMultipleTimes_Works()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);

            for (int i = 0; i < 2; i++)
            {
                EventBuilder eventBuilder = type.DefineEvent("EventName", EventAttributes.None, typeof(int));
                MethodBuilder addOnMethod = type.DefineMethod("addOnMethod", MethodAttributes.Public);
                ILGenerator ilGenerator = addOnMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ret);

                MethodBuilder removeOnMethod = type.DefineMethod("removeOnMethod", MethodAttributes.Public);
                ilGenerator = removeOnMethod.GetILGenerator();
                ilGenerator.Emit(OpCodes.Ret);

                eventBuilder.SetAddOnMethod(addOnMethod);
                eventBuilder.SetRemoveOnMethod(removeOnMethod);
            }

            Type createdType = type.CreateType();
            EventInfo[] events = createdType.GetEvents();
            Assert.Equal(1, events.Length);
            Assert.Equal("EventName", events[0].Name);
        }

        [Fact]
        public void DefineEvent_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.DefineGenericParameters("T");

            type.DefineEvent("TestEvent", EventAttributes.None, typeof(int));
            type.CreateType();
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

        [Fact]
        public void DefineEvent_ByRefEventType()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Class | TypeAttributes.Public);
            type.DefineEvent("Name", EventAttributes.None, typeof(int).MakeByRefType());
            type.CreateType();
        }

        [Fact]
        public void GetEvent_TypeNotCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Throws<NotSupportedException>(() => type.AsType().GetEvent("Any"));
        }

        [Fact]
        public void GetEvents_TypeNotCreated_ThrowsNotSupportedException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Public);
            Assert.Throws<NotSupportedException>(() => type.AsType().GetEvents());
        }
    }
}
