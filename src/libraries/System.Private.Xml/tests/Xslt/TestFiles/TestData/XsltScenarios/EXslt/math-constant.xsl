<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:math="http://exslt.org/math" exclude-result-prefixes="math">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="/">
        <out>
            <test1>                
                <xsl:value-of select="math:constant('PI', 10)"/>
            </test1>
            <test2>                
                <xsl:value-of select="math:constant('E', 100)"/>
            </test2>
            <test3>                
                <xsl:value-of select="math:constant('SQRRT2', 10)"/>
            </test3>
            <test4>                
                <xsl:value-of select="math:constant('LN2', 3)"/>
            </test4>
            <test5>                
                <xsl:value-of select="math:constant('LN10', 3)"/>
            </test5>
            <test6>                
                <xsl:value-of select="math:constant('LOG2E', 4)"/>
            </test6>
            <test7>                
                <xsl:value-of select="math:constant('SQRT1_2', 5)"/>
            </test7>
            <test8>                
                <xsl:value-of select="math:constant('PI', -1)"/>
            </test8>
            <test9>                
                <xsl:value-of select="math:constant('PI', 22 div 0)"/>
            </test9>
            <test10>                
                <xsl:value-of select="math:constant('PI', -22 div 0)"/>
            </test10>
        </out>
    </xsl:template>
</xsl:stylesheet>

  