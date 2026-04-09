// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Xml.XPath;
using System.Xml.Xsl;

class Program
{
    static int Main(string[] args)
    {
        using (StringReader text = new StringReader(
    @"<?xml version=""1.0"" encoding=""UTF-8""?>
<catalog>
    <cd>
        <title>Empire Burlesque</title>
        <artist>Bob Dylan</artist>
        <country>USA</country>
        <company>Columbia</company>
        <price>10.90</price>
        <year>1985</year>
    </cd>
</catalog>"))
        {


            XPathDocument myXPathDoc = new XPathDocument(text);
            XslCompiledTransform myXslTrans = new XslCompiledTransform();
            string xmlStr =
@"<?xml version=""1.0""?>
<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"">
<xsl:template match=""/"">
<html>
<body>
<h2> My CD Collection</h2>
   <table border = ""1"">
      <tr bgcolor = ""#9acd32"">
         <th> Title </th>
         <th> Artist </th>
       </tr>

      <xsl:for-each select = ""catalog/cd"">
          <tr>
            <td><xsl:value-of select = ""title"" /></td>
            <td><xsl:value-of select = ""artist"" /></td>
          </tr>
        </xsl:for-each>
       </table>
     </body>
     </html>
   </xsl:template>
   </xsl:stylesheet>";

            using (StringReader str = new StringReader(xmlStr))
            using (var reader = System.Xml.XmlReader.Create(str))
            {
                myXslTrans.Load(reader);

                StringWriter myWriter = new StringWriter();
                myXslTrans.Transform(myXPathDoc, null, myWriter);

                string result = myWriter.ToString();
                if (result.Contains("<td>Empire Burlesque</td>") &&
                    result.Contains("<td>Bob Dylan</td>"))
                {
                    return 100;
                }
            }
        }

        return -1;
    }
}
