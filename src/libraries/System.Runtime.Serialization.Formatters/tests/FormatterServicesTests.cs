// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public class FormatterServicesTests
    {
        [Fact]
        public void CheckTypeSecurity_Nop()
        {
            FormatterServices.CheckTypeSecurity(typeof(int), TypeFilterLevel.Full);
            FormatterServices.CheckTypeSecurity(typeof(int), TypeFilterLevel.Low);
        }

        [Fact]
        public void GetSerializableMembers_InvalidArguments_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>("type", () => FormatterServices.GetSerializableMembers(null));
        }

        [Fact]
        public void GetSerializableMembers_Interface()
        {
            Assert.Equal<MemberInfo>(new MemberInfo[0], FormatterServices.GetSerializableMembers(typeof(IDisposable)));
        }

        [Fact]
        public void GetUninitializedObject_InvalidArguments_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>("type", () => FormatterServices.GetUninitializedObject(null));
            Assert.Throws<ArgumentNullException>("type", () => FormatterServices.GetSafeUninitializedObject(null));
        }

        [Fact]
        public void GetUninitializedObject_DoesNotRunConstructor()
        {
            Assert.Equal(42, new ObjectWithDefaultCtor().Value);
            Assert.Equal(0, ((ObjectWithDefaultCtor)FormatterServices.GetUninitializedObject(typeof(ObjectWithDefaultCtor))).Value);
            Assert.Equal(0, ((ObjectWithDefaultCtor)FormatterServices.GetSafeUninitializedObject(typeof(ObjectWithDefaultCtor))).Value);
        }

        private class ObjectWithDefaultCtor
        {
            public int Value = 42;
        }

        [Fact]
        public void PopulateObjectMembers_InvalidArguments_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>("obj", () => FormatterServices.PopulateObjectMembers(null, new MemberInfo[0], new object[0]));
            Assert.Throws<ArgumentNullException>("members", () => FormatterServices.PopulateObjectMembers(new object(), null, new object[0]));
            Assert.Throws<ArgumentNullException>("data", () => FormatterServices.PopulateObjectMembers(new object(), new MemberInfo[0], null));
            Assert.Throws<ArgumentException>(() => FormatterServices.PopulateObjectMembers(new object(), new MemberInfo[1], new object[2]));
            Assert.Throws<ArgumentNullException>("members", () => FormatterServices.PopulateObjectMembers(new object(), new MemberInfo[1], new object[1]));
            Assert.Throws<SerializationException>(() => FormatterServices.PopulateObjectMembers(new object(), new MemberInfo[] { typeof(object).GetMethod("GetHashCode") }, new object[] { new object() }));
        }

        [Fact]
        public void GetObjectData_InvalidArguments_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>("obj", () => FormatterServices.GetObjectData(null, new MemberInfo[0]));
            Assert.Throws<ArgumentNullException>("members", () => FormatterServices.GetObjectData(new object(), null));
            Assert.Throws<ArgumentNullException>("members", () => FormatterServices.GetObjectData(new object(), new MemberInfo[1]));
            Assert.Throws<SerializationException>(() => FormatterServices.GetObjectData(new object(), new MethodInfo[] { typeof(object).GetMethod("GetHashCode") }));
        }

        [Fact]
        public void GetSurrogateForCyclicalReference_InvalidArguments_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>("innerSurrogate", () => FormatterServices.GetSurrogateForCyclicalReference(null));
        }

        [Fact]
        public void GetSurrogateForCyclicalReference_ValidSurrogate_GetsObject()
        {
            var surrogate = new NonSerializablePairSurrogate();
            ISerializationSurrogate newSurrogate = FormatterServices.GetSurrogateForCyclicalReference(surrogate);
            Assert.NotNull(newSurrogate);
            Assert.NotSame(surrogate, newSurrogate);
        }

        [Fact]
        public void GetTypeFromAssembly_InvalidArguments_ThrowsException()
        {
            Assert.Throws<ArgumentNullException>("assem", () => FormatterServices.GetTypeFromAssembly(null, "name"));
            Assert.Null(FormatterServices.GetTypeFromAssembly(GetType().Assembly, Guid.NewGuid().ToString("N"))); // non-existing type doesn't throw
        }
    }
}
