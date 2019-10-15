<!DOCTYPE stylesheet [
  <!ENTITY myent  "RESOLVED_ENTITY" >
]>

<xsl:stylesheet version= '1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform' >

    <xsl:template match="/">
    <testOut>
      Let's see if we can resolve entities: &myent; 
    </testOut>
  </xsl:template>

</xsl:stylesheet>
