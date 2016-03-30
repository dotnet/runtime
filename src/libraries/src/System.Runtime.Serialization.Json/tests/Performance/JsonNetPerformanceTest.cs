// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.IO;
using Microsoft.Xunit.Performance;
using Newtonsoft.Json;
using Xunit;

namespace System.Runtime.Serialization.Json.Tests.Performance
{
    #region Json.NET performance tests

    public class JsonNetPerformanceTest
    {
        public static IEnumerable<object[]> SerializeMemberDataJsonNet()
        {
            return PerformanceTestCommon.PerformanceMemberData();
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberDataJsonNet))]
        public void JsonNetSerializationTest(int iterations, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunSerializationPerformanceTest(iterations, testType, testSize, JsonNetSerializerFactory.GetInstance());
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberDataJsonNet))]
        public void JsonNetDeSerializationTest(int iterations, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunDeSerializationPerformanceTest(iterations, testType, testSize, JsonNetSerializerFactory.GetInstance());
        }
    }

    #endregion

    #region Json.Net serializer wrapper

    internal class JsonNetSerializerFactory : SerializerFactory
    {
        private static JsonNetSerializerFactory _instance = null;
        public static JsonNetSerializerFactory GetInstance()
        {
            if (_instance == null)
            {
                _instance = new JsonNetSerializerFactory();
            }
            return _instance;
        }

        public override IPerfTestSerializer GetSerializer()
        {
            return new JsonNetSerializer();
        }
    }

    internal class JsonNetSerializer : IPerfTestSerializer
    {
        private JsonSerializer _serializer;

        public void Deserialize(Stream stream)
        {
            // Assumption: Deserialize() is always called after Init()
            // Assumption: stream != null
            stream.Position = 0;
            JsonReader reader = new JsonTextReader(new StreamReader(stream));
            _serializer.Deserialize(reader);
        }

        public void Init(object obj)
        {
            _serializer = new JsonSerializer();
        }

        public void Serialize(object obj, Stream stream)
        {
            // Assumption: Serialize() is always called after Init()
            // Assumption: stream != null and stream position will be reset to 0 after this method
            var writer = new StreamWriter(stream);
            _serializer.Serialize(writer, obj);
            writer.Flush();
            stream.Position = 0;
        }
    }

    #endregion
}
