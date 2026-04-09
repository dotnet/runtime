<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<xsl:output method="xml" omit-xml-declaration="yes"/>
	<xsl:template match="root">
		<out>
		<xsl:for-each select="test">
			<xsl:if test="position()&lt;3"><xsl:value-of select="."/>,</xsl:if>
		</xsl:for-each>
		</out>
 	</xsl:template>
 </xsl:stylesheet>
