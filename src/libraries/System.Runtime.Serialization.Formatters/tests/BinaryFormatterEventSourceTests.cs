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
    [ActiveIssue("https://github.com/dotnet/runtime/issues/49568", typeof(PlatformDetection), nameof(PlatformDetection.IsMacOsAppleSilicon))]
    public static class BinaryFormatterEventSourceTests
    {
        private const string BinaryFormatterEventSourceName = "System.Runtime.Serialization.Formatters.Binary.BinaryFormatterEventSource";

        [Fact]
        public static void RecordsSerialization()
        {
            using LoggingEventListener listener = new LoggingEventListener();

            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(Stream.Null, CreatePerson());
            string[] capturedLog = listener.CaptureLog();

            string[] expected = new string[]
            {
                "SerializationStart [Start, 00000001]: <no payload>",
                "SerializingObject [Info, 00000001]: " + typeof(Person).AssemblyQualifiedName,
                "SerializingObject [Info, 00000001]: " + typeof(Address).AssemblyQualifiedName,
                "SerializationStop [Stop, 00000001]: <no payload>",
            };

            Assert.Equal(expected, capturedLog);
        }

        [Fact]
        public static void RecordsDeserialization()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, CreatePerson());
            ms.Position = 0;

            using LoggingEventListener listener = new LoggingEventListener();
            formatter.Deserialize(ms);
            string[] capturedLog = listener.CaptureLog();

            string[] expected = new string[]
            {
                "DeserializationStart [Start, 00000002]: <no payload>",
                "DeserializingObject [Info, 00000002]: " + typeof(Person).AssemblyQualifiedName,
                "DeserializingObject [Info, 00000002]: " + typeof(Address).AssemblyQualifiedName,
                "DeserializationStop [Stop, 00000002]: <no payload>",
            };

            Assert.Equal(expected, capturedLog);
        }

        [Fact]
        public static void RecordsNestedSerializationCalls()
        {
            // First, serialization

            using LoggingEventListener listener = new LoggingEventListener();

            MemoryStream ms = new MemoryStream();
            BinaryFormatter formatter = new BinaryFormatter();
            formatter.Serialize(ms, new ClassWithNestedDeserialization());
            string[] capturedLog = listener.CaptureLog();
            ms.Position = 0;

            string[] expected = new string[]
            {
                "SerializationStart [Start, 00000001]: <no payload>",
                "SerializingObject [Info, 00000001]: " + typeof(ClassWithNestedDeserialization).AssemblyQualifiedName,
                "SerializationStart [Start, 00000001]: <no payload>",
                "SerializingObject [Info, 00000001]: " + typeof(Address).AssemblyQualifiedName,
                "SerializationStop [Stop, 00000001]: <no payload>",
                "SerializationStop [Stop, 00000001]: <no payload>",
            };

            Assert.Equal(expected, capturedLog);
            listener.ClearLog();

            // Then, deserialization

            ms.Position = 0;
            formatter.Deserialize(ms);
            capturedLog = listener.CaptureLog();

            expected = new string[]
            {
                "DeserializationStart [Start, 00000002]: <no payload>",
                "DeserializingObject [Info, 00000002]: " + typeof(ClassWithNestedDeserialization).AssemblyQualifiedName,
                "DeserializationStart [Start, 00000002]: <no payload>",
                "DeserializingObject [Info, 00000002]: " + typeof(Address).AssemblyQualifiedName,
                "DeserializationStop [Stop, 00000002]: <no payload>",
                "DeserializationStop [Stop, 00000002]: <no payload>",
            };

            Assert.Equal(expected, capturedLog);
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
            private readonly List<string> _log = new List<string>();

            private void AddToLog(FormattableString message)
            {
                _log.Add(FormattableString.Invariant(message));
            }

            // Captures the current log
            public string[] CaptureLog()
            {
                return _log.ToArray();
            }

            public void ClearLog()
            {
                _log.Clear();
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
