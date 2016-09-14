<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
  <xsl:template  match="bbb">
    <xsl:variable  name="xxx">L-A</xsl:variable>
    <xsl:variable  name="te1" select="$A"></xsl:variable>
    <xsl:variable  name="te3" select="$C"></xsl:variable>
    <xsl:variable  name="te2" select="$B"></xsl:variable>
    <xsl:variable  name="te5" select="$te2"></xsl:variable>
    <xsl:variable  name="te6" select="$te2"></xsl:variable>
    <xsl:variable  name="te7" select="$te6"></xsl:variable>
    <xsl:variable  name="te4" select="$te1"></xsl:variable>

    <bbb>
      <local>
        <xsl:value-of  select="$xxx"/>
      </local>
      <global>
        <xsl:value-of  select="$B"/>
      </global>
      <tel>
        <xsl:value-of  select="$te1"/>
        <xsl:value-of  select="$te7"/>
      </tel>
    </bbb>
    
    
  </xsl:template>

  <xsl:variable name="C" select="$A > $B"/>
  <xsl:variable name="A" select="1"/>
  <xsl:variable name="B" select="2"/>



</xsl:stylesheet>