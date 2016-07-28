<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">


    <xsl:template match="/">

	<result>

		String  Argument: <xsl:value-of select="myObj:ArgStringTest('Hello')"/>
		Double  Argument: <xsl:value-of select="myObj:ArgDoubleTest(3.14)"/>
		Boolean Argument: <xsl:value-of select="myObj:ArgBoolTest(true())"/>
		Boolean True Argument: <xsl:value-of select="myObj:ArgBoolTest(false())"/>
	</result>
	
    </xsl:template>
</xsl:stylesheet>