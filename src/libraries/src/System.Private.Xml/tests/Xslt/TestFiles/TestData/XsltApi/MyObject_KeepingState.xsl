<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">


    <xsl:template match="/">

	<result>
		A:<xsl:value-of select="myObj:MyValue()"/>
		B:<xsl:value-of select="myObj:ReduceCount(4)"/>
		C:<xsl:value-of select="myObj:MyValue()"/>
		D:<xsl:value-of select="myObj:ReduceCount(0)"/>
		E:<xsl:value-of select="myObj:ReduceCount(-14)"/>
		F:<xsl:value-of select="myObj:AddToString('Hello ')"/>
		G:<xsl:value-of select="myObj:AddToString('World ')"/>
		E:<xsl:value-of select="myObj:ReduceCount(50.2)"/>
	</result>
	
    </xsl:template>
</xsl:stylesheet>