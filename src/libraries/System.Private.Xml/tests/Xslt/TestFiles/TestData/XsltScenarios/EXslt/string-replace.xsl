<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:str="http://exslt.org/strings" exclude-result-prefixes="str">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
          <test1>
            <xsl:copy-of select="str:replace('foo bar', 'bar', 'baz')"/>    
          </test1>
          <test2>
            <xsl:copy-of select="str:replace('foo bar', 'no', 'baz')"/>    
          </test2>
          <test3>
            <xsl:value-of select="str:replace(email, '@', '@NOSPAM')"/>
          </test3>
          <test4>
            <xsl:value-of select="str:replace('', 'foo', 'bar')"/>
          </test4>
        </out>
    </xsl:template>
</xsl:stylesheet>

  