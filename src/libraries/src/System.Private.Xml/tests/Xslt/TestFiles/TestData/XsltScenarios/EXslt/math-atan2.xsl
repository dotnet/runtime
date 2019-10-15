<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:math="http://exslt.org/math" exclude-result-prefixes="math">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="/">
        <out>
            <test1>                
                <xsl:value-of select="math:atan2(0.5, 0.7)"/>
            </test1>
            <test2>                
                <xsl:value-of select="math:atan2(-0.9, -0.4)"/>
            </test2>
            <test3>                
                <xsl:value-of select="math:atan2(/data/bad-data, 4)"/>
            </test3>                                    
            <test4>                
                <xsl:value-of select="math:atan2(22 div 0, 3)"/>
            </test4>
            <test5>                
                <xsl:value-of select="math:atan2(-22 div 0, 2)"/>
            </test5>
            <test6>                
                <xsl:value-of select="math:atan2(0, 0)"/>
            </test6>
        </out>
    </xsl:template>
</xsl:stylesheet>

  