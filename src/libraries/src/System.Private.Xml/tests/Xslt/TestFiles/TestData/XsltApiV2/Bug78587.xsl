<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:id="id" xmlns:cap="capitalizer">
	<xsl:template match="/">
		<out>
			<id><xsl:value-of select="id:GetId()" /></id>
			<ID><xsl:value-of select="cap:Capitalize(id:GetId())" /></ID>
		</out>
	</xsl:template>
</xsl:stylesheet>
