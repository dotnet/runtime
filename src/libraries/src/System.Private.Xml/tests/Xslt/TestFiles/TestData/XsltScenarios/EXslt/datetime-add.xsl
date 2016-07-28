<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:date="http://exslt.org/dates-and-times" exclude-result-prefixes="date">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:value-of select="date:add(date, 'P2D')"/>
            </test1>
            <test2>
                <xsl:value-of select="date:add(date2, 'PT5H2M')"/>
            </test2>
            <test3>
                <xsl:value-of select="date:add(bad-date, 'P2Y')"/>
            </test3>
            <test4>
                <xsl:value-of select="date:add(date3, 'P2DT1H1M')"/>
            </test4>
            <test5>
                <xsl:value-of select="date:add('2000-01-12T12:13:14Z', 'P1Y3M5DT7H10M3S')"/>
            </test5>
            <test6>
                <xsl:value-of select="date:add('2000-01', '-P3M')"/>
            </test6>
            <test7>
                <xsl:value-of select="date:add('2000-01-12', 'PT33H')"/>
            </test7>            
        </out>
    </xsl:template>
</xsl:stylesheet>

  