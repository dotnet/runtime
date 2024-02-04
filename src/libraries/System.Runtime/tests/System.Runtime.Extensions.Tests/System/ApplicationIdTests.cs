// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text;
using Xunit;

namespace System
{
    public class ApplicationIdTests
    {
        [Fact]
        public void Properties()
        {
            byte[] token = { 1, 2, 3, 4, 5 };
            var id = new ApplicationId(token, "Pizza", new Version(1, 0), "pepperoni", "it-it");
            CheckId(id, token);
            CheckId(id.Copy(), token);
        }

        [Fact]
        public void ToStringTest()
        {
            byte[] token = { 1, 2, 3, 4, 5 };
            var id = new ApplicationId(token, "Pizza", new Version(1, 0), "pepperoni", "it-it");
            Assert.Equal(
                "Pizza, culture=\"it-it\", version=\"1.0\", publicKeyToken=\"0102030405\", processorArchitecture =\"pepperoni\"",
                id.ToString());
        }

        private void CheckId(ApplicationId id, byte[] token)
        {
            Assert.Equal("Pizza", id.Name);
            Assert.Equal(new Version(1, 0), id.Version);
            Assert.Equal("pepperoni", id.ProcessorArchitecture);
            Assert.Equal("it-it", id.Culture);
            byte[] pk = id.PublicKeyToken;
            Assert.Equal(token, pk);
            Assert.NotSame(token, pk);
            Assert.NotSame(pk, id.PublicKeyToken);
        }
    }
}
