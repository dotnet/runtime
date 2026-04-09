<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:str="http://exslt.org/strings" exclude-result-prefixes="str">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:copy-of 
                select="str:split('a, simple, list', ', ')"/>
            </test1>
            <test2>
                <xsl:copy-of 
                select="str:split('date math str')"/>
            </test2>                        
            <test3>
                <xsl:copy-of 
                select="str:split('foo', '')"/>
            </test3>
            <test4>
                <xsl:copy-of 
                select="str:split('cats and dogs', ' and ')"/>
            </test4>
            <test5>
                <xsl:copy-of 
                select="str:split('', ' and ')"/>
            </test5>
        </out>
    </xsl:template>
</xsl:stylesheet>

  