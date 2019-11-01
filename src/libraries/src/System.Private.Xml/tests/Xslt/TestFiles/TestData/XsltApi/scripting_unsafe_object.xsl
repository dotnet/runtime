<xsl:stylesheet version="1.0"
        xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
        xmlns:msxsl="urn:schemas-microsoft-com:xslt"
	xmlns:xslscript="my:urn">

<msxsl:script language="JScript" implements-prefix="xslscript">
function createFile()
{
	var fso = new ActiveXObject("Scripting.FileSystemObject");
	var a = fso.CreateTextFile("c:\MSXMLSec\foo.txt");
}
</msxsl:script>

<xsl:template match="/">
<html>
<body>
<xsl:value-of select="xslscript:createFile()"/>
</body>
</html>
</xsl:template>

</xsl:stylesheet>
