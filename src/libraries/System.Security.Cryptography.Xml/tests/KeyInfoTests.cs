// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{
    public class KeyInfoTests
    {
        [Fact]
        public void Constructor()
        {
            KeyInfo keyInfo = new KeyInfo();

            Assert.Equal(0, keyInfo.Count);
            Assert.Equal(null, keyInfo.Id);

            XmlElement xmlElement = keyInfo.GetXml();
            Assert.NotNull(xmlElement);
            Assert.Equal("<KeyInfo xmlns=\"http://www.w3.org/2000/09/xmldsig#\" />", xmlElement.OuterXml);

            IEnumerator enumerator = keyInfo.GetEnumerator();
            Assert.NotNull(enumerator);
            Assert.False(enumerator.MoveNext());
        }

        [Fact]
        public void AddClause()
        {
            KeyInfo keyInfo = new KeyInfo();
        }
    }
}
