<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">


    <xsl:template match="/">

	<result>
		Protected:<xsl:value-of select="myObj:ProtectedFunction()"/>
		You should not see this! Bug this if you do.
	</result>
	
    </xsl:template>
</xsl:stylesheet>