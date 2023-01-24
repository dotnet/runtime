<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">


    <xsl:template match="/">

	<result>

		Test1<xsl:value-of select="myObj:DoNothing()"/>
		Test2: <xsl:value-of select="myObj:GetUninitialized()"/>
	</result>

    </xsl:template>
</xsl:stylesheet>