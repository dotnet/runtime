<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:date="http://exslt.org/dates-and-times" exclude-result-prefixes="date">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:value-of select="date:duration(994241336)"/>                                
            </test1>
            <test2>
                <xsl:value-of select="date:duration(1000274342)"/>                                                                
            </test2>
            <test3>
                <xsl:value-of select="date:duration(10368000)"/>                                                                
            </test3>
            <test4>
                <xsl:value-of select="date:duration(63072000)"/>                                                                                
            </test4>
            <test5>
                <xsl:value-of select="date:duration(7776000)"/>                                                                                
            </test5>
            <test6>
                <xsl:value-of select="date:duration(-7776000)"/>                                                                                
            </test6>    
            <test7>
                <xsl:value-of select="date:duration(11 div 0)"/>                                                                                
            </test7>
            <test8>
                <xsl:value-of select="date:duration(0)"/>                                                                                
            </test8>                             
        </out>
    </xsl:template>
</xsl:stylesheet>

  