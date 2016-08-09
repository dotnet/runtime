<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

<xsl:output method="xml" omit-xml-declaration="yes" indent="yes"/>

	<xsl:template match="root">
		<body>
			<xsl:for-each select="test">
				<xsl:variable name="val" select="."/>
				<xsl:if test="$val = 1 and $val &lt; 2"> 
					<elem>
						<xsl:value-of select="."/>
					</elem>
				</xsl:if>
			</xsl:for-each>
		</body>
 	</xsl:template>
 </xsl:stylesheet>
