<xsl:stylesheet version="1.0"
        xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:import href="XmlResolverImport.xsl"/>

<xsl:include href="XmlResolverInclude.xsl"/>

<xsl:template match="/">
	<out>
		<xsl:apply-templates/>
	</out>
</xsl:template>

</xsl:stylesheet>
