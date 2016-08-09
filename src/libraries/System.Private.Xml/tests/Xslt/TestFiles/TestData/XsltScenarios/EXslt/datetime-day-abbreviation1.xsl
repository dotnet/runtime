<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:date2="http://gotdotnet.com/exslt/dates-and-times" exclude-result-prefixes="date2">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>                
Day name abbreviation in Swedish: <xsl:value-of select="date2:day-abbreviation(date, 'sv-SE')"/>
            </test1>
            <test2>                
                <xsl:value-of select="date2:day-abbreviation(date, 'no-such-culture')"/>
            </test2>
            <test3>                
                <xsl:value-of select="date2:day-abbreviation(bad-data, 'en-US')"/>
            </test3>                              
        </out>
    </xsl:template>
</xsl:stylesheet>

  