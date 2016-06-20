<?xml version='1.0'?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0"><xsl:output method="xml" omit-xml-declaration="yes" indent="yes"/>
	<xsl:variable name="image-dir">/images</xsl:variable>
	<xsl:template match="photograph">
		<img src="{$image-dir}/{href}" width="{size/@width}"/>
	</xsl:template>
</xsl:stylesheet>
