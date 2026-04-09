<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">


<xsl:param name="myArg1" select="'No Value Specified'"/>
<xsl:param name="myArg2" select="'No Value Specified'"/>
<xsl:param name="myArg3" select="'No Value Specified'"/>
<xsl:param name="myArg4" select="'No Value Specified'"/>
<xsl:param name="myArg5" select="'No Value Specified'"/>
<xsl:param name="myArg6" select="'No Value Specified'"/>

    <xsl:template match="/">

	<result>
		1.<xsl:value-of select="$myArg1" />
		2.<xsl:value-of select="$myArg2" />
		3.<xsl:value-of select="$myArg3" />
		4.<xsl:value-of select="$myArg4" />
		5.<xsl:value-of select="$myArg5" />
		6.<xsl:value-of select="$myArg6" />
	</result>
	
    </xsl:template>
</xsl:stylesheet>