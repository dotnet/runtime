<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="http://www.miocrosoft.com/this/is/a/very/long/namespace/uri/to/do/the/api/testing/for/xslt/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/0123456789/">


    <xsl:template match="/">

	<result>
		1.<xsl:value-of select="myObj:Fn1()"/>
		2.<xsl:value-of select="myObj:Fn2()"/>
		3.<xsl:value-of select="myObj:Fn3()"/>
	</result>
	
    </xsl:template>
</xsl:stylesheet>