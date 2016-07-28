<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:set="http://exslt.org/sets" exclude-result-prefixes="set">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>                
                <xsl:copy-of select="set:distinct(orders/order/product)"/>    
            </test1>
            <test2>                
                <xsl:copy-of select="set:distinct(set/*)"/>    
            </test2>
            <test3>                
                <xsl:copy-of select="set:distinct(/no/such/nodes)"/>    
            </test3>                     
        </out>
    </xsl:template>
</xsl:stylesheet>