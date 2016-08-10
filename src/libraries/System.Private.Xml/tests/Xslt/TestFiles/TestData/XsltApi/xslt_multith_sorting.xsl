<?xml version="1.0"?>

<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

	<xsl:output omit-xml-declaration="yes" indent="no" encoding='utf-8' />

	<xsl:template match='numbers'>
		<root>
		<xsl:for-each select='number'>
			<xsl:sort order='ascending' data-type='number' select='.' case-order='lower-first' />
			<number>
			<xsl:value-of select='.' />
			</number>
		</xsl:for-each>
		<xsl:for-each select='number'>
			<xsl:sort order='descending' data-type='number' select='.' case-order='lower-first' />
			<number>
			<xsl:value-of select='.' />
			</number>
		</xsl:for-each>
		<xsl:for-each select='number'>
			<xsl:sort order='ascending' data-type='number' select='.' case-order='upper-first' />
			<number>
			<xsl:value-of select='.' />
			</number>
		</xsl:for-each>
		<xsl:for-each select='number'>
			<xsl:sort order='descending' data-type='number' select='.' case-order='upper-first' />
			<number>
			<xsl:value-of select='.' />
			</number>
		</xsl:for-each>
		</root>
	</xsl:template>	

</xsl:stylesheet>