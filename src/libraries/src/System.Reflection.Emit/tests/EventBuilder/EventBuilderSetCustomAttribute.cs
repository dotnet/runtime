// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Linq;
using Xunit;

namespace System.Reflection.Emit.Tests
{
    public class EventBuilderSetCustomAttribute
    {
        public delegate void TestEventHandler(object sender, object arg);

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            ConstructorInfo atrtributeConstructor = typeof(EmptyAttribute).GetConstructor(new Type[0]);
            byte[] bytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();

            eventBuilder.SetCustomAttribute(atrtributeConstructor, bytes);
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullConstructorInfo_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder ev = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            AssertExtensions.Throws<ArgumentNullException>("con", () => ev.SetCustomAttribute(null, new byte[256]));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_NullByteArray_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            ConstructorInfo attributeConstructor = typeof(EmptyAttribute).GetConstructor(new Type[0]);

            AssertExtensions.Throws<ArgumentNullException>("binaryAttribute", () => eventBuilder.SetCustomAttribute(attributeConstructor, null));
        }

        [Fact]
        public void SetCustomAttribute_ConstructorInfo_ByteArray_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            ConstructorInfo attributeConstructor = typeof(EmptyAttribute).GetConstructor(new Type[0]);
            byte[] bytes = Enumerable.Range(0, 256).Select(i => (byte)i).ToArray();
            type.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => eventBuilder.SetCustomAttribute(attributeConstructor, bytes));
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            ConstructorInfo attributeConstructor = typeof(EmptyAttribute).GetConstructor(new Type[0]);
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(attributeConstructor, new object[0]);

            eventBuilder.SetCustomAttribute(attribute);
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder_NullBuilder_ThrowsArgumentNullException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            AssertExtensions.Throws<ArgumentNullException>("customBuilder", () => eventBuilder.SetCustomAttribute(null));
        }

        [Fact]
        public void SetCustomAttribute_CustomAttributeBuilder_TypeCreated_ThrowsInvalidOperationException()
        {
            TypeBuilder type = Helpers.DynamicType(TypeAttributes.Abstract);
            EventBuilder eventBuilder = type.DefineEvent("TestEvent", EventAttributes.None, typeof(TestEventHandler));
            ConstructorInfo attributeConstructor = typeof(EmptyAttribute).GetConstructor(new Type[0]);
            CustomAttributeBuilder attribute = new CustomAttributeBuilder(attributeConstructor, new object[0]);
            type.CreateTypeInfo().AsType();

            Assert.Throws<InvalidOperationException>(() => eventBuilder.SetCustomAttribute(attribute));
        }
    }
}
