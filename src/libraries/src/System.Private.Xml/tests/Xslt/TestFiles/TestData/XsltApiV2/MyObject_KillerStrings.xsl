<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">
    <xsl:template match="/">

	<result>
		<Gun><xsl:value-of select="myObj:MessMeUp()"/></Gun>
		<Missle><xsl:value-of select="myObj:MessMeUp2()"/></Missle>
		<Nuclear><xsl:value-of select="myObj:MessMeUp3()"/></Nuclear>
		<End>Wow...survived all killer ammo.</End>
	</result>
	
    </xsl:template>
</xsl:stylesheet>