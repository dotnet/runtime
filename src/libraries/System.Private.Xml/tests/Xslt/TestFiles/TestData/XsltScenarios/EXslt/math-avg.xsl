<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:math2="http://gotdotnet.com/exslt/math" exclude-result-prefixes="math2">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>                
Average value: <xsl:value-of select="math2:avg(values/value)"/>.
            </test1>
            <test2>                
Average value: <xsl:value-of select="math2:avg(values/bad-value)"/>.
            </test2>
            <test3>                
Average value: <xsl:value-of select="math2:avg(no/such/nodes)"/>.
            </test3>                              
        </out>
    </xsl:template>
</xsl:stylesheet>

  