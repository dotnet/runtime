<?xml version="1.0" encoding="utf-8"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:l="http://www.schema.de/XSL/ST4DocuManagerlang" version="1.0">
	<xsl:template match="/">
		<xsl:for-each select="/root/bla">
			<xsl:comment>
				<xsl:value-of select="@test"/>
			</xsl:comment>
			<xsl:call-template name="duplicate">
				<xsl:with-param name="value" select="@test" />
			</xsl:call-template>
		</xsl:for-each>
	</xsl:template>

	<xsl:template name="duplicate">
		<xsl:param name="value" />
		<xsl:param name="result" />
		<xsl:choose>
			<xsl:when test="contains($value, ' ')">
				<xsl:call-template name="duplicate">
					<xsl:with-param name="value" select="substring-after($value, ' ')" />
					<xsl:with-param name="result" select="concat($result,' ', substring-before($value, ' ') * 2)" />
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise>
				<xsl:value-of select="concat($result,' ', $value * 2)" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

</xsl:stylesheet>