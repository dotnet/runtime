<?xml version="1.0"?>

<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>

<xsl:output method="html" indent="yes"/>

<xsl:param name="ns"/>

<xsl:template match="/">
  <h2>Classes in <xsl:value-of select="$ns"/></h2>
  <table border="1">
    <tr>
      <td>Class Name</td>
      <td>Head Maintainer</td>
      <td>Last Activity</td>
      <td>Implementation</td>
      <td>Test Suite</td>
      <td>Completion</td>
    </tr>

  <xsl:for-each select='classes/class'>
    <xsl:sort select='@name' />
    <xsl:if test="starts-with(@name, $ns) and not(contains(substring-after(@name, concat($ns, '.')), '.'))">
      <tr>
        <td><xsl:value-of select="@name"/></td>
        <td><a href='mailto:{maintainers/maintainer[1]}'><xsl:value-of select="maintainers/maintainer[1]"/></a></td>
        <td><xsl:value-of select="last-activity"/></td>
        <td><xsl:value-of select="implementation"/></td>
        <td><xsl:value-of select="test-suite"/></td>
        <td><xsl:value-of select="completion"/></td>
      </tr>
    </xsl:if>
  </xsl:for-each>
  
  </table>
  
</xsl:template>

</xsl:stylesheet>
