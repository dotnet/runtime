// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Microsoft.Xunit.Performance;
using Xunit;

namespace System.Xml.XmlSerializer.Tests.Performance
{
    #region XmlSerializer performance tests

    public class XsPerformanceTest
    {
#if ReflectionOnly
        private static readonly string SerializationModeSetterName = "set_Mode";

        static XsPerformanceTest()
        {
            if (!PlatformDetection.IsFullFramework)
            {
                MethodInfo method = typeof(Serialization.XmlSerializer).GetMethod(SerializationModeSetterName, BindingFlags.NonPublic | BindingFlags.Static);
                Assert.True(method != null, $"No method named {SerializationModeSetterName}");
                method.Invoke(null, new object[] { 1 });    
            }
        }
#endif

        public static IEnumerable<object[]> SerializeMemberData()
        {
            foreach (PerfTestConfig config in PerformanceTestCommon.PerformanceTestConfigurations())
            {
                // XmlSerializer doesn't support Dictionary type
                if (config.PerfTestType == TestType.Dictionary
                  || config.PerfTestType == TestType.DictionaryOfSimpleType) 
                {
                    continue;
                }

                yield return config.ToObjectArray();
            }
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberData))]
        public void XsSerializationTest(int numberOfRuns, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunSerializationPerformanceTest(numberOfRuns, testType, testSize, XsSerializerFactory.GetInstance());
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberData))]
        public void XsDeSerializationTest(int numberOfRuns, TestType testType, int testSize)
        {
            PerformanceTestCommon.RunDeserializationPerformanceTest(numberOfRuns, testType, testSize, XsSerializerFactory.GetInstance());
        }
    }

    #endregion

    #region XmlSerializer wrapper

    internal class XsSerializerFactory : ISerializerFactory
    {
        private static readonly XsSerializerFactory s_instance = new XsSerializerFactory();
        public static XsSerializerFactory GetInstance()
        {
            return s_instance;
        }

        public IPerfTestSerializer GetSerializer()
        {
            return new XsSerializer();
        }
    }

    internal class XsSerializer : IPerfTestSerializer
    {
        private Serialization.XmlSerializer _serializer;

        public void Deserialize(Stream stream)
        {
            Debug.Assert(_serializer != null);
            Debug.Assert(stream != null);
            _serializer.Deserialize(stream);
        }

        public void Init(object obj)
        {
            _serializer = new Serialization.XmlSerializer(obj.GetType());
        }

        public void Serialize(object obj, Stream stream)
        {
            Debug.Assert(_serializer != null);
            Debug.Assert(stream != null);
            _serializer.Serialize(stream, obj);
        }
    }

    #endregion
}
