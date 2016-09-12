<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:user="user">

<foo/>

<xsl:template match="/">
    <xsl:value-of select="user:my_code()"/>
</xsl:template>

</xsl:stylesheet>
