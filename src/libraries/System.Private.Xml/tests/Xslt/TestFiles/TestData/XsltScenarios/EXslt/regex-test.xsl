<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:regexp="http://exslt.org/regular-expressions" exclude-result-prefixes="regexp">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                  <xsl:if test="not(regexp:test(email/valid, 
                  '\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*'))">Email address is not valid.</xsl:if>              
            </test1>
            <test2>
                  <xsl:if test="not(regexp:test(email/invalid, 
                  '\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*'))">Email address is not valid.</xsl:if>              
            </test2>            
            <test3>
                  <xsl:if test="not(regexp:test('', 
                  '\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*'))">Email address is not valid.</xsl:if>              
            </test3>
            <test4>
                  <xsl:if test="not(regexp:test(email/valid, ''))">Email address is not valid.</xsl:if>              
            </test4>
            <test5>
                  <xsl:if test="not(regexp:test(email/valid, 
                  '\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*', ''))">Email address is not valid.</xsl:if>              
            </test5>
            <test6>
                  <xsl:if test="not(regexp:test(email/valid, 
                  '\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*', 'dummy'))">Email address is not valid.</xsl:if>              
            </test6>            
            <test7>
                  <xsl:if test="not(regexp:test(email/valid, 
                  '\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*', 'g'))">Email address is not valid.</xsl:if>              
            </test7>
            <test8>
                  <xsl:if test="regexp:test('FOO', 'foo', 'i')">Ok</xsl:if>              
            </test8>
            <test9>
                  <xsl:if test="regexp:test('FOO', 'foo')">Ok</xsl:if>              
            </test9>
        </out>
    </xsl:template>
</xsl:stylesheet>

  