// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    [ConditionalClass(typeof(PlatformDetection), nameof(PlatformDetection.IsBinaryFormatterSupported))]
    public static class BinaryFormatterEventSourceTests
    {
        private const string BinaryFormatterEventSourceName = "System.Runtime.Serialization.Formatters.Binary.BinaryFormatterEventSource";

        [Fact]
        public static void RecordsSerialization()
        {
            LoggingEventListener listener = new LoggingEventListener();

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(Stream.Null, CreatePerson());
            listener.Dispose();

            string[] expected = new string[]
            {
                "SerializationStarted [Start, 00000001]: <no payload>",
                "SerializingObject [Info, 00000001]: " + typeof(Person).AssemblyQualifiedName,
                "SerializingObject [Info, 00000001]: " + typeof(Address).AssemblyQualifiedName,
                "SerializationEnded [Stop, 00000001]: <no payload>",
            };

            Assert.Equal(expected, listener.Log);
        }

        [Fact]
        public static void RecordsDeserialization()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, CreatePerson());
            ms.Position = 0;

            LoggingEventListener listener = new LoggingEventListener();
            formatter.Deserialize(ms);
            listener.Dispose();

            string[] expected = new string[]
            {
                "DeserializationStarted [Start, 00000002]: <no payload>",
                "DeserializingObject [Info, 00000002]: " + typeof(Person).AssemblyQualifiedName,
                "DeserializingObject [Info, 00000002]: " + typeof(Address).AssemblyQualifiedName,
                "DeserializationEnded [Stop, 00000002]: <no payload>",
            };

            Assert.Equal(expected, listener.Log);
        }

        [Fact]
        public static void RecordsNestedSerializationCalls()
        {
            // First, serialization

            LoggingEventListener listener = new LoggingEventListener();

            MemoryStream ms = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, new ClassWithNestedDeserialization());
            ms.Position = 0;

            string[] expected = new string[]
            {
                "SerializationStarted [Start, 00000001]: <no payload>",
                "SerializingObject [Info, 00000001]: " + typeof(ClassWithNestedDeserialization).AssemblyQualifiedName,
                "SerializationStarted [Start, 00000001]: <no payload>",
                "SerializingObject [Info, 00000001]: " + typeof(Address).AssemblyQualifiedName,
                "SerializationEnded [Stop, 00000001]: <no payload>",
                "SerializationEnded [Stop, 00000001]: <no payload>",
            };

            Assert.Equal(expected, listener.Log);
            listener.Log.Clear();

            // Then, deserialization

            ms.Position = 0;
            formatter.Deserialize(ms);
            listener.Dispose();

            expected = new string[]
            {
                "DeserializationStarted [Start, 00000002]: <no payload>",
                "DeserializingObject [Info, 00000002]: " + typeof(ClassWithNestedDeserialization).AssemblyQualifiedName,
                "DeserializationStarted [Start, 00000002]: <no payload>",
                "DeserializingObject [Info, 00000002]: " + typeof(Address).AssemblyQualifiedName,
                "DeserializationEnded [Stop, 00000002]: <no payload>",
                "DeserializationEnded [Stop, 00000002]: <no payload>",
            };

            Assert.Equal(expected, listener.Log);
        }

        [Fact]
        public static void DoesNotRecordRecursiveSerializationPerformedByEventListener()
        {
            // First, serialization

            LoggingEventListener listener = new LoggingEventListener();
            listener.EventWritten += (source, args) => listener.Log.Add("Callback fired.");
            listener.EventWritten += (source, args) => new BinaryFormatter().Serialize(Stream.Null, CreatePerson());

            MemoryStream ms = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, CreatePerson());
            ms.Position = 0;

            string[] expected = new string[]
            {
                "SerializationStarted [Start, 00000001]: <no payload>",
                "Callback fired.",
                "SerializingObject [Info, 00000001]: " + typeof(Person).AssemblyQualifiedName,
                "Callback fired.",
                "SerializingObject [Info, 00000001]: " + typeof(Address).AssemblyQualifiedName,
                "Callback fired.",
                "SerializationEnded [Stop, 00000001]: <no payload>",
                "Callback fired.",
            };

            Assert.Equal(expected, listener.Log);
            listener.Log.Clear();

            // Then, deserialization

            ms.Position = 0;
            formatter.Deserialize(ms);
            listener.Dispose();

            expected = new string[]
            {
                "DeserializationStarted [Start, 00000002]: <no payload>",
                "Callback fired.",
                "DeserializingObject [Info, 00000002]: " + typeof(Person).AssemblyQualifiedName,
                "Callback fired.",
                "DeserializingObject [Info, 00000002]: " + typeof(Address).AssemblyQualifiedName,
                "Callback fired.",
                "DeserializationEnded [Stop, 00000002]: <no payload>",
                "Callback fired.",
            };

            Assert.Equal(expected, listener.Log);
        }

        private static Person CreatePerson()
        {
            return new Person()
            {
                Name = "Some Chap",
                HomeAddress = new Address()
                {
                    Street = "123 Anywhere Ln",
                    City = "Anywhere ST 00000 United States"
                }
            };
        }

        private sealed class LoggingEventListener : EventListener
        {
            private readonly Thread _activeThread = Thread.CurrentThread;
            internal readonly List<string> Log = new List<string>();

            private void AddToLog(FormattableString message)
            {
                Log.Add(FormattableString.Invariant(message));
            }

            protected override void OnEventSourceCreated(EventSource eventSource)
            {
                if (eventSource.Name == BinaryFormatterEventSourceName)
                {
                    EnableEvents(eventSource, EventLevel.Verbose);
                }

                base.OnEventSourceCreated(eventSource);
            }

            protected override void OnEventWritten(EventWrittenEventArgs eventData)
            {
                // The test project is parallelized. We want to filter to only events that fired
                // on the current thread, otherwise we could throw off the test results.

                if (Thread.CurrentThread != _activeThread)
                {
                    return;
                }

                AddToLog($"{eventData.EventName} [{eventData.Opcode}, {(int)eventData.Keywords & int.MaxValue:X8}]: {ParsePayload(eventData.Payload)}");
                base.OnEventWritten(eventData);
            }

            private static string ParsePayload(IReadOnlyCollection<object> collection)
            {
                if (collection?.Count > 0)
                {
                    return string.Join("; ", collection.Select(o => o?.ToString() ?? "<null>"));
                }
                else
                {
                    return "<no payload>";
                }
            }
        }

        [Serializable]
        private class Person
        {
            public string Name { get; set; }
            public Address HomeAddress { get; set; }
        }

        [Serializable]
        private class Address
        {
            public string Street { get; set; }
            public string City { get; set; }
        }

        [Serializable]
        public class ClassWithNestedDeserialization : ISerializable
        {
            public ClassWithNestedDeserialization()
            {
            }

            protected ClassWithNestedDeserialization(SerializationInfo info, StreamingContext context)
            {
                byte[] serializedData = (byte[])info.GetValue("SomeField", typeof(byte[]));
                MemoryStream ms = new MemoryStream(serializedData);
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Deserialize(ms); // should deserialize an 'Address' instance
            }

            public void GetObjectData(SerializationInfo info, StreamingContext context)
            {
                MemoryStream ms = new MemoryStream();
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(ms, new Address());
                info.AddValue("SomeField", ms.ToArray());
            }
        }
    }
}
