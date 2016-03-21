// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Xunit;

namespace System.Xml.XmlSerializer.Tests.Performance
{
    public class XmlSerializationTests
    {
        public static IEnumerable<object[]> SerializeMemberData()
        {
            return PerformanceTestCommon.PerformanceMemberData(SerializerType.XmlSerializer);
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberData))]
        public void XsSerializationTest(int iterations, TestType testType, TestSize testSize, SerializerType serializerType)
        {
            PerformanceTestCommon.RunSerializationPerformanceTest(iterations, serializerType, testType, testSize, XsSerializerFactory.GetInstance());
        }
    }
}
