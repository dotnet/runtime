<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:date="http://exslt.org/dates-and-times" exclude-result-prefixes="date">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:value-of select="date:seconds(date)"/>                                
            </test1>
            <test2>
                <xsl:value-of select="date:seconds(date2)"/>                                                                
            </test2>
            <test3>
                <xsl:value-of select="date:seconds('P120D')"/>                                                                
            </test3>
            <test4>
                <xsl:value-of select="date:seconds('P2Y')"/>                                                                                
            </test4>
            <test5>
                <xsl:value-of select="date:seconds('P3M')"/>                                                                                
            </test5>                             
        </out>
    </xsl:template>
</xsl:stylesheet>

  