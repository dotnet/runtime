<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:date="http://exslt.org/dates-and-times" exclude-result-prefixes="date">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:value-of select="date:add-duration('P1YT2S', 'P2D')"/>                                
            </test1>
            <test2>
                <xsl:value-of select="date:add-duration('P3M2D', 'PT5H2M')"/>                                                                
            </test2>
            <test3>
                <xsl:value-of select="date:add-duration('ZZZ', 'P2Y')"/>                                                                
            </test3>
            <test4>
                <xsl:value-of select="date:add-duration('', 'P2Y')"/>                                                                
            </test4>                             
        </out>
    </xsl:template>
</xsl:stylesheet>

  