<xsl:stylesheet
xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0"
xmlns:ext="http://foo.com" extension-element-prefixes="ext">
    <xsl:template match="/">
        <distinct-countries>
            <xsl:for-each select="ext:distinct(//@country)">
                <xsl:sort select="." />
                <xsl:value-of select="."/>
                <xsl:if test="position() != last()">, </xsl:if>
            </xsl:for-each>
        </distinct-countries>
    </xsl:template>
</xsl:stylesheet>
