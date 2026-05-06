<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="html" />

<xsl:template match="/">
<!-- Expected 7 -->
<xsl:value-of select="count(document('books.xml')//book)"/>
</xsl:template>
</xsl:stylesheet>	
