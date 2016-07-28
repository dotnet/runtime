<?xml version="1.0" encoding="utf-8"?>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
                xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl">
  <xsl:output method="xml" indent="yes" />

  <xsl:template match="node()">
    <xsl:for-each select="/*">
      <xsl:text>Hello </xsl:text>
      <xsl:value-of select="local-name()" />
      <xsl:text>!</xsl:text>
    </xsl:for-each>
  </xsl:template>
</xsl:stylesheet>