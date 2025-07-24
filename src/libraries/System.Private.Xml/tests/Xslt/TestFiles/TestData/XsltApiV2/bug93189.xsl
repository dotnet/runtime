<?xml version="1.0" encoding="UTF-8"?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:date="http://exslt.org/dates-and-times" xmlns:ms="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="date">
	<xsl:output indent="yes" omit-xml-declaration="yes" />
	<xsl:template match="data">
		<out>
			<dates>
				<test1>
					<xsl:value-of select="ms:format-date(date, 'D')" />
				</test1>
				<test2>
					<xsl:value-of select="ms:format-date(date, 'd')" />
				</test2>
				<test3>
					<xsl:value-of select="ms:format-date(date, '')" />
				</test3>
				<test4>
					<xsl:value-of select="ms:format-date(date, 'd', 'en-US')" />
				</test4>
				<test5>
					<xsl:value-of select="ms:format-date(date, 'D', 'fr-FR')" />
				</test5>
				<test6>
					<xsl:value-of select="ms:format-date(date, 'd', 'fr-FR')" />
				</test6>
			</dates>
			<times>
				<test1>
					<xsl:value-of select="ms:format-time(date, 'T')" />
				</test1>
				<test2>
					<xsl:value-of select="ms:format-time(date, 't')" />
				</test2>
				<test3>
					<xsl:value-of select="ms:format-time(date, '')" />
				</test3>
				<test4>
					<xsl:value-of select="ms:format-date(date, 't', 'en-US')" />
				</test4>
				<test5>
					<xsl:value-of select="ms:format-date(date, 'T', 'fr-FR')" />
				</test5>
				<test6>
					<xsl:value-of select="ms:format-date(date, 't', 'fr-FR')" />
				</test6>
			</times>
		</out>
	</xsl:template>
</xsl:stylesheet>
