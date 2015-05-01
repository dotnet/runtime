// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
            [ActiveIssue(54)]
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