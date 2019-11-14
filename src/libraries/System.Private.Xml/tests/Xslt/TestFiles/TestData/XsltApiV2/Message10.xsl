<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" omit-xml-declaration="yes"/>

<xsl:template match="message">
<xsl:message terminate="yes">
<xsl:processing-instruction name="msg">piMessage</xsl:processing-instruction>
<xsl:comment>cmtMessage</xsl:comment>
<xsl:variable name="msg"><varMessage/></xsl:variable>
<xsl:element name="Message">
<xsl:attribute name="id">10</xsl:attribute>
<xsl:copy>
<xsl:copy-of select="$msg"/>
<xsl:text>Message : </xsl:text>
<xsl:choose>
<xsl:when test="1=1">
<xsl:call-template name="temp" />
</xsl:when>
<xsl:otherwise><xsl:apply-templates/></xsl:otherwise>
</xsl:choose>
<xsl:if test="1=1">
<xsl:value-of select="."/>
</xsl:if>
</xsl:copy>
</xsl:element>
</xsl:message>
</xsl:template>

<xsl:template name="temp">
	<xsl:value-of select="'in temp. '"/>
</xsl:template>
</xsl:stylesheet>