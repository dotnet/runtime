<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" omit-xml-declaration="yes" />

<xsl:param name="myArg1" select="'No Value Specified'"/>
<xsl:param name="myArg2" select="'No Value Specified'"/>
<xsl:param name="myArg3" select="'No Value Specified'"/>
<xsl:param name="myArg4" select="'No Value Specified'"/>
<xsl:param name="myArg5" select="'No Value Specified'"/>
<xsl:param name="myArg6" select="'No Value Specified'"/>

    <xsl:template match="/">
	<result>
		<arg1><xsl:value-of select="$myArg1" /></arg1>
		<arg2><xsl:value-of select="$myArg2" /></arg2>
		<arg3><xsl:value-of select="$myArg3" /></arg3>
		<arg4><xsl:value-of select="$myArg4" /></arg4>
		<arg5><xsl:value-of select="$myArg5" /></arg5>
		<arg6><xsl:value-of select="$myArg6" /></arg6>
	</result>
    </xsl:template>

</xsl:stylesheet>