<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
     version="1.0">

  <xsl:include href=".\bft20a.xsl"/>

  <xsl:template match="foo">
    <html>
      <xsl:apply-templates/>
    </html>
  </xsl:template>

</xsl:stylesheet>

