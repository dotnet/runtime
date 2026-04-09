<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

	<xsl:import href="layout.xsl" />
	<xsl:import href="coupons.xsl" />
	
	<xsl:template name="metadata">
		<title>Categories</title>
	</xsl:template>
	
	<xsl:template name="extraheader">
		<script type="text/javascript" language="javascript" src="../jscript/dates.js">
		</script>
	</xsl:template>
	
		
	<xsl:template match="eventpage">
		<div class="temp">
			category.xsl
		</div>
		<xsl:apply-templates select="selectedcategory" />		
	</xsl:template>
	
	<xsl:template match="selectedcategory">
		<h1><xsl:value-of select="@desc" /></h1>
		<xsl:apply-templates select="categorypath" />
		<xsl:apply-templates select="categorygroupmarkets" />
		
		<xsl:if test="/eventpage/selectedcategory[@typeID !=1 and @typeID !=2]">
			<xsl:call-template name="timezonemessage" />
		</xsl:if>
		
		<xsl:apply-templates select="categories" />
		<xsl:apply-templates select="events" />
		
		<xsl:if test="@typeID &gt; 2 and @typeID &lt; 6">
			<xsl:call-template name="oddstypes">
				<xsl:with-param name="params" select="concat('catid=',@ID)" />
			</xsl:call-template>
		</xsl:if>
	</xsl:template>
	
	<xsl:template name="timezonemessage">
		<div class="timezone">
			GMT
			(
			<script language="javascript" type="text/javascript">displaytimezone()</script>
			)
		</div>
	</xsl:template>


	<xsl:template match="market" mode="eachway">
		<xsl:text> </xsl:text>
		<xsl:apply-templates select="." mode="eachway_format">
			<xsl:with-param name="caption">Each way</xsl:with-param>
		</xsl:apply-templates>
	</xsl:template>
	
	
	<!-- ==================================================================
			                       BREADCRUMB TRAIL
	=================================================================== -->
	<xsl:template match="categorypath">
		<div class="breadcrumb">
			<xsl:apply-templates select="/eventpage/sporttabs/sporttab[@ID=parent::sporttabs/@selected]" />
			<xsl:apply-templates select="category" />
		</div>
	</xsl:template>
	
	
	<xsl:template match="categorypath/category">
		<a href="CategoryPage.aspx?tab={/eventpage/sporttabs/@selected}&amp;catid={@ID}">
			<xsl:value-of select="@desc" /><xsl:text />
		</a>
		<xsl:text> / </xsl:text>
	</xsl:template>
	
	
	<xsl:template match="categorypath/category[@ID=/eventpage/selectedcategory/@ID]">
		<xsl:value-of select="@desc" />
	</xsl:template>
	
	
	<xsl:template match="sporttab">
		<a href="CategoryPage.aspx?tab={@ID}">
			<xsl:value-of select="@desc" /><xsl:text />
		</a>
		<xsl:text> / </xsl:text>
	</xsl:template>
	
	
	
	<!-- ==================================================================
                       MARKET SELECTOR AT TOP OF PAGE
	                            USED BY CATEGORY 3
	=================================================================== -->	
	<xsl:template match="categorygroupmarkets">
		<xsl:if test="parent::selectedcategory/@typeID=3 and markettype[position()=2]">
			<div class="markettypes">
				<xsl:apply-templates select="markettype" />
			</div>
		</xsl:if>
	</xsl:template>
	
	<xsl:template match="markettype">
		<span>
			<a href="CategoryPage.aspx?tab={/eventpage/sporttabs/@selected}&amp;catid={/eventpage/selectedcategory/@ID}&amp;mkttypeid={@ID}">
				<xsl:value-of select="@desc" />
			</a>
		</span>
	</xsl:template>

	<xsl:template match="markettype[@ID=parent::categorygroupmarkets/@selected]">
		<span>
			<xsl:value-of select="@desc" />
		</span>
	</xsl:template>
	
	
	
	<!-- ==================================================================
	                                 CATEGORY 1
			                       SUB-CATEGORIES LIST
	=================================================================== -->	
	<xsl:template match="categories">
		<xsl:apply-templates />
	</xsl:template>
	
	<xsl:template match="categories/category">
		<h2>
			<a href="CategoryPage.aspx?tab={/eventpage/sporttabs/@selected}&amp;catid={@ID}">
				<xsl:value-of select="@desc" />
			</a>
		</h2>
		<xsl:apply-templates select="@narrative[.!='']" />
	</xsl:template>
	
	<xsl:template match="@narrative">
		<div>
			<xsl:value-of select="." />
		</div>
	</xsl:template>
	

	
	
	<!-- ==================================================================
	                                CATEGORY 2
                            LIST OF EVENTS IN CATEGORY
	=================================================================== -->	
	<xsl:template match="selectedcategory[@typeID=2]/events" priority="2">
		<ul>
			<xsl:apply-templates mode="category2" />
		</ul>
	</xsl:template>
	
	<xsl:template match="event" mode="category2">
		<li>
			<a href="CategoryPage.aspx?tab={/eventpage/sporttabs/@selected}&amp;evntid={@ID}">
				<xsl:value-of select="@desc" />
			</a>
		</li>
	</xsl:template>
	
	
	<!-- ==================================================================
	                  MAIN CATEGORY TEMPLATE REDIRECTION
	=================================================================== -->	

	<!-- determine whether to branch off to list by markets or list by events -->
	<xsl:template match="selectedcategory/events">
		<xsl:variable name="typeID" select="parent::selectedcategory/@typeID" />

		<xsl:choose>
			<xsl:when test="$typeID = 3 or $typeID = 5">
				<xsl:apply-templates select="parent::selectedcategory/categorygroupmarkets/markettype" mode="direct" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates select="event" mode="direct" />
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	

	<!-- list markets grouped by event - determine which style to use -->
	<xsl:template match="event" mode="direct">
		<xsl:variable name="styleid" select="market[position()=1]/@styleID" />
		<xsl:choose>
			<xsl:when test="$styleid = 1">
				<xsl:apply-templates select="." mode="event1" />
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates select="." mode="eventdefault">
					<xsl:with-param name="styleID" select="$styleid" />
				</xsl:apply-templates>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	
	<!-- list events grouped by market - determine which style to use -->
	<xsl:template match="markettype" mode="direct">
		<xsl:variable name="morebets">
			<xsl:if test="@ID = parent::categorygroupmarkets/@selected and /eventpage/selectedcategory/@typeID = 3">
				<xsl:text>1</xsl:text>
			</xsl:if>
		</xsl:variable>
		
		<xsl:variable name="styleid" select="parent::categorygroupmarkets/parent::selectedcategory/events/event/market[@typeID=current()/@ID]/@styleID" />
						
		<xsl:choose>
			<xsl:when test="$styleid = 2 or $styleid = 3">
				<xsl:apply-templates select="." mode="market2and3">
					<xsl:with-param name="morebets" select="$morebets" />
				</xsl:apply-templates>			
			</xsl:when>
			<xsl:when test="$styleid = 4">
				<xsl:apply-templates select="." mode="market4">
					<xsl:with-param name="morebets" select="$morebets" />
				</xsl:apply-templates>			
			</xsl:when>
			<xsl:when test="$styleid = 5">
				<xsl:apply-templates select="." mode="market5">
					<xsl:with-param name="morebets" select="$morebets" />
				</xsl:apply-templates>
			</xsl:when>
			<xsl:when test="$styleid = 6 or $styleid = 7">
				<xsl:apply-templates select="." mode="market6and7">
					<xsl:with-param name="morebets" select="$morebets" />
				</xsl:apply-templates>			
			</xsl:when>
			<xsl:otherwise>
				<xsl:apply-templates select="." mode="market1">
					<xsl:with-param name="morebets" select="$morebets" />
				</xsl:apply-templates>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>
	
	
	<!-- ==================================================================
	                           DUMP - CATEGORY 7
	=================================================================== -->	
	<xsl:template match="selectedcategory[@typeID=7]/events">
		<p>
			Category is no longer available.
		</p>
	</xsl:template>


	<!-- ==================================================================
	                           ASIAN HANDICAPS
	=================================================================== -->	
	<xsl:template match="selectedcategory[@typeID=6]/events">
		<p>
			Asian Note 1
		</p>
		<p>
			Asian Note 2
		</p>
		
		<xsl:apply-templates select="event" mode="asianhandicap" />
	</xsl:template>
	
	<xsl:template match="event" mode="asianhandicap">
		<table class="coupon">
			<tr class="title">
				<th colspan="3">
					<xsl:value-of select="@desc" />
				</th>
			</tr>
			<tr class="col">
				<th>
					<xsl:apply-templates select="@date" mode="date" />
				</th>
				<th>
					Asian Handicaps 
				</th>
				<th>
					Asian Odds
				</th>
			</tr>
			<xsl:apply-templates select="market" mode="asianhandicap" />
		</table>
	</xsl:template>
	
		
	<xsl:template match="market" mode="asianhandicap">
		<xsl:apply-templates select="outcome" mode="asianhandicap" />

		<xsl:apply-templates select="." mode="asiannarrative">
			<xsl:with-param name="firstteam" select="outcome[@asianhandicapfactor='-1' or (@asianhandicapfactor='0' and position()=1)]" />
			<xsl:with-param name="secondteam" select="outcome[@asianhandicapfactor='1' or (@asianhandicapfactor='0' and position()=2)]" />
			<xsl:with-param name="swap"><xsl:if test="outcome[position()=1]/@asianhandicapfactor='1'">1</xsl:if></xsl:with-param>
		</xsl:apply-templates>
	</xsl:template>
	
	
	<xsl:template match="outcome" mode="asianhandicap">
		<tr>
			<xsl:call-template name="cssrowclass" />
			<td>
				<xsl:value-of select="@desc" />
			</td>
			<td>
				<xsl:if test="@asianhandicapfactor='-1'">
					<xsl:text>-</xsl:text>
				</xsl:if>
				<xsl:apply-templates select="parent::market/@asiangoal1" /><xsl:text />
				<xsl:apply-templates select="parent::market/@asiangoal2" /><xsl:text />
			</td>
			<td>
				<xsl:apply-templates select="." mode="odds" />
			</td>
		</tr>
	</xsl:template>
	
	<xsl:template match="@asiangoal2">
		<xsl:text>/</xsl:text><xsl:value-of select="." /><xsl:text />
	</xsl:template>

	<xsl:template match="@asiandeductpercent">
		<xsl:text>(</xsl:text><xsl:value-of select="." /><xsl:text>%)</xsl:text>
	</xsl:template>
	
	
	<!-- ==================================================================
	                        ASIAN HANDICAP NARRATIVES
	=================================================================== -->	
	<xsl:template match="market[@asiangoal1='2.5' and not(@asiangoal2)]" mode="asiannarrative">
		<xsl:param name="firstteam" />
		<xsl:param name="secondteam" />
		Multi-lingual
	</xsl:template>

	<xsl:template match="market[@asiangoal1='2' and @asiangoal2='2.5']" mode="asiannarrative">
		<xsl:param name="firstteam" />
		<xsl:param name="secondteam" />	
		<xsl:param name="swap" />
		<xsl:call-template name="displayasiandesc">
			<xsl:with-param name="firstdesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="seconddesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="swap" select="$swap" />
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="market[@asiangoal1='2' and not(@asiangoal2)]" mode="asiannarrative">
		<xsl:param name="firstteam" />
		<xsl:param name="secondteam" />	
		<xsl:param name="swap" />
		<xsl:call-template name="displayasiandesc">
			<xsl:with-param name="firstdesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="seconddesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="swap" select="$swap" />
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="market[@asiangoal1='1.5' and @asiangoal2='2']" mode="asiannarrative">
		<xsl:param name="firstteam" />
		<xsl:param name="secondteam" />	
		<xsl:param name="swap" />
		<xsl:call-template name="displayasiandesc">
			<xsl:with-param name="firstdesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="seconddesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="swap" select="$swap" />
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="market[@asiangoal1='1.5' and not(@asiangoal2)]" mode="asiannarrative">
		<xsl:param name="firstteam" />
		<xsl:param name="secondteam" />	
		<xsl:param name="swap" />
		<xsl:call-template name="displayasiandesc">
			<xsl:with-param name="firstdesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="seconddesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="swap" select="$swap" />
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="market[@asiangoal1='1' and @asiangoal2='1.5']" mode="asiannarrative">
		<xsl:param name="firstteam" />
		<xsl:param name="secondteam" />	
		<xsl:param name="swap" />
	
		<xsl:call-template name="displayasiandesc">
			<xsl:with-param name="firstdesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="seconddesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="swap" select="$swap" />
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="market[@asiangoal1='1' and not(@asiangoal2)]" mode="asiannarrative">
		<xsl:param name="firstteam" />
		<xsl:param name="secondteam" />	
		<xsl:param name="swap" />
		<xsl:call-template name="displayasiandesc">
			<xsl:with-param name="firstdesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="seconddesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="swap" select="$swap" />
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="market[@asiangoal1='0.5' and @asiangoal2='1']" mode="asiannarrative">
		<xsl:param name="firstteam" />
		<xsl:param name="secondteam" />	
		<xsl:param name="swap" />
		<xsl:call-template name="displayasiandesc">
			<xsl:with-param name="firstdesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="seconddesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="swap" select="$swap" />
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="market[@asiangoal1='0.5' and not(@asiangoal2)]" mode="asiannarrative">
		<xsl:param name="firstteam" />
		<xsl:param name="secondteam" />	
		<xsl:param name="swap" />
		<xsl:call-template name="displayasiandesc">
			<xsl:with-param name="firstdesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="seconddesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="swap" select="$swap" />
		</xsl:call-template>
	</xsl:template>

	<xsl:template match="market[@asiangoal1='0' and @asiangoal2='0.5']" mode="asiannarrative">
		<xsl:param name="firstteam" />
		<xsl:param name="secondteam" />	
		<xsl:param name="swap" />
		<xsl:call-template name="displayasiandesc">
			<xsl:with-param name="firstdesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="seconddesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="swap" select="$swap" />
		</xsl:call-template>
	</xsl:template>


	<xsl:template match="market[@asiangoal1='0' and not(@asiangoal2)]" mode="asiannarrative">
		<xsl:param name="firstteam" />
		<xsl:param name="secondteam" />	
		<xsl:param name="swap" />

		<xsl:call-template name="displayasiandesc">
			<xsl:with-param name="firstdesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="seconddesc">
				Multi-lingual
			</xsl:with-param>
			<xsl:with-param name="swap" select="$swap" />
		</xsl:call-template>
	</xsl:template>

	
	<!-- display an asian handicap description -->
	<xsl:template name="displayasiandesc">
		<xsl:param name="firstdesc" />
		<xsl:param name="seconddesc" />
		<xsl:param name="swap" />	
		
		<tr>
			<td colspan="3">
				<xsl:choose>
					<xsl:when test="$swap='1'">
						<xsl:copy-of select="$seconddesc" />
						<p>
							<xsl:copy-of select="$firstdesc" />
						</p>
					</xsl:when>
					<xsl:otherwise>
						<xsl:copy-of select="$firstdesc" />
						<p>
							<xsl:copy-of select="$seconddesc" />
						</p>
					</xsl:otherwise>
				</xsl:choose>
			</td>
		</tr>
	</xsl:template>
</xsl:stylesheet>
