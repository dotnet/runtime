// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;
using System.Xml.Linq;

using Xunit;

namespace XLinqTests
{
    public class XObjectTests
    {
        public class SkipNotifyTests
        {
            [Fact]
            public void NoXObjectChangeAnnotation()
            {
                var sut = new FakeXObject();
                sut.AddAnnotation(new object());
                sut.AddAnnotation(new object());
                Assert.True(sut.SkipNotify());
            }

            [Fact]
            public void XObjectChangeAnnotation()
            {
                var sut = new FakeXObject();
                sut.AddAnnotation(new object());
                sut.AddAnnotation(new object());
                sut.AddAnnotation(new XObjectChangeAnnotation());
                Assert.False(sut.SkipNotify());
            }
        }

        private class FakeXObject : XObject
        {
            public override XmlNodeType NodeType
            {
                get
                {
                    return XmlNodeType.None;
                }
            }
        }
    }
}
