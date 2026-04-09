<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

<xsl:output method="xml" omit-xml-declaration="yes" indent="yes"/>

<xsl:variable name="var" select="//test"/>

	<xsl:template match="/">
		<out>
			<xsl:for-each select="$var">
				<xsl:value-of select="."/>
			</xsl:for-each>
		</out>
 	</xsl:template>

 </xsl:stylesheet>
