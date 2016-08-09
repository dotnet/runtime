<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:random="http://exslt.org/random" exclude-result-prefixes="random">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>                
                <xsl:value-of select="count(random:random-sequence(10))"/>    
            </test1>
            <test2>                
                <xsl:value-of select="count(random:random-sequence())"/>    
            </test2>                  
            <test3>                
                <xsl:value-of select="count(random:random-sequence(0))"/>    
            </test3>
            <test4>                
                <xsl:value-of select="count(random:random-sequence(3.5, 0.5))"/>    
            </test4>               
            <test5>                
                <xsl:value-of select="count(random:random-sequence(-5))"/>    
            </test5>
            <test6>                
                <xsl:value-of select="count(random:random-sequence(5, 0))"/>    
            </test6>
            <test7>                
                <xsl:value-of select="count(random:random-sequence(5, 5 div 0))"/>    
            </test7>
            <test8>                
                <xsl:value-of select="count(random:random-sequence(5, -0.5))"/>    
            </test8>
        </out>
    </xsl:template>
</xsl:stylesheet>

  