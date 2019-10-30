<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
xmlns:msxsl="urn:schemas-microsoft-com:xslt" exclude-result-prefixes="msxsl exobj" xmlns:exobj="urn-myobject">

<xsl:output method="xml" indent="yes"/>

<xsl:template match="/">
    <xsl:variable name="titles" select="exobj:Increment()"/>
    <out><xsl:value-of select="$titles"/></out>
</xsl:template>

</xsl:stylesheet>
