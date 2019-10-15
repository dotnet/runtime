<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:date="http://exslt.org/dates-and-times" exclude-result-prefixes="date">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:value-of select="date:day-name(date)"/>                                
            </test1>
            <test2>
                <xsl:value-of select="date:day-name(bad-date)"/>                                
            </test2>
            <test3>
                <xsl:value-of select="date:day-name('2004-09-19T07:59:02')"/>                                  
            </test3>
            <test4>
                <xsl:value-of select="date:day-name('2004-09-20T07:59:02')"/>                                  
            </test4>
            <test5>
                <xsl:value-of select="date:day-name('2004-09-21T07:59:02')"/>                                  
            </test5>
            <test6>
                <xsl:value-of select="date:day-name('2004-09-22T07:59:02')"/>                                  
            </test6>
            <test7>
                <xsl:value-of select="date:day-name('2004-09-23T07:59:02')"/>                                  
            </test7>
            <test8>
                <xsl:value-of select="date:day-name('2004-09-24T07:59:02')"/>                                  
            </test8>
            <test9>
                <xsl:value-of select="date:day-name('2004-09-25T07:59:02')"/>                                  
            </test9>    
        </out>
    </xsl:template>
</xsl:stylesheet>

  