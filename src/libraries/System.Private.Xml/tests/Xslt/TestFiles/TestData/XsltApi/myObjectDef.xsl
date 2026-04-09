<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">


    <xsl:template match="/">

	<result>
		1.<xsl:value-of select="myObj:Fn1()"/>
		2.<xsl:value-of select="myObj:Fn2()"/>
		3.<xsl:value-of select="myObj:Fn3()"/>
	</result>
	
    </xsl:template>
</xsl:stylesheet>