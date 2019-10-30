<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">


    <xsl:template match="/">

	<result>
		Recursive Function Returning the factorial of five:<xsl:value-of select="myObj:RecursionSample()"/>
	</result>
	
    </xsl:template>
</xsl:stylesheet>