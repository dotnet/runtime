<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" xmlns:myObj="urn:my-object">
    <xsl:template match="/">
	<result>
		<A><xsl:value-of select="myObj:MyValue()"/></A>
		<B><xsl:value-of select="myObj:ReduceCount(4)"/></B>
		<C><xsl:value-of select="myObj:MyValue()"/></C>
		<D><xsl:value-of select="myObj:ReduceCount(0)"/></D>
		<E><xsl:value-of select="myObj:ReduceCount(-14)"/></E>
		<F><xsl:value-of select="myObj:AddToString('Hello ')"/></F>
		<G><xsl:value-of select="myObj:AddToString('World ')"/></G>
		<H><xsl:value-of select="myObj:ReduceCount(50.2)"/></H>
	</result>
	
    </xsl:template>
</xsl:stylesheet>