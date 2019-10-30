<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:msxsl="urn:schemas-microsoft-com:xslt" xmlns:custom="ns:custom" exclude-result-prefixes="msxsl custom">

    <!-- Root template  -->
    <xsl:template match="/">
	<out>
		<xsl:value-of select="custom:safe()"/>
	</out>
    </xsl:template>
	
    <!-- Schema Production -->
    <msxsl:script language="C#" implements-prefix="custom">
	public string dangerous()
	{
		return ("You have been hacked");
	}

	public string safe()
	{
		return ("You are safe");
	}

    </msxsl:script>

</xsl:stylesheet>
