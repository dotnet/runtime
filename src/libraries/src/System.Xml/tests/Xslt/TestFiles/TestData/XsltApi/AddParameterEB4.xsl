
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<xsl:output method="xml" omit-xml-declaration="yes" />
<xsl:param name="param1" select="'default global'"/>

<xsl:template match="/">
    <xsl:call-template name="Test">
	<xsl:with-param name="param1" select="'with-param'" />
    </xsl:call-template>	
</xsl:template>

<xsl:template name="Test">
	<xsl:variable name="param1" select="'default local'" />
	<result><xsl:value-of select="$param1" /></result>
</xsl:template>

</xsl:stylesheet>