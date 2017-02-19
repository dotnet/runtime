//
// DSAKeyValueTest.cs - Test Cases for DSAKeyValue
//
// Author:
//	Sebastien Pouliot (spouliot@motus.com)
//
// (C) 2002, 2003 Motus Technologies Inc. (http://www.motus.com)
//
// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

using System.Xml;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class DSAKeyValueTest
    {

        [Fact]
        public void GenerateKey()
        {
            DSAKeyValue dsa1 = new DSAKeyValue();
            Assert.NotNull(dsa1.Key);
            XmlElement xmlkey = dsa1.GetXml();

            DSAKeyValue dsa2 = new DSAKeyValue();
            dsa2.LoadXml(xmlkey);

            Assert.True((dsa1.GetXml().OuterXml) == (dsa2.GetXml().OuterXml));

            DSA key = dsa1.Key;
            DSAKeyValue dsa3 = new DSAKeyValue(key);
            Assert.True((dsa3.GetXml().OuterXml) == (dsa1.GetXml().OuterXml));
            Assert.True((dsa3.GetXml().OuterXml) == (dsa2.GetXml().OuterXml));
        }

        [Fact]
        public void ImportKey()
        {
            string dsaKey = "<KeyValue xmlns=\"http://www.w3.org/2000/09/xmldsig#\"><DSAKeyValue><P>xc+QZRWTgr390gzwNXF+WzoepZkvAQvCzfCm+YyXj0KPoeHHeSc5ORzXQw81V+7XJR3gupvlI4F7lW9YC538l+3eqGm8IQlCIS+U+7ICTDOFFKevqsYX0BnjO0vvE4aAtDyxfSOTCOAo1cJ+6G6xgcC1JGIBEYCtg1tH8wUewDE=</P><Q>yyfZb0S/rimXl9ScJ3zIba2oGl8=</Q><G>crLazMg+vgI7u6+Idgi9iTLdRa4fptat3gdY97zcc857+OVdmT+lVRpK3okWpmBbw2wSffU8QltwFf42BVs+/HGUOUo2hNqSSXgzl1i+1frO7/cqooHVcy5WX0xxaIPsKcREPI5pNPj/3g8apTgErLMGsHkFdngwbMed9DArTks=</G><Y>FlAozo17wV/LCMRrtnmMKxVQNpidJVkZNM1/0eR65x8giwPs6yXzJmFT8f2tmPJY2FIOAtp5JYin4xUhwIHF452Gg50wUrjV6WTGkiC+gzLC2fVIyGlVsFecLj6ue7J+MACG+b3NQnxFuT5maQnPnEeuGgjLXfwYsAR1vfU0Gas=</Y><J>+UPMvUPq9Fo6Q1fr2oEYDxfGMMtfdoQmVBxI+TkUYQsReodRzBbnvGV1uPLWTpKKd/uJNUHO/QGb05Cvc6u49/AToDJIyi4e01hTLNCzeQk/Hj19gowb5wkTIjyaH04VyPE5zYoTYfuu3Y3Q</J><Seed>+cvoO7bzdpAwAjnDDApPzBCl6zg=</Seed><PgenCounter>ATM=</PgenCounter></DSAKeyValue></KeyValue>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(dsaKey);

            DSAKeyValue dsa1 = new DSAKeyValue();
            Assert.Throws<PlatformNotSupportedException>(() => dsa1.LoadXml(doc.DocumentElement));

            //string s = (dsa1.GetXml().OuterXml);
            //Assert.Equal(dsaKey, s);
        }

        [Fact]
        public void InvalidValue1()
        {
            string badKey = "<Test></Test>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(badKey);

            DSAKeyValue dsa1 = new DSAKeyValue();
            Assert.Throws<ArgumentNullException>(() => dsa1.LoadXml(null));
        }

        [Fact]
        public void InvalidValue2()
        {
            string badKey = "<Test></Test>";
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(badKey);

            DSAKeyValue dsa1 = new DSAKeyValue();
            Assert.Throws<CryptographicException>(() => dsa1.LoadXml(doc.DocumentElement));
        }
    }
}
