<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:exsl="http://exslt.org/common" exclude-result-prefixes="exsl">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="/">
        <out>
            <test1>
                <xsl:variable name="rtf">
                    <foo>
                        <bar>baz</bar>
                    </foo>
                </xsl:variable> 
                <xsl:for-each select="exsl:node-set($rtf)/*">
                    <xsl:copy-of select="."/>                    
                </xsl:for-each>
            </test1>            
            <test2>
                <xsl:variable name="rtf">
                    <foo>
                        <bar>baz</bar>
                    </foo>
                    some text                  
                </xsl:variable> 
                <xsl:for-each select="exsl:node-set($rtf)/node()">
                    <xsl:copy-of select="."/>                    
                </xsl:for-each>
            </test2>
            <test3>
                <xsl:variable name="var" select="/*"/>                    
                <xsl:for-each select="exsl:node-set($var)/*">
                    <xsl:copy-of select="."/>                    
                </xsl:for-each>
            </test3>                        
            <test4>                
                <xsl:for-each select="exsl:node-set('text')">
                    <xsl:copy-of select="."/>                    
                </xsl:for-each>
            </test4>            
        </out>
    </xsl:template>
</xsl:stylesheet>

  