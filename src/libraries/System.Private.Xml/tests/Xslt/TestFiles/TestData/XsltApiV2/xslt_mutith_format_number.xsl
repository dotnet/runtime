<?xml version="1.0"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<xsl:template match="/">
		<html>
 			<head>
 			</head>
 			<body>
 				<xsl:value-of select="format-number(12, '0')"/>
 				<xsl:value-of select="."/>
 			</body>
 		</html>
 	</xsl:template>
 </xsl:stylesheet>
