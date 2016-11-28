// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Threading.Tests
{
    public static class CompressedStackTests
    {
        [Fact]
        public static void Capture_GetCompressedStack_CreateCopy_Test()
        {
            CompressedStack compressedStack = CompressedStack.Capture();
            Assert.NotNull(compressedStack);
            Assert.NotNull(compressedStack.CreateCopy());
            Assert.NotNull(CompressedStack.GetCompressedStack());
            Assert.NotNull(CompressedStack.GetCompressedStack().CreateCopy());
        }

        [Fact]
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework)] // desktop framework throws ArgumentException
        public static void RunTest_SkipOnDesktopFramework()
        {
            Assert.Throws<ArgumentNullException>(() => CompressedStack.Run(null, state => { }, null));
        }

        [Fact]
        public static void RunTest()
        {
            CompressedStack compressedStack = CompressedStack.Capture();
            Assert.Throws<NullReferenceException>(() => CompressedStack.Run(compressedStack, null, null));

            var obj = new object();
            Thread mainThread = Thread.CurrentThread;
            bool callbackRan = false;
            CompressedStack.Run(
                compressedStack,
                state =>
                {
                    Assert.Same(obj, state);
                    Assert.Same(mainThread, Thread.CurrentThread);
                    callbackRan = true;
                },
                obj);
            Assert.True(callbackRan);
        }

        [Fact]
        public static void SerializationTest()
        {
            CompressedStack compressedStack = CompressedStack.Capture();
            Assert.Throws<ArgumentNullException>(() => compressedStack.GetObjectData(null, new StreamingContext()));

            var binaryFormatter = new BinaryFormatter();
            var memoryStream = new MemoryStream();
            binaryFormatter.Serialize(memoryStream, compressedStack);
            memoryStream.Close();
            byte[] binaryData = memoryStream.ToArray();

            memoryStream = new MemoryStream(binaryData);
            compressedStack = (CompressedStack)binaryFormatter.Deserialize(memoryStream);
            memoryStream.Close();
        }
    }
}
