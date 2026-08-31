<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform">

<xsl:import href="XmlResolver_Import.xsl"/>

<xsl:include href="XmlResolver_Include.xsl"/>

<xsl:template match="FruitRack">
	<result><xsl:apply-templates/></result>
</xsl:template>
    
</xsl:stylesheet>