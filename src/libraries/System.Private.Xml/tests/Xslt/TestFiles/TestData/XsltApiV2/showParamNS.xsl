<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
		xmlns:myObj1="urn:http://www.w3.org/1999/XSL/Transform"
		xmlns:myObj2="urn:tmp"
		xmlns:myObj3="urn:my-object"
		xmlns:myObj4="urn:MY-OBJECT"
		>

<xsl:param name="myObj1:myArg1" select="'No Value Specified'"/>
<xsl:param name="myObj2:myArg2" select="'No Value Specified'"/>
<xsl:param name="myObj3:myArg3" select="'No Value Specified'"/>
<xsl:param name="myObj4:myArg4" select="'No Value Specified'"/>
<xsl:param name="myObj4:myArg5" select="'No Value Specified'"/>
<xsl:param name="myObj3:myArg6" select="'No Value Specified'"/>

    <xsl:template match="/">

	<result>
		<arg1>1.<xsl:value-of select="$myObj1:myArg1" /></arg1>
		<arg2>2.<xsl:value-of select="$myObj2:myArg2" /></arg2>
		<arg3>3.<xsl:value-of select="$myObj3:myArg3" /></arg3>
		<arg4>4.<xsl:value-of select="$myObj4:myArg4" /></arg4>
		<arg5>5.<xsl:value-of select="$myObj4:myArg5" /></arg5>
		<arg6>6.<xsl:value-of select="$myObj3:myArg6" /></arg6>
	</result>
	
    </xsl:template>
</xsl:stylesheet>