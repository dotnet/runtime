<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

    <xsl:template match="/">

	<result>

		<xsl:for-each select="document('xmlResolver_document_function.xml')//elem">
			<elem><xsl:value-of select="."/></elem>
		</xsl:for-each>

	</result>
	
	</xsl:template>
    
</xsl:stylesheet>