<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:math="http://exslt.org/math" exclude-result-prefixes="math">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="/">
        <out>
            <test1>                
                <xsl:variable name="rnd" select="math:random()"/>
                <xsl:value-of select="$rnd >= 0 and $rnd &lt;= 1"/>
            </test1>            
        </out>
    </xsl:template>
</xsl:stylesheet>

  