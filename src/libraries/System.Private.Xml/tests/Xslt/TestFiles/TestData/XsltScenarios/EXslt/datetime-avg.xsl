<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:date2="http://gotdotnet.com/exslt/dates-and-times">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>                
Average time interval is <xsl:value-of select="date2:avg(timespan)"/>.
            </test1>
            <test2>                
Average time interval is <xsl:value-of select="date2:avg(/no/such/nodes)"/>.
            </test2>
            <test2>                
Average time interval is <xsl:value-of select="date2:avg(bad-data)"/>.
            </test2>                              
        </out>
    </xsl:template>
</xsl:stylesheet>

  