// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    public static class SerializationGuardTests
    {
        [Fact]
        public static void BlockAssemblyLoads()
        {
            TryPayload(new AssemblyLoader());
        }

        [Fact]
        public static void BlockProcessStarts()
        {
            TryPayload(new ProcessStarter());
        }

        [Fact]
        public static void BlockFileWrites()
        {
            TryPayload(new FileWriter());
        }

        [Fact]
        public static void BlockReflectionDodging()
        {
            // Ensure that the deserialization tracker cannot be called by reflection.
            MethodInfo trackerMethod = typeof(Thread).GetMethod(
                "GetThreadDeserializationTracker",
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(trackerMethod);

            Assert.Equal(1, trackerMethod.GetParameters().Length);
            object[] args = new object[1];
            args[0] = Enum.ToObject(typeof(Thread).Assembly.GetType("System.Threading.StackCrawlMark"), 0);

            try
            {
                object tracker = trackerMethod.Invoke(null, args);
                throw new InvalidOperationException(tracker?.ToString() ?? "(null tracker returned)");
            }
            catch (TargetInvocationException ex)
            {
                Exception baseEx = ex.GetBaseException();
                AssertExtensions.Throws<ArgumentException>("stackMark", () => throw baseEx);
            }
        }

        [Fact]
        public static void BlockAsyncDodging()
        {
            TryPayload(new AsyncDodger());
        }

        private static void TryPayload(object payload)
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter writer = new BinaryFormatter();
            writer.Serialize(ms, payload);
            ms.Position = 0;

            BinaryFormatter reader = new BinaryFormatter();
            Assert.Throws<DeserializationBlockedException>(() => reader.Deserialize(ms));
        }
    }

    [Serializable]
    internal class AssemblyLoader : ISerializable
    {
        public AssemblyLoader() { }

        public AssemblyLoader(SerializationInfo info, StreamingContext context)
        {
            Assembly.Load(new byte[1000]);
        }
        
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }

    [Serializable]
    internal class ProcessStarter : ISerializable
    {
        public ProcessStarter() { }

        private ProcessStarter(SerializationInfo info, StreamingContext context)
        {
            Process.Start("calc.exe");
        }
        
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }

    [Serializable]
    internal class FileWriter : ISerializable
    {
        public FileWriter() { }

        private FileWriter(SerializationInfo info, StreamingContext context)
        {
            string tempPath = Path.GetTempFileName();
            File.WriteAllText(tempPath, "This better not be written...");
            throw new InvalidOperationException("Unreachable code (SerializationGuard should have kicked in)");
        }
        
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }

    [Serializable]
    internal class AsyncDodger : ISerializable
    {
        public AsyncDodger() { }

        private AsyncDodger(SerializationInfo info, StreamingContext context)
        {
            try
            {
                Task t = Task.Factory.StartNew(LoadAssemblyOnBackgroundThread, TaskCreationOptions.LongRunning);
                t.Wait();
            }
            catch (AggregateException ex)
            {
                throw ex.InnerException;
            }
        }

        private void LoadAssemblyOnBackgroundThread()
        {
            Assembly.Load(new byte[1000]);
        }
        
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }
}
