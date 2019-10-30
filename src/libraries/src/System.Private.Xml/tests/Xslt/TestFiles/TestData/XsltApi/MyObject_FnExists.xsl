<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">


    <xsl:template match="/">

	<result>
		DoNothing Function<xsl:if test="function-available('myObj:DoNothing')"> Test Pass</xsl:if>
		Construtor Function<xsl:if test="function-available('myObj:MyObject')"> Test Pass</xsl:if>
		Return Int  Function<xsl:if test="function-available('myObj:MyValue')"> Test Pass</xsl:if>
		Return String Function<xsl:if test="function-available('myObj:Fn1')"> Test Pass</xsl:if>
		ReturnInt  Function<xsl:if test="function-available('myObj:MyValue')"> Test Pass</xsl:if>
		Taking in args <xsl:if test="function-available('myObj:AddToString')"> Test Pass</xsl:if>
		Public Function<xsl:if test="function-available('myObj:PublicFunction')"> Test Pass</xsl:if>
		Protected Function<xsl:if test="function-available('myObj:ProtectedFunction')"> Test Pass</xsl:if>
		Private Function<xsl:if test="function-available('myObj:PrivateFunction')"> Test Pass</xsl:if>
		Default Function<xsl:if test="function-available('myObj:DefaultFunction')"> Test Pass</xsl:if>

		</result>
	
    </xsl:template>
</xsl:stylesheet>