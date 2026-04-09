<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:str="http://exslt.org/strings" exclude-result-prefixes="str">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
          <test1>
            <xsl:value-of select="str:padding(20, '_')"/>    
          </test1>
          <test2>
            <xsl:value-of select="str:padding(5, 'abc')"/>    
          </test2>
          <test3>
            <xsl:value-of select="str:padding(10, '')"/>
          </test3>
          <test4>
            <xsl:value-of select="str:padding(12)"/>
          </test4>
          <test5>
            <xsl:value-of select="str:padding(0, 'foo')"/>
          </test5>
          <test6>
            <xsl:value-of select="str:padding(-2, 'foo')"/>
          </test6>
          <test7>
            <xsl:value-of select="str:padding(-4)"/>
          </test7>
        </out>
    </xsl:template>
</xsl:stylesheet>

  