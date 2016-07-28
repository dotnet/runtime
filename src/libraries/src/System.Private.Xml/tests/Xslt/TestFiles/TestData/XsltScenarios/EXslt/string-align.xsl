<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:str="http://exslt.org/strings" exclude-result-prefixes="str">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
          <test1>
            <xsl:value-of select="str:align(name, str:padding(20, '_'), 'left')"/>
          </test1>
          <test2>
            <xsl:value-of select="str:align(name, str:padding(20, '_'), 'center')"/>
          </test2>
          <test3>
            <xsl:value-of select="str:align(name, str:padding(20, '_'), 'right')"/>
          </test3>
          <test4>
            <xsl:value-of select="str:align(name, str:padding(20, '_'))"/>            
          </test4>
          <test5>
            <xsl:value-of select="str:align(name, str:padding(20, '_'), 'none')"/>
          </test5>
          <test6>
            <xsl:value-of select="str:align(name, str:padding(2, '*'), 'center')"/>
          </test6>
          <test7>
            <xsl:value-of select="str:align('', '*******', 'center')"/>
          </test7>
          <test8>
            <xsl:value-of select="str:align('foo', '')"/>
          </test8>
          <test9>
            <xsl:value-of select="str:align('foo', '******', '')"/>
          </test9>
        </out>
    </xsl:template>
</xsl:stylesheet>

  