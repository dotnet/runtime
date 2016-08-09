<xsl:stylesheet version="1.5" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:output method="text"/>

<xsl:template match="/">
    <xsl:value-of select="1.23e2"/>
    <xsl:value-of select="4.567e3"/>
</xsl:template>

</xsl:stylesheet>