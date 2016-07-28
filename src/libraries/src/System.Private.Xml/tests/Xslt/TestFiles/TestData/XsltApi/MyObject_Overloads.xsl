<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">


    <xsl:template match="/">

	<result>
		Overloaded Double: <xsl:value-of select="myObj:OverloadType(1234.012)"/>
		Overloaded Int: <xsl:value-of select="myObj:OverloadType(1234)"/>
		Overloaded String: <xsl:value-of select="myObj:OverloadType('This is a test')"/>
	</result>
	
    </xsl:template>
</xsl:stylesheet>