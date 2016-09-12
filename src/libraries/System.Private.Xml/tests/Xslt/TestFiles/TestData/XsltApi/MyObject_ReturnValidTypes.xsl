<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">


    <xsl:template match="/">

	<result>
		Get A String:<xsl:value-of select="myObj:ReturnString()"/>
		Get A Double:<xsl:value-of select="myObj:ReturnDouble()"/>
		Get A True Boolean:<xsl:value-of select="myObj:ReturnBooleanTrue()"/>
		Get A False Boolean:<xsl:value-of select="myObj:ReturnBooleanFalse()"/>
		Get An Int:<xsl:value-of select="myObj:ReturnInt()"/>
		Get Other with ToString() Support:<xsl:value-of select="myObj:ReturnOther()"/>
		Call function with no return type:<xsl:value-of select="myObj:DoNothing()"/>

	</result>
	
    </xsl:template>
</xsl:stylesheet>