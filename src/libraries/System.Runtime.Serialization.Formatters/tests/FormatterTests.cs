// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public class FormatterTests
    {
        [Fact]
        public void DefaultCtor_ObjectsInitialized()
        {
            var f = new TestFormatter();

            Assert.NotNull(f.m_idGenerator);
            Assert.NotNull(f.m_objectQueue);
            Assert.Equal(0, f.m_objectQueue.Count);

            bool firstTime;
            AssertExtensions.Throws<ArgumentNullException>("obj", () => f.m_idGenerator.GetId(null, out firstTime));
            AssertExtensions.Throws<ArgumentNullException>("obj", () => f.m_idGenerator.HasId(null, out firstTime));
        }

        [Fact]
        public void ScheduleAndGetObjects_ExpectedIDsAndOrder()
        {
            var f = new TestFormatter();

            // null values don't count
            long actualId;
            Assert.Equal(0, f.Schedule(null));
            Assert.Equal(0, f.Schedule(null));
            Assert.Null(f.GetNext(out actualId));
            Assert.Equal(0, actualId);

            var objects = new object[] { new object(), new object(), new object() };

            // Add each object for the first time
            long nextExpectedId = 1;
            foreach (var obj in objects)
            {
                Assert.Equal(nextExpectedId++, f.Schedule(obj));
            }

            // Adding them again should produce the same IDs
            nextExpectedId = 1;
            foreach (var obj in objects)
            {
                Assert.Equal(nextExpectedId++, f.Schedule(obj));
            }

            // Now retrieve them all
            nextExpectedId = 1;
            foreach (var obj in objects)
            {
                var actualObj = f.GetNext(out actualId);
                Assert.Same(obj, actualObj);
                Assert.Equal(nextExpectedId++, actualId);
            }
        }

        [Fact]
        public void GetNext_UnexpectedObject_ThrowsException()
        {
            var f = new TestFormatter();
            f.m_objectQueue.Enqueue(new object());
            long id;
            Assert.Throws<SerializationException>(() => f.GetNext(out id));
        }

        [Fact]
        public void WriteMember_InvokesProperMethod()
        {
            string calledMethod = null;
            object result = null;
            var f = new TestFormatter { WriteCallback = (name, val) => { calledMethod = name; result = val; } };

            Action<string, object> verify = (expectedMember, expectedValue) =>
            {
                f.WriteMember("Member", expectedValue);
                Assert.Equal(expectedMember, calledMethod);
                Assert.Equal(expectedValue, result);
            };
            verify("WriteBoolean", true);
            verify("WriteByte", (byte)42);
            verify("WriteChar", 'c');
            verify("WriteDateTime", DateTime.Now);
            verify("WriteDecimal", 42m);
            verify("WriteDouble", 1.2);
            verify("WriteInt16", (short)42);
            verify("WriteInt32", 42);
            verify("WriteInt64", (long)42);
            verify("WriteSByte", (sbyte)42);
            verify("WriteSingle", 1.2f);
            verify("WriteUInt16", (ushort)42);
            verify("WriteUInt32", (uint)42);
            verify("WriteUInt64", (ulong)42);
            verify("WriteValueType", new KeyValuePair<int, int>(1, 2));
            // verify("WriteTimeSpan", TimeSpan.FromSeconds(42)); // Fails on both .NET Framework and core, getting routed as a normal ValueType:
            verify("WriteValueType", TimeSpan.FromSeconds(42));
            verify("WriteObjectRef", new ObjectWithIntStringUShortUIntULongAndCustomObjectFields());
            verify("WriteObjectRef", null);

            f.WriteMember("Member", new[] { 1, 2, 3, 4, 5 });
            Assert.Equal("WriteArray", calledMethod);
            Assert.Equal<int>(new[] { 1, 2, 3, 4, 5 }, (int[])result);
        }

        private sealed class TestFormatter : Formatter
        {
            public new object GetNext(out long objID) => base.GetNext(out objID);
            public new long Schedule(object obj) => base.Schedule(obj);
            public new void WriteMember(string memberName, object data) => base.WriteMember(memberName, data);
            public new ObjectIDGenerator m_idGenerator => base.m_idGenerator;
            public new Queue m_objectQueue => base.m_objectQueue;
            public Action<string, object> WriteCallback;

            protected override void WriteArray(object obj, string name, Type memberType) => WriteCallback?.Invoke("WriteArray", obj);
            protected override void WriteBoolean(bool val, string name) => WriteCallback?.Invoke("WriteBoolean", val);
            protected override void WriteByte(byte val, string name) => WriteCallback?.Invoke("WriteByte", val);
            protected override void WriteChar(char val, string name) => WriteCallback?.Invoke("WriteChar", val);
            protected override void WriteDateTime(DateTime val, string name) => WriteCallback?.Invoke("WriteDateTime", val);
            protected override void WriteDecimal(decimal val, string name) => WriteCallback?.Invoke("WriteDecimal", val);
            protected override void WriteDouble(double val, string name) => WriteCallback?.Invoke("WriteDouble", val);
            protected override void WriteInt16(short val, string name) => WriteCallback?.Invoke("WriteInt16", val);
            protected override void WriteInt32(int val, string name) => WriteCallback?.Invoke("WriteInt32", val);
            protected override void WriteInt64(long val, string name) => WriteCallback?.Invoke("WriteInt64", val);
            protected override void WriteObjectRef(object obj, string name, Type memberType) => WriteCallback?.Invoke("WriteObjectRef", obj);
            protected override void WriteSByte(sbyte val, string name) => WriteCallback?.Invoke("WriteSByte", val);
            protected override void WriteSingle(float val, string name) => WriteCallback?.Invoke("WriteSingle", val);
            protected override void WriteTimeSpan(TimeSpan val, string name) => WriteCallback?.Invoke("WriteTimeSpan", val);
            protected override void WriteUInt16(ushort val, string name) => WriteCallback?.Invoke("WriteUInt16", val);
            protected override void WriteUInt32(uint val, string name) => WriteCallback?.Invoke("WriteUInt32", val);
            protected override void WriteUInt64(ulong val, string name) => WriteCallback?.Invoke("WriteUInt64", val);
            protected override void WriteValueType(object obj, string name, Type memberType) => WriteCallback?.Invoke("WriteValueType", obj);

            public override SerializationBinder Binder { get; set; }
            public override StreamingContext Context { get; set; }
            public override ISurrogateSelector SurrogateSelector { get; set; }
#pragma warning disable CS0672 // Member overrides obsolete member
            public override object Deserialize(Stream serializationStream) => null;
            public override void Serialize(Stream serializationStream, object graph) { }
#pragma warning restore CS0672 // Member overrides obsolete member
        }
    }
}
