<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:output omit-xml-declaration="yes"/>

<xsl:template match="/">
    <xsl:copy-of select="document('Stra%C3%9Fe.xml')"/>
</xsl:template>

</xsl:stylesheet>
