<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="html" />

<xsl:template match="/">
<xsl:value-of select="document('http://IDontExist/IDontExist.xml', //book)"/>
</xsl:template>
</xsl:stylesheet>