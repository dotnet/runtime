<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl exobj" xmlns:exobj="urn-myobject">

<xsl:output method="xml" indent="yes"/>

<xsl:template match="/">
    <out>
	<xsl:call-template name="book">	
		<xsl:with-param name="titles" select="exobj:ReturnNodeSet('/books/book')/title"/>
	</xsl:call-template>
    </out>
</xsl:template>

<xsl:template name="book">
	<xsl:param name="titles"/>
	<xsl:copy-of select="$titles"/>
</xsl:template>

</xsl:stylesheet>
