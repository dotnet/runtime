<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:output method="xml" encoding="Shift_JIS" indent="yes" />

  <xsl:template match="/">
    <xsl:copy-of select="content" />
  </xsl:template>
</xsl:stylesheet>
