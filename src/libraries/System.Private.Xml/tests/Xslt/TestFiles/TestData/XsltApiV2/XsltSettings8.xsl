<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
			 xmlns:ms="urn:schemas-microsoft-com:xslt"
			 xmlns:user1="user1" xmlns:user2="user2" xmlns:user3="user3"
			 exclude-result-prefixes="ms user1 user2 user3">
<xsl:output method="html" />

<ms:script language="C#" implements-prefix="user1">
public int add(int i, int j)
{
	return i+j;
}
</ms:script>

<ms:script language="JScript" implements-prefix="user2">
function sub(i, j)
{
	return i-j;
}
</ms:script>

<ms:script language="VB" implements-prefix="user3">
Function div(i, j)
	div = i/j
End Function
</ms:script>

<xsl:template match="/">
<xsl:value-of select="user1:add(10, 20)"/>,<xsl:value-of select="user2:sub(20, 10)"/>,<xsl:value-of select="user3:div(20, 10)"/>
</xsl:template>
</xsl:stylesheet>	

