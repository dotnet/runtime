<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:date="http://exslt.org/dates-and-times" exclude-result-prefixes="date">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:value-of select="date:difference(date, date2)"/>                                
            </test1>
            <test2>
                <xsl:value-of select="date:difference(date2, date)"/>                                                                
            </test2>
            <test3>
                <xsl:value-of select="date:difference(bad-date, date)"/>                                                                
            </test3>
            <test4>
                <xsl:value-of select="date:difference('', date)"/>                                                                
            </test4>                            
        </out>
    </xsl:template>
</xsl:stylesheet>

  