// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

namespace System.Runtime.InteropServices.Tests
{
#pragma warning disable 0618 // ComSourceInterfacesAttribute is marked as Obsolete.
    public class ComSourceInterfacesAttributeTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("SourceInterfaces")]
        public void Ctor_SourceInterfaces(string sourceInterfaces)
        {
            var attribute = new ComSourceInterfacesAttribute(sourceInterfaces);
            Assert.Equal(sourceInterfaces, attribute.Value);
        }

        [Fact]
        public void Ctor_SourceInterface1()
        {
            var attribute = new ComSourceInterfacesAttribute(typeof(int));
            Assert.Equal("System.Int32", attribute.Value);
        }

        [Fact]
        public void Ctor_NullSourceInterfaceType1_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => new ComSourceInterfacesAttribute((Type)null));
            Assert.Throws<NullReferenceException>(() => new ComSourceInterfacesAttribute(null, typeof(int)));
            Assert.Throws<NullReferenceException>(() => new ComSourceInterfacesAttribute(null, typeof(int), typeof(string)));
            Assert.Throws<NullReferenceException>(() => new ComSourceInterfacesAttribute(null, typeof(int), typeof(string), typeof(bool)));
        }

        [Fact]
        public void Ctor_SourceInterface1_SourceInterfaceType2()
        {
            var attribute = new ComSourceInterfacesAttribute(typeof(int), typeof(string));
            Assert.Equal("System.Int32\0System.String", attribute.Value);
        }

        [Fact]
        public void Ctor_NullSourceInterfaceType2_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => new ComSourceInterfacesAttribute(typeof(int), null));
            Assert.Throws<NullReferenceException>(() => new ComSourceInterfacesAttribute(typeof(int), null, typeof(string)));
            Assert.Throws<NullReferenceException>(() => new ComSourceInterfacesAttribute(typeof(int), null, typeof(string), typeof(bool)));
        }

        [Fact]
        public void Ctor_SourceInterface1_SourceInterfaceType2_SourceInterfaceType3()
        {
            var attribute = new ComSourceInterfacesAttribute(typeof(int), typeof(string), typeof(bool));
            Assert.Equal("System.Int32\0System.String\0System.Boolean", attribute.Value);
        }

        [Fact]
        public void Ctor_NullSourceInterfaceType3_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => new ComSourceInterfacesAttribute(typeof(int), typeof(string), null));
            Assert.Throws<NullReferenceException>(() => new ComSourceInterfacesAttribute(typeof(int), typeof(string), null, typeof(bool)));
        }

        [Fact]
        public void Ctor_SourceInterface1_SourceInterfaceType2_SourceInterfaceType3_SourceInterfaceType4()
        {
            var attribute = new ComSourceInterfacesAttribute(typeof(int), typeof(string), typeof(bool), typeof(short));
            Assert.Equal("System.Int32\0System.String\0System.Boolean\0System.Int16", attribute.Value);
        }

        [Fact]
        public void Ctor_NullSourceInterfaceType4_ThrowsNullReferenceException()
        {
            Assert.Throws<NullReferenceException>(() => new ComSourceInterfacesAttribute(typeof(int), typeof(string), typeof(bool), null));
        }
    }
#pragma warning restore 0618
}
