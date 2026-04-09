<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:exsl="http://exslt.org/common" exclude-result-prefixes="exsl">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="/">
        <out>
            <test1>
                <xsl:variable name="node" select="/data"/>
                <xsl:variable name="string" select="'string'"/>
                <xsl:variable name="number" select="11"/>
                <xsl:variable name="rtf">
                    <foo/>
                </xsl:variable>                
                <xsl:variable name="boolean" select="true()"/>
                Node: <xsl:value-of select="exsl:object-type($node)"/>          
                String: <xsl:value-of select="exsl:object-type($string)"/>          
                Number: <xsl:value-of select="exsl:object-type($number)"/>          
                RTF: <xsl:value-of select="exsl:object-type($rtf)"/>          
                Boolean: <xsl:value-of select="exsl:object-type($boolean)"/>                          
            </test1>                                    
        </out>
    </xsl:template>
</xsl:stylesheet>

  