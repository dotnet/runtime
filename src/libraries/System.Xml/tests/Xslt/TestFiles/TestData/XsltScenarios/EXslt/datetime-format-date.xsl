<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:date="http://exslt.org/dates-and-times" exclude-result-prefixes="date">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>                
                <xsl:value-of select="date:format-date(date, 'yyyy.MM.dd G HH:mm:ss z')"/>
            </test1>    
            <test2>
                <xsl:value-of select="date:format-date(date, 'EEE, MMM d, yyyy')"/>                
            </test2>
            <test3>
                <xsl:value-of select="date:format-date(date, 'h:mm a')"/>                
            </test3>
            <test4>
                <xsl:value-of select="date:format-date(date, 'hh a, zzzz')"/>                
            </test4>
            <test5>
                <xsl:value-of select="date:format-date(date, 'K:mm a, z')"/>                
            </test5>
            <test6>
                <xsl:value-of select="date:format-date(date, 'yyyyy.MMMMM.dd GGG hh:mm aaa')"/>                
            </test6>
            <test7>
                <xsl:value-of select="date:format-date(date, 'EEE, d MMM yyyy HH:mm:ss Z')"/>                
            </test7>
            <test8>
                <xsl:value-of select="date:format-date(date, 'yyMMddHHmmssZ')"/>                
            </test8>
            <test9>
                <xsl:value-of select="date:format-date(/no/such/node, 'yyMMddHHmmssZ')"/>                
            </test9>
            <test10>
                <xsl:value-of select="date:format-date(date, '')"/>                
            </test10>
        </out>
    </xsl:template>
</xsl:stylesheet>

  