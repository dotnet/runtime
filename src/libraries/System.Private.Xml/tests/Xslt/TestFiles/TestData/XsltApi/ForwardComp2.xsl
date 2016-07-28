<xsl:stylesheet version="1.5" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
    <xsl:template match="/">
        <foo bar="{+1}"/>
    </xsl:template>
</xsl:stylesheet>