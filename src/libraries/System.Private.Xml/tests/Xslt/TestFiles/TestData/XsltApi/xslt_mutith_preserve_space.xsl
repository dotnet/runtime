<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

<xsl:output method="xml" omit-xml-declaration="yes" indent="yes"/>

<xsl:strip-space elements="*"/>

<xsl:preserve-space elements="test1"/>

	<xsl:template match="root">
		<body>
			<xsl:for-each select="test1">
				<elem>
					<xsl:value-of select="."/>
				</elem>
			</xsl:for-each>
			<xsl:for-each select="test2">
				<elem>
					<xsl:value-of select="."/>
				</elem>
			</xsl:for-each>
		</body>
 	</xsl:template>
 </xsl:stylesheet>
