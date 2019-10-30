<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:str="http://exslt.org/strings" exclude-result-prefixes="str">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:value-of 
                select="str:encode-uri('http://www.example.com/my résumé.html',false())"/>
            </test1>
            <test2>
                <xsl:value-of 
                select="str:encode-uri('http://www.example.com/my résumé.html',true())"/>
            </test2>
            <test3>
                <xsl:value-of 
                select="str:encode-uri('http://www.example.com/my résumé.html',false(),'iso-8859-1')"/>
            </test3>
            <test4>
              <xsl:value-of 
              select="str:encode-uri('http://www.example.com/my résumé.html',false(),'not-supported-enc')"/>
            </test4>
            <test5>
              <xsl:value-of 
              select="str:encode-uri('http://www.example.com/my résumé.html%24',false())"/>
            </test5>
            <test6>
              <xsl:value-of 
              select="str:encode-uri('http://www.example.com/my résumé.html%2sss',false())"/>
            </test6>
            <test7>
              <xsl:value-of 
              select="str:encode-uri('http://www.ex%ample.com/my résumé.html',false())"/>
            </test7>
            <test8>
              <xsl:value-of 
              select="str:encode-uri('http://www.example.com/my résumé.html',false(),'')"/>
            </test8>
            <test9>
              <xsl:value-of 
              select="str:encode-uri('http://www.example.com/my résumé.html',true(),'')"/>
            </test9>
            <test9>
              <xsl:value-of 
              select="str:encode-uri(uri ,true(),'')"/>
            </test9>
        </out>
    </xsl:template>
</xsl:stylesheet>

  