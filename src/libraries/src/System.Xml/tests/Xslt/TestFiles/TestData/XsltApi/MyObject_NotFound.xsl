<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">


    <xsl:template match="/">

	<result>
		<xsl:value-of select="myObj:ImNotImplemented()"/>
	</result>
	
    </xsl:template>
</xsl:stylesheet>