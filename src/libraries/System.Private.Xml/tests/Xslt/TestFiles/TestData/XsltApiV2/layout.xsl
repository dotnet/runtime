<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">
	<xsl:output method="xml" omit-xml-declaration="yes" />

	<xsl:param name="langcode" select="'en'" /> 
	<xsl:param name="oddstype" select="2" />

	<xsl:template match="/">
		<html lang="{$langcode}">
			<head>
				<xsl:call-template name="metadata" />
				<xsl:call-template name="contenttype" />
				<xsl:call-template name="css" />
				<xsl:call-template name="extraheader" />
			</head>
			<body>
				<xsl:apply-templates select="/eventpage/sporttabs" mode="tab" />
				<xsl:apply-templates />
				
				<div class="temp">
					Current category type ID = <xsl:value-of select="/eventpage/selectedcategory/@typeID" />
				</div>
			</body>
		</html>
	</xsl:template>
	
	<xsl:template name="css">
		<link href="css/style.css" rel="stylesheet" type="text/css" />
	</xsl:template>
	
	<xsl:template name="extraheader">
	</xsl:template>
	
	<xsl:template name="metadata">
		<title>betinternet.com</title>
	</xsl:template>
	
	<xsl:template name="contenttype">
		<xsl:choose>
			<xsl:when test="$langcode='zh'">
				<meta http-equiv="Content-Type" content="text/html; charset=big5" /> 
			</xsl:when>
			<xsl:when test="$langcode='th'">
				<meta http-equiv="Content-Type" content="text/html; charset=tis-620" /> 
			</xsl:when>
			<xsl:when test="$langcode='ja'">
				<meta http-equiv="Content-Type" content="text/html; charset=utf-8" /> 
			</xsl:when>
			<xsl:otherwise>
				<meta http-equiv="Content-Type" content="text/html; charset=iso-8859-1" /> 
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

	
	
	<!-- ==================================================================
			                      ODDS FORMATTING
	=================================================================== -->
	<xsl:template match="*" mode="oddslink">
		<xsl:param name="num" select="number(@oddsnum)" />
		<xsl:param name="den" select="number(@oddsden)" />
		
		<a target="betslip" href="fr_slip.asp?action=add&amp;outcome={@ID}">
			<xsl:apply-templates select="." mode="odds">
				<xsl:with-param name="num" select="$num" />
				<xsl:with-param name="den" select="$den" />
			</xsl:apply-templates>
		</a>
	</xsl:template>

	<xsl:template match="*" mode="odds">
		<xsl:param name="num" select="number(@oddsnum)" />
		<xsl:param name="den" select="number(@oddsden)" />
		
		<xsl:choose>
		
			<!-- uk odds -->
			<xsl:when test="$oddstype=1">
				<xsl:value-of select="concat($num, '/', $den)" />
			</xsl:when>
			
			<!-- us odds -->
			<xsl:when test="$oddstype=3">
				<xsl:variable name="factor">
					<xsl:choose>
						<xsl:when test="$num &gt;= $den">
							<xsl:value-of select="$num div $den" />
						</xsl:when>
						<xsl:otherwise>
							<xsl:value-of select="(-1 * ($den div $num))" />
						</xsl:otherwise>
					</xsl:choose>
				</xsl:variable>
        		<xsl:value-of select="format-number(number($factor), '#,##0.000')" />
			</xsl:when>
			
			<!-- european odds -->
			<xsl:otherwise>
				<xsl:choose>
					<xsl:when test="$num = 0 or $den = 0">
						No Odds
					</xsl:when>
					<xsl:otherwise>
						<xsl:value-of select="format-number(($num div  $den) + 1, '#,##0.000')"/>
					</xsl:otherwise>
				</xsl:choose>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	<xsl:template name="cssrowclass">
		<xsl:choose>
			<xsl:when test="position() mod 2 = 0">
				<xsl:attribute name="class">one</xsl:attribute>
			</xsl:when>			
			<xsl:otherwise>
				<xsl:attribute name="class">two</xsl:attribute>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	
	<!-- ==================================================================
	                                EACH WAY
	=================================================================== -->	
	<xsl:template match="market" mode="eachway_format">
		<xsl:param name="caption" />
		
		<market ID="97933" typeID="106258" styleID="1" desc="Outright Betting" places="4" placingnum="1" placingden="6" />
		
		<xsl:value-of select="$caption" />
		<xsl:text> </xsl:text>
		<xsl:apply-templates select="." mode="odds">
			<xsl:with-param name="den" select="@placingden" />
			<xsl:with-param name="num" select="@placingnum" />
		</xsl:apply-templates>
		<xsl:text> </xsl:text>		
		<xsl:call-template name="eachwayplace">
			<xsl:with-param name="currentplace" select="1" />
			<xsl:with-param name="maxplaces" select="@places" />
		</xsl:call-template>
		
		<xsl:value-of select="eachway/eachwayplaces"/>
	</xsl:template>
	
	
	<xsl:template name="eachwayplace">
		<xsl:param name="currentplace" select="1" />
		<xsl:param name="maxplaces" select="1" />
		
		<xsl:value-of select="$currentplace" />
		
		<xsl:if test="$currentplace &lt; $maxplaces">
			<xsl:text>,</xsl:text>
			<xsl:call-template name="eachwayplace">
				<xsl:with-param name="currentplace" select="$currentplace + 1" />
				<xsl:with-param name="maxplaces" select="$maxplaces" />
			</xsl:call-template>
		</xsl:if>
	</xsl:template>


	<xsl:template match="sporttabs" mode="tab">
		<div class="sports">
			<xsl:apply-templates mode="tab" />
		</div>
	</xsl:template>
	
	<xsl:template match="sporttab" mode="tab">
		<a href="CategoryPage.aspx?tab={@ID}" target="main">
			<xsl:if test="@ID=parent::sporttabs/@selected">
				<xsl:attribute name="class">selectedtab</xsl:attribute>
			</xsl:if>
			<xsl:value-of select="@desc" />
		</a>
		<xsl:text> </xsl:text>
	</xsl:template>
	
	
	<!--
		odds type selector
		- pass in URL of current page including a ? or ampersand
	-->
	<xsl:template name="oddstypes">
		<xsl:param name="params" />
		<xsl:variable name="url">
			<xsl:text>CategoryPage.aspx?tab=</xsl:text>
			<xsl:value-of select="/eventpage/sporttabs/@selected" />
			<xsl:text>&amp;</xsl:text>
			<xsl:value-of select="$params" />
			<xsl:text>&amp;oddstypeid=</xsl:text>
		</xsl:variable>
		
		<div class="oddtype">
			<xsl:choose>
				<xsl:when test="$oddstype = 1">
					<span>Fractional</span>
				</xsl:when>
				<xsl:otherwise>
					<a href="{$url}1">Fractional</a>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:text> | </xsl:text>
			<xsl:choose>
				<xsl:when test="$oddstype = 2">
					<span>Decimal</span>
				</xsl:when>
				<xsl:otherwise>
					<a href="{$url}2">Decimal</a>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:text> | </xsl:text>
			<xsl:choose>
				<xsl:when test="$oddstype = 3">
					<span>U.S.</span>
				</xsl:when>
				<xsl:otherwise>
					<a href="{$url}3">U.S.</a>
				</xsl:otherwise>
			</xsl:choose>
		</div>
	</xsl:template>


</xsl:stylesheet>
