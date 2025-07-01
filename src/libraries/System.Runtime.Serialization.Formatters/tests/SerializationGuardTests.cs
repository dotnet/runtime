// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Xunit;

namespace System.Runtime.Serialization.Formatters.Tests
{
    // When BinaryFormatter was built-in to the platform we used to activate SerializationGuard in ObjectReader.Deserialize,
    // but now that it has moved to an OOB offering it no longer does.
    [ConditionalClass(typeof(TestConfiguration), nameof(TestConfiguration.IsBinaryFormatterEnabled))]
    public static class SerializationGuardTests
    {
        [Fact]
        public static void IsNoLongerActivated()
        {
            MemoryStream ms = new MemoryStream();
            BinaryFormatter writer = new BinaryFormatter();
            writer.Serialize(ms, new ThrowIfDeserializationInProgress());
            ms.Position = 0;

            BinaryFormatter reader = new BinaryFormatter();
            Assert.NotNull(reader.Deserialize(ms));
        }
    }

    [Serializable]
    internal class ThrowIfDeserializationInProgress : ISerializable
    {
        private static int s_cachedSerializationSwitch;

        public ThrowIfDeserializationInProgress() { }

        private ThrowIfDeserializationInProgress(SerializationInfo info, StreamingContext context)
        {
            SerializationGuard.ThrowIfDeserializationInProgress("AllowProcessCreation", ref s_cachedSerializationSwitch);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
        }
    }
}
