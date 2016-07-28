<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:date2="http://gotdotnet.com/exslt/dates-and-times">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>                
                <xsl:value-of select="date2:max(timespan)"/>
            </test1>
            <test2>                
                <xsl:value-of select="date2:max(/no/such/nodes)"/>
            </test2>
            <test2>                
                <xsl:value-of select="date2:max(bad-data)"/>
            </test2>                              
        </out>
    </xsl:template>
</xsl:stylesheet>

  