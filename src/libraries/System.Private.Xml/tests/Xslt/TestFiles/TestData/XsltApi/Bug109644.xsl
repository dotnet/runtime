<?xml version="1.0" encoding="UTF-8" ?>
<!DOCTYPE xsl:stylesheet 
					[<!ENTITY nbsp "<xsl:text disable-output-escaping='yes'>&amp;nbsp;</xsl:text>">]>

<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
								xmlns:msxsl="urn:schemas-microsoft-com:xslt"
								xmlns:hrpd="urn:hrpd-functions"
								version="1.0">

	<!-- This template illustrates a bug using for-each to iterate over
			 a node set.  The results are incorrect for all subsequent calls after
			 the first one.  Note, this failure also happens on nodes input through
			 the source XML document.
			 
			 I suspect this is a bug resetting a re-used node iterator object correctly.
	  -->

	<xsl:template match="/">
		<xsl:variable name="TestXml">
			<Nodes name="Nodes">
				<Node name="1"/>
				<Node name="2"/>
				<Node name="3"/>
			</Nodes>
		</xsl:variable>
		<!-- This shows the node count -->
		Node Count: {<xsl:value-of select="count(msxsl:node-set($TestXml)/Nodes/Node)"/>}

		<xsl:variable name="TestNodes" select="msxsl:node-set($TestXml)/Nodes/Node"/>

		<!-- First Iteration: correctly outputs (1)(2)(3) -->
		Correct Output: <xsl:for-each select="$TestNodes">(<xsl:value-of select="@name"/>)</xsl:for-each>

		<!-- Second Iteration: incorrectly outputs [3][2][3] -->
		Incorrect Output: <xsl:for-each select="$TestNodes">[<xsl:value-of select="@name"/>]</xsl:for-each>
	</xsl:template>
</xsl:stylesheet>
