<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="html" />

<xsl:template match="/">
<!-- Expect PASS, no script and document() -->
<xsl:value-of select="'PASS'"/>
</xsl:template>
</xsl:stylesheet>	
