<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

  <xsl:variable name="param1" select="'global-param1-default'" /><!-- 'global-param1-arg' will be given -->
  <xsl:param name="param2" select="'global-param2-default'" /><!-- this parameter won't be specified -->


  <xsl:template match="/">
  <out>
    <xsl:call-template name="Test">
      <xsl:with-param name="param1" select="'local-param1-arg'" />
      <xsl:with-param name="param2" select="'local-param2-arg'" />
    </xsl:call-template>
  </out>
  </xsl:template>

  <xsl:template name="Test">
    <xsl:param name="param1" select="'local-param1-default'" />
    <xsl:param name="param2" select="'local-param2-default'" />

<xsl:text>
param1 (correct answer is 'local-param1-arg'): </xsl:text><xsl:value-of select="$param1" /><xsl:text>
param2 (correct answer is 'local-param2-arg'): </xsl:text><xsl:value-of select="$param2" /><xsl:text>
</xsl:text>
  </xsl:template>
</xsl:stylesheet>