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

using System.Linq;
using System.Xml;
using Windows.Globalization.DateTimeFormatting;
using Xunit;

namespace System.Security.Cryptography.Xml.Tests
{

    public class DSAKeyValueTest
    {
        [Fact]
        public void Ctor_Empty()
        {
            DSAKeyValue dsaKeyValue = new DSAKeyValue();
            Assert.NotNull(dsaKeyValue.Key);
        }

        [Fact]
        public void Ctor_Dsa()
        {
            using (DSA dsa = DSA.Create())
            {
                DSAKeyValue dsaKeyValue = new DSAKeyValue(dsa);
                Assert.Equal(dsa, dsaKeyValue.Key);
            }
        }

        [Fact]
        public void Ctor_Dsa_Null()
        {
            DSAKeyValue dsaKeyValue = new DSAKeyValue(null);
            Assert.NotNull(dsaKeyValue.Key);
        }

        [Fact]
        public void GetXml()
        {
            DSAKeyValue dsa = new DSAKeyValue();
            XmlElement xmlkey = dsa.GetXml();

            // Schema check. Should not throw.
            const string schema = "http://www.w3.org/2000/09/xmldsig#";
            new [] { "P", "Q", "G", "Y", "J", "Seed", "PgenCounter"}
                .Select(elementName => Convert.FromBase64String(xmlkey.SelectSingleNode($"*[name()=DSAKeyValue & namespace-uri()='{schema}']/*[name()='{elementName}' & namespace-uri()='{schema}']").InnerText));
        }

        [Fact]
        public void GetXml_SameDsa()
        {
            using (DSA dsa = DSA.Create())
            {
                DSAKeyValue dsaKeyValue1 = new DSAKeyValue(dsa);
                DSAKeyValue dsaKeyValue2 = new DSAKeyValue(dsa);
                Assert.Equal(dsaKeyValue1.GetXml(), dsaKeyValue2.GetXml());
            }
        }

        [Fact]
        public void LoadXml_PlatformNotSupported()
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
        public void LoadXml_Null()
        {
            DSAKeyValue dsa1 = new DSAKeyValue();
            Assert.Throws<PlatformNotSupportedException>(() => dsa1.LoadXml(null));
        }
    }
}
