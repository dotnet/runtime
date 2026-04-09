<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl exobj" xmlns:exobj="urn-myobject">

<xsl:output method="xml" indent="yes"/>

<xsl:template match="/">
    <out>
	<xsl:for-each select="exobj:ReturnNodeSet('/books/book')">
		<!--this sort may not work because the context is defined in sort's select clause but 
			just testing to see if we can call an extension object from select attribute of xsl:sort-->
	        <xsl:sort select="exobj:ReturnNodeSet('/books/book')/title" />
	        <xsl:copy-of select="title" />
	</xsl:for-each>
    </out>
</xsl:template>

</xsl:stylesheet>
