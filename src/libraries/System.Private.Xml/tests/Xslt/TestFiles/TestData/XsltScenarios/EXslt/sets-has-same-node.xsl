<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:set="http://exslt.org/sets" exclude-result-prefixes="set">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>                
                <xsl:copy-of select="set:has-same-node(set/*, set/foo)"/>    
            </test1>               
            <test2>                
                <xsl:copy-of select="set:has-same-node(set/foo[not(@bar)], set/foo[@bar])"/>    
            </test2>                  
            <test3>                
                <xsl:copy-of select="set:has-same-node(set/*, /no/such/nodes)"/>    
            </test3>               
        </out>
    </xsl:template>
</xsl:stylesheet>