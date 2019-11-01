<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
     version="1.0">

  <xsl:template match="bar">
    <b>
      <xsl:apply-templates/>
    </b>
  </xsl:template>

</xsl:stylesheet>

