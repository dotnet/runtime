<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:regexp2="http://gotdotnet.com/exslt/regular-expressions" 
exclude-result-prefixes="regexp2">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:copy-of select="regexp2:tokenize(input, ' ')"/>                          
            </test1>
            <test2>
                <xsl:copy-of select="regexp2:tokenize(input, ' ', 'm')"/>                          
            </test2>
            <test3>
                <xsl:copy-of select="regexp2:tokenize(input, ' ', 'i')"/>                          
            </test3>
            <test4>
                <xsl:copy-of select="regexp2:tokenize(input, ' ', 'mi')"/>                          
            </test4>
            <test5>
                <xsl:copy-of select="regexp2:tokenize(input, ' ', '')"/>                          
            </test5>
            <test6>
                <xsl:copy-of select="regexp2:tokenize(/no/such/node, ' ')"/>                          
            </test6>
            <test7>
                <xsl:copy-of select="regexp2:tokenize(input, '')"/>                          
            </test7>
        </out>
    </xsl:template>
</xsl:stylesheet>

  