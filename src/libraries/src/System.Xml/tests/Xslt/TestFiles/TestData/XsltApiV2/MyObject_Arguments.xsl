<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">
    <xsl:template match="/">
	<result>
		<string><xsl:value-of select="myObj:ArgStringTest('Hello')"/></string>
		<double><xsl:value-of select="myObj:ArgDoubleTest(3.14)"/></double>
		<boolean><xsl:value-of select="myObj:ArgBoolTest(true())"/></boolean>
		<boolean><xsl:value-of select="myObj:ArgBoolTest(false())"/></boolean>
	</result>
    </xsl:template>
</xsl:stylesheet>