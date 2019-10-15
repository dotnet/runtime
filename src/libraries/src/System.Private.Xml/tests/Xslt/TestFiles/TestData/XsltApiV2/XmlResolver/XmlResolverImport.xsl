<xsl:stylesheet version="1.0"
        xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:template match="orange">
	<orange>
		<xsl:attribute name="color">
			<xsl:value-of select="color"/>
		</xsl:attribute>
	</orange>
</xsl:template>

</xsl:stylesheet>
