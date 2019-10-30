<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:date="http://exslt.org/dates-and-times" exclude-result-prefixes="date">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:value-of select="date:month-abbreviation(date)"/>                                
            </test1>
            <test2>
                <xsl:value-of select="date:month-abbreviation(bad-date)"/>                                
            </test2>
            <test3>
                <xsl:value-of select="date:month-abbreviation('2004-01-19T07:59:02')"/>                                
            </test3>
            <test4>
                <xsl:value-of select="date:month-abbreviation('2004-02-19T07:59:02')"/>                                
            </test4>
            <test5>
                <xsl:value-of select="date:month-abbreviation('2004-03-19T07:59:02')"/>                                
            </test5>
            <test6>
                <xsl:value-of select="date:month-abbreviation('2004-04-19T07:59:02')"/>                                
            </test6>
            <test7>
                <xsl:value-of select="date:month-abbreviation('2004-05-19T07:59:02')"/>                                
            </test7>
            <test8>
                <xsl:value-of select="date:month-abbreviation('2004-06-19T07:59:02')"/>                                
            </test8>
            <test9>
                <xsl:value-of select="date:month-abbreviation('2004-07-19T07:59:02')"/>                                
            </test9>
            <test10>
                <xsl:value-of select="date:month-abbreviation('2004-08-19T07:59:02')"/>                                
            </test10>
            <test11>
                <xsl:value-of select="date:month-abbreviation('2004-09-19T07:59:02')"/>                                
            </test11>
            <test12>
                <xsl:value-of select="date:month-abbreviation('2004-10-19T07:59:02')"/>                                
            </test12>
            <test13>
                <xsl:value-of select="date:month-abbreviation('2004-11-19T07:59:02')"/>                                
            </test13>
            <test14>
                <xsl:value-of select="date:month-abbreviation('2004-12-19T07:59:02')"/>                                
            </test14>
            <test15>
                <xsl:value-of select="date:month-abbreviation('2004-13-19T07:59:02')"/>                                
            </test15>    
        </out>
    </xsl:template>
</xsl:stylesheet>

  