<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:math="http://exslt.org/math" exclude-result-prefixes="math">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="/">
        <out>
            <test1>                
Max orders: <xsl:copy-of select="math:highest(/data/orders/order)"/>
            </test1>
            <test2>                
Max orders: <xsl:copy-of select="math:highest(/no/such/nodes)"/>
            </test2>
            <test3>                
Max orders: <xsl:copy-of select="math:highest(/data/bad-data)"/>
            </test3>                                    
        </out>
    </xsl:template>
</xsl:stylesheet>

  