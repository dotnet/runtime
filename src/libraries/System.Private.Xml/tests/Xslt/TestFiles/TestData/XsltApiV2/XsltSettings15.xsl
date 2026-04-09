<!DOCTYPE xsl:stylesheet [
<!ENTITY doc "document('http://webxtest/testcases/books.xml')">
]>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="html" />

<xsl:template match="/">
<xsl:value-of select="&doc;"/>
</xsl:template>

</xsl:stylesheet>