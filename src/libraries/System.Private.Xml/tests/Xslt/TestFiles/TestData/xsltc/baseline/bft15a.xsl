<xsl:stylesheet version= '1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform' >  

    <xsl:include href="bft15b.xsl"/>

    <xsl:template match="/">  
        <xsl:for-each select="//foo">
        <xsl:value-of select="."/>
        </xsl:for-each>
    </xsl:template>  
 
</xsl:stylesheet>  
