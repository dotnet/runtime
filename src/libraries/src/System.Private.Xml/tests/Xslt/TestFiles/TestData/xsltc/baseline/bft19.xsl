<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
     version="1.0">

  <xsl:import href=".\bft19a.xsl"/>

  <xsl:template match="foo">
    <html>
      <xsl:apply-templates/>
    </html>
  </xsl:template>

</xsl:stylesheet>

