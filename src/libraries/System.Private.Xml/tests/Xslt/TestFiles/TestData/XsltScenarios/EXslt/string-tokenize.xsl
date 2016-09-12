<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:str="http://exslt.org/strings" exclude-result-prefixes="str">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="/">
        <out>
            <test1>
                <xsl:copy-of select="str:tokenize('2001-06-03T11:40:23', '-T:')"/>
            </test1>
            <test2>
                <xsl:copy-of select="str:tokenize('date math str')"/>
            </test2>
            <test3>
                <xsl:copy-of select="str:tokenize('foo', '')"/>
            </test3>
            <test4>
              <xsl:copy-of select="str:tokenize('', '-')"/>
            </test4>
        </out>
    </xsl:template>
</xsl:stylesheet>

  