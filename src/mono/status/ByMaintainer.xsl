<?xml version="1.0"?>

<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>

<xsl:param name="email"/>
<xsl:param name="name"/>

<xsl:template match="/">
  <h2>Maintained by <xsl:value-of select="$name"/> (<xsl:value-of select="$email"/>)</h2>
  <table border="1">
    <tr>
      <td>Class Name</td>
      <td>Last Activity</td>
      <td>Implementation</td>
      <td>Test Suite</td>
      <td>Completion</td>
    </tr>
    <xsl:apply-templates/>
  </table>
</xsl:template>

<xsl:template match="class">
  <xsl:if test="contains(maintainers/*, $email)">
    <tr>
      <td><xsl:value-of select="@name"/></td>
      <td><xsl:value-of select="last-activity"/></td>
      <td><xsl:value-of select="implementation"/></td>
      <td><xsl:value-of select="test-suite"/></td>
      <td><xsl:value-of select="completion"/></td>
    </tr>
  </xsl:if>
</xsl:template>

</xsl:stylesheet>
