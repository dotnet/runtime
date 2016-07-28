<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:regexp="http://exslt.org/regular-expressions" exclude-result-prefixes="regexp">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:value-of select="regexp:replace(text, '&lt;[^>]*>', 'g', '')"/>              
            </test1>
            <test2>
                <xsl:value-of select="regexp:replace(text, '&lt;[^>]*>', '', '')"/>              
            </test2>
            <test3>
                <xsl:value-of select="regexp:replace('fooBar', 'BAR', 'i', 'Baz')"/>              
            </test3>
            <test4>
                <xsl:value-of select="regexp:replace(/no/such/node, 'BAR', 'i', 'Baz')"/>              
            </test4>            
        </out>
    </xsl:template>
</xsl:stylesheet>

  