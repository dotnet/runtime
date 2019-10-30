<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">


    <xsl:template match="/">

	<result>

		Aiming with Gun: <xsl:value-of select="myObj:MessMeUp()"/>
		Aiming with Missile: <xsl:value-of select="myObj:MessMeUp2()"/>
		Aiming with Nuclear: <xsl:value-of select="myObj:MessMeUp3()"/>
		Wow...survived all killer ammo.
	</result>
	
    </xsl:template>
</xsl:stylesheet>