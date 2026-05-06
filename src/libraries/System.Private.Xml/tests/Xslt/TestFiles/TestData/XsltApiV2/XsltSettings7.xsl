<!DOCTYPE xsl:stylesheet [
<!ENTITY script SYSTEM "script.txt">
]>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
			 xmlns:ms="urn:schemas-microsoft-com:xslt"
			 xmlns:user="user"
			 exclude-result-prefixes="ms user">
<xsl:output method="html" />

&script;

<xsl:template match="/">
<xsl:value-of select="user:add(10, 20)"/>
</xsl:template>
</xsl:stylesheet>	

