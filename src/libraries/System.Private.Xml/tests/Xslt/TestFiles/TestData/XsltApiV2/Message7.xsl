<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">
<xsl:output method="xml" omit-xml-declaration="yes"/>

<xsl:template match="message">
<xsl:message terminate="yes"><Message>Message #7</Message></xsl:message>
</xsl:template>
</xsl:stylesheet>
