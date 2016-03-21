// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Xunit.Performance;
using System.Collections.Generic;
using Xunit;

namespace System.Runtime.Serialization.Json.Tests.Performance
{
    public class DcjsDeserializationTests
    {
        public static IEnumerable<object[]> SerializeMemberData()
        {
            return PerformanceTestCommon.PerformanceMemberData(SerializerType.DataContractJsonSerializer);
        }

        [Benchmark]
        [MemberData(nameof(SerializeMemberData))]
        public void DcjsDeSerializationTest(int iterations, TestType testType, TestSize testSize, SerializerType serializerType)
        {
            PerformanceTestCommon.RunDeSerializationPerformanceTest(iterations, serializerType, testType, testSize, DcjsSerializerFactory.GetInstance());
        }
    }
}
