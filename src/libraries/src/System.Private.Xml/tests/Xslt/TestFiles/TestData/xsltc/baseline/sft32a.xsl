<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
<xsl:template match="/">
  <out>
    <xsl:value-of select="document('mail.xml')/entry/name"/>
  </out>
</xsl:template>
</xsl:stylesheet>
