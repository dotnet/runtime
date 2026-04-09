<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:str="http://exslt.org/strings" exclude-result-prefixes="str">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:value-of 
                select="str:decode-uri('http://www.example.com/my%20r%C3%A9sum%C3%A9.html')"/>
            </test1>
            <test2>
                <xsl:value-of 
                select="str:decode-uri('http://www.example.com/my%20r%E9sum%E9.html','iso-8859-1')"/>
            </test2>
            <test3>
                <xsl:value-of 
                select="str:decode-uri('http://www.example.com/my%20r%E9sum%E9.html','not-supported-enc')"/>
            </test3>
            <test4>
              <xsl:value-of 
              select="str:decode-uri('http://www.example.com/my%20r%E9sum%E9.html','')"/>
            </test4>
            <test4>
              <xsl:value-of 
              select="str:decode-uri(url, '')"/>
            </test4>
            <test5>
              <xsl:value-of 
              select="str:decode-uri(uri)"/>
            </test5>
        </out>
    </xsl:template>
</xsl:stylesheet>

  