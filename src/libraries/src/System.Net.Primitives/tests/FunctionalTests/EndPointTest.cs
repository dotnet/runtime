// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Net.Primitives.Functional.Tests
{
    public class MockEndPoint: EndPoint
    {
    }

    public static class EndPointTest
    {
        private static EndPoint CreateEndPoint()
        {
            return Activator.CreateInstance<MockEndPoint>();
        }

        [Fact]
        public static void AddressFamily_Get_Invalid()
        {
            EndPoint ep = CreateEndPoint();
            Assert.Throws<NotImplementedException>(() => ep.AddressFamily);
        }

        [Fact]
        public static void Serialize_Invalid()
        {
            EndPoint ep = CreateEndPoint();
            Assert.Throws<NotImplementedException>(() => ep.Serialize());
        }

        [Fact]
        public static void Create_Invalid()
        {
            EndPoint ep = CreateEndPoint();
            Assert.Throws<NotImplementedException>(() => ep.Create(null));
        }
    }
}
