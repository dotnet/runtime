<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">
    <xsl:template match="/">

	<result>
		<DoNothing><xsl:if test="function-available('myObj:DoNothing')"> Test Pass</xsl:if></DoNothing>
		<Constructor><xsl:if test="function-available('myObj:MyObject')"> Test Pass</xsl:if></Constructor>
		<ReturnInt><xsl:if test="function-available('myObj:MyValue')"> Test Pass</xsl:if></ReturnInt>
		<ReturnString><xsl:if test="function-available('myObj:Fn1')"> Test Pass</xsl:if></ReturnString>
		<ReturnInt><xsl:if test="function-available('myObj:MyValue')"> Test Pass</xsl:if></ReturnInt>
		<Arguments><xsl:if test="function-available('myObj:AddToString')"> Test Pass</xsl:if></Arguments>
		<Public><xsl:if test="function-available('myObj:PublicFunction')"> Test Pass</xsl:if></Public>
		<Protected><xsl:if test="function-available('myObj:ProtectedFunction')"> Test Pass</xsl:if></Protected>
		<Private><xsl:if test="function-available('myObj:PrivateFunction')"> Test Pass</xsl:if></Private>
		<Default><xsl:if test="function-available('myObj:DefaultFunction')"> Test Pass</xsl:if></Default>
	</result>
	
    </xsl:template>
</xsl:stylesheet>