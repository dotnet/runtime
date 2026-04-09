<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">


    <xsl:template match="/">

	<result>

		String  Argument: <xsl:value-of xmlns:myObj="myArg1" select="myObj:GetType()"/>
		Int32   Argument: <xsl:value-of xmlns:myObj="myArg2" select="myObj:GetType()"/>
		Boolean Argument: <xsl:value-of xmlns:myObj="myArg3" select="myObj:GetType()"/>
		Boolean Argument: <xsl:value-of xmlns:myObj="myArg4" select="myObj:GetType()"/>
		Double  Argument: <xsl:value-of xmlns:myObj="myArg5" select="myObj:GetType()"/>
		String  Argument: <xsl:value-of xmlns:myObj="myArg6" select="myObj:GetType()"/>
	</result>
	
    </xsl:template>
</xsl:stylesheet>