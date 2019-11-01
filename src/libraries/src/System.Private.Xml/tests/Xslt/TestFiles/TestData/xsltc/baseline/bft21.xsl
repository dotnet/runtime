<?xml version="1.0" encoding="utf-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

  <xsl:template match="foo">
    <HTML>
      <BODY>
        <H3>Foo Listing</H3>

          <xsl:for-each select="bar">
		  <xsl:value-of select="document('.\bft21a.xml')/Foos/Foo[Id=1]/Name"/>
          </xsl:for-each>

      </BODY>
    </HTML>
  </xsl:template>

</xsl:stylesheet>

