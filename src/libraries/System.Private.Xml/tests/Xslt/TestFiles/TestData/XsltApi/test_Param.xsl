<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:param name="myParam1" select="'No Value Specified'"/>

	<xsl:template match="/">
		<out>Param: <xsl:value-of select="$myParam1" /></out>
	</xsl:template>

</xsl:stylesheet>
