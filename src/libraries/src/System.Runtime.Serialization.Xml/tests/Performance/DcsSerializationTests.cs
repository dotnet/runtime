// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using System.Collections.Generic;
using Xunit;

namespace System.Runtime.Serialization.Xml.Tests.Performance
{
    public class DcsSerializationTests
    {
        public static IEnumerable<object[]> SerializeMemberData()
        {
            return PerformanceTestCommon.PerformanceMemberData(SerializerType.DataContractSerializer);
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberData))]
        public void DcsSerializationTest(int iterations, TestType testType, TestSize testSize, SerializerType serializerType)
        {
            PerformanceTestCommon.RunSerializationPerformanceTest(iterations, serializerType, testType, testSize, DcsSerializerFactory.GetInstance());
        }
    }
}
