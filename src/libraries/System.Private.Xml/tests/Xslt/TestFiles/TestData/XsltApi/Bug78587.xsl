<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:id="id" xmlns:cap="capitalizer">
	<xsl:template match="/">
		<out>
			ID: <xsl:value-of select="id:GetId()" />
			Capitalized ID: <xsl:value-of select="cap:Capitalize(id:GetId())" />
		</out>
	</xsl:template>
</xsl:stylesheet>
