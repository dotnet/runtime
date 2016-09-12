<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
			 xmlns:ms="urn:schemas-microsoft-com:xslt"
			 xmlns:user="user"
			 exclude-result-prefixes="ms user">
<xsl:output method="html" />

<ms:script language="C#" implements-prefix="user">
public int add(int i, int j)
{
	return i+j;
}
</ms:script>

<xsl:template match="/">
<!-- invalidating the XSLT script by introducing non existing function -->
<xsl:value-of select="user:badfn(10, count(document('books.xml')//book))" />
</xsl:template>
</xsl:stylesheet>