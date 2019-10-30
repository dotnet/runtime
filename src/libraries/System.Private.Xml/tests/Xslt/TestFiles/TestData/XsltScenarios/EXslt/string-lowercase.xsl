<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:str2="http://gotdotnet.com/exslt/strings" 
extension-element-prefixes="str2">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:value-of select="str2:lowercase(input)"/>
            </test1>
            <test2>
                <xsl:value-of select="str2:lowercase('')"/>
            </test2>            
            <test3>
                <xsl:value-of select="str2:lowercase(input-ru)"/>
            </test3>
        </out>
    </xsl:template>
</xsl:stylesheet>

  