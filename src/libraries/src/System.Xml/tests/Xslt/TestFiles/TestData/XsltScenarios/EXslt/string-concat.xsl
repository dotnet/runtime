<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:str="http://exslt.org/strings" exclude-result-prefixes="str">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:value-of 
                select="str:concat(list/*)"/>
            </test1>
            <test2>
                <xsl:value-of 
                select="str:concat(/no/such/nodes)"/>
            </test2>                        
        </out>
    </xsl:template>
</xsl:stylesheet>

  