<xsl:stylesheet xmlns:xsl = "http://www.w3.org/1999/XSL/Transform" version = "1.0" > 
    <xsl:output method = "xml" /> 
    
	<xsl:key name = "k1" match = "BBB" use = "@bbb" /> 
    
	<xsl:template match = "/" > 
        	<xsl:variable name="k1"  select="key('k1','22')"/> 
        	<xsl:variable name="k2"  select="//BBB"/> 
		k1 = <xsl:copy-of select ="$k1"/>
		k2 = <xsl:copy-of select ="$k2"/>
    </xsl:template>              
</xsl:stylesheet> 