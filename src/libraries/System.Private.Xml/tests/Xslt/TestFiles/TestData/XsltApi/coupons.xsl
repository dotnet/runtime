<?xml version="1.0" encoding="ISO-8859-1"?>
<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform" version="1.0">

	<xsl:template match="outcome" mode="oddsTD">
		<td class="odd">
			<xsl:apply-templates select="." mode="oddslink" />
		</td>
	</xsl:template>
	
	<xsl:template match="outcome" mode="td">
		<td>
			<xsl:value-of select="@desc" />
		</td>
	</xsl:template>
	
	<xsl:template match="outcome" mode="outhead">
		<td class="outhead">
			<xsl:value-of select="@desc" />
		</td>
	</xsl:template>
	
	<xsl:template match="*|@*" mode="date">
		<script language="javascript" type="text/javascript">
			<![CDATA[displaydate(']]><xsl:value-of select="."/><![CDATA[');]]>
		</script>
	</xsl:template>
	


	<!-- ==================================================================
	                         COUPON - GROUP BY EVENTS
                                   STYLE 1
	=================================================================== -->	
	
	<xsl:template match="event" mode="event1">
		<xsl:param name="markets" select="market" />
		
		<div class="temp">
			group by event style 1
		</div>
		
		<table class="coupon">
			<tr class="title">
				<th colspan="4">
					<xsl:value-of select="@desc" /> (event desc - probably not needed)
				</th>
			</tr>
			<xsl:apply-templates select="$markets" mode="event1" />
		</table>
	</xsl:template>
	
	
	<xsl:template match="market" mode="event1">
		<tr class="subtitle">
			<th colspan="4">
				<xsl:value-of select="@desc" />
				<xsl:apply-templates select="." mode="eachway" />
			</th>
		</tr>
		<xsl:apply-templates select="outcome" mode="event1" />
	</xsl:template>
	
	
	<xsl:template match="outcome" mode="event1">
		<tr>
			<xsl:choose>
				<xsl:when test="position() mod 4 = 1">
					<xsl:attribute name="class">one</xsl:attribute>
				</xsl:when>
				<xsl:otherwise>
					<xsl:attribute name="class">two</xsl:attribute>
				</xsl:otherwise>
			</xsl:choose>
			<xsl:apply-templates select="." mode="twocells" />
			<xsl:apply-templates select="following-sibling::outcome[position()=1]" mode="twocells" />
		</tr>
	</xsl:template>
	
	<xsl:template match="outcome[position() mod 2 = 0]" mode="event1">
	</xsl:template>
	
	<xsl:template match="outcome" mode="twocells">
		<xsl:apply-templates select="." mode="td" />
		<xsl:apply-templates select="." mode="oddsTD" />
	</xsl:template>
	
	
	<!-- ==================================================================
	                       COUPON - GROUP BY EVENTS
                       DEFAULT STYLE - USED BY 2, 3, 4, 7
	=================================================================== -->	
	
	<xsl:template match="event" mode="eventdefault">
		<xsl:param name="markets" select="market" />
		<xsl:param name="styleID" select="1" />

		<div class="temp">
			group by event default style (id=<xsl:value-of select="$styleID" />)
		</div>
		
		<table class="coupon">
			<tr class="title">
				<th>
					<xsl:attribute name="colspan">
						<xsl:choose>
							<xsl:when test="$styleID=3 or $styleID=4">
								<xsl:text>4</xsl:text>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text>3</xsl:text>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:attribute>
					
					<xsl:value-of select="@compname" />
					<xsl:text> - </xsl:text>
					<xsl:value-of select="@desc" />
					<xsl:text> - </xsl:text>
					<xsl:apply-templates select="@eventdate" mode="date" />
				</th>
			</tr>
			<xsl:apply-templates select="$markets" mode="eventdefault" />
		</table>
	</xsl:template>
	
	
	<xsl:template match="market" mode="eventdefault">
		<tr>
			<xsl:call-template name="cssrowclass" />
			<td rowspan="2">
				<xsl:value-of select="@desc" />
			</td>
			<xsl:apply-templates select="outcome" mode="outhead" />
		</tr>
		<tr>
			<xsl:call-template name="cssrowclass" />
			<xsl:apply-templates select="outcome" mode="oddsTD" />
		</tr>
	</xsl:template>
	
	
	
	
	
	<!-- ==================================================================
                             COUPON - GROUP BY MARKETS
                                      STYLE 1
	=================================================================== -->	
	
	<xsl:template match="markettype" mode="market1">
		<div class="temp">
			market style 1
		</div>

		<xsl:if test="@ID = parent::categorygroupmarkets/@selected or parent::categorygroupmarkets/parent::selectedcategory/@typeID != 3">
			<table class="coupon">
				<xsl:apply-templates select="parent::categorygroupmarkets /parent::selectedcategory/events/event/market[@typeID=current()/@ID]" mode="market1" />
			</table>
		</xsl:if>
	</xsl:template>
	
	
	<xsl:template match="market" mode="market1">
		<tr class="title">
			<th colspan="4">
				<xsl:value-of select="parent::event/@desc" />
			</th>
		</tr>
		<!-- redirect to the event1 mode -->
		<xsl:apply-templates select="." mode="event1" />
	</xsl:template>
	
	
	<!-- ==================================================================
                             COUPON - GROUP BY MARKETS
                                STYLES 2 AND 3
	=================================================================== -->	
	
	<xsl:template match="markettype" mode="market2and3">
		<xsl:param name="morebets" />
		
		<div class="temp">
			group by market - style 2 and 3
		</div>
		
		<xsl:if test="@ID = parent::categorygroupmarkets/@selected or parent::categorygroupmarkets/parent::selectedcategory/@typeID != 3">
			<table class="coupon">
				<xsl:apply-templates select="parent::categorygroupmarkets/parent::selectedcategory/events/event/market[@typeID=current()/@ID]" mode="market2and3">
					<xsl:with-param name="morebets" select="$morebets" />
				</xsl:apply-templates>
			</table>
		</xsl:if>
	</xsl:template>
	

	<xsl:template match="market" mode="market2and3">
		<xsl:param name="morebets" />
		
		<!-- title if needed -->
		<xsl:if test="position() = 1 or parent::event/@compID != current()/preceding-sibling::market[position()=1]/parent::event/@compID">
			<tr class="title">
				<th>
					<xsl:attribute name="colspan">
						<xsl:choose>
							<xsl:when test="@styleID=2">
								<xsl:text>3</xsl:text>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text>4</xsl:text>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:attribute>
					<xsl:value-of select="parent::event/@compname" />
					<xsl:text> - </xsl:text>
					<xsl:value-of select="@desc" />
				</th>
			</tr>
		</xsl:if>

		<!-- events -->
		<tr>
			<xsl:call-template name="cssrowclass" />
			<td rowspan="2">			
				<div class="evname">
					<xsl:value-of select="parent::event/@desc" />
				</div>
				<div class="evdate">
					<xsl:apply-templates select="parent::event/@date" mode="date" />
				</div>
			</td>
			<xsl:apply-templates select="outcome" mode="outhead" />

			<xsl:if test="$morebets = 1 and parent::event/market[position()=2]">
				<td rowspan="2" class="morebets">
					<a href="CategoryPage.aspx?tab={/eventpage/sporttabs/@selected}&amp;evntid={parent::event/@ID}">
						more bets
					</a>
				</td>
			</xsl:if>
		</tr>
		<tr>
			<xsl:call-template name="cssrowclass" />
			<xsl:apply-templates select="outcome" mode="oddsTD" />
		</tr>
	</xsl:template>
	
	
	
	<!-- ==================================================================
                           COUPON - GROUP BY MARKETS
                                  1x2 : STYLE 4
	=================================================================== -->	
	
	<xsl:template match="markettype" mode="market4">
		<xsl:param name="morebets" />
		
		<div class="temp">
			group by market - style 4 (1x2)
		</div>
		
		<xsl:if test="@ID = parent::categorygroupmarkets/@selected or parent::categorygroupmarkets/parent::selectedcategory/@typeID != 3">

			<table class="coupon">
				<xsl:apply-templates select="parent::categorygroupmarkets /parent::selectedcategory/events/event[market/@typeID=current()/@ID]" mode="market4">
					<xsl:with-param name="morebets" select="$morebets" />
					<xsl:with-param name="typeID" select="@ID" />
				</xsl:apply-templates>
			</table>
		</xsl:if>
	</xsl:template>
	
	
	
	<xsl:template match="event" mode="market4">
		<xsl:param name="morebets" />
		<xsl:param name="typeID" />
		
		<!-- title if needed -->
		<xsl:if test="position() = 1 or @compID != preceding-sibling::event[position()=1]/@compID">
			<tr class="title">
				<th colspan="4">
					<xsl:value-of select="@compname" />
					<xsl:text> - </xsl:text>
					<xsl:value-of select="market[@typeID = $typeID and position() = 1]/@desc" />
				</th>
			</tr>
			<tr class="col">
				<th>
					&#160;
				</th>
				<th>
					1
				</th>
				<th>
					x
				</th>
				<th>
					2
				</th>
			</tr>
		</xsl:if>
		<xsl:apply-templates select="market[@typeID = $typeID]" mode="market4">
			<xsl:with-param name="morebets" select="$morebets" />
			<xsl:with-param name="pos" select="position()" />
		</xsl:apply-templates>

	</xsl:template>
	

	<xsl:template match="market" mode="market4">
		<xsl:param name="morebets" />
		<xsl:param name="pos" />
		
		<xsl:variable name="class">
			<xsl:choose>
				<xsl:when test="$pos mod 2 = 1">
					<xsl:text>one</xsl:text>
				</xsl:when>
				<xsl:otherwise>
					<xsl:text>two</xsl:text>
				</xsl:otherwise>
			</xsl:choose>
		</xsl:variable>

		<!-- events -->
		<tr class="{$class}">
		
			<td rowspan="2">			
				<div class="evname">
					<xsl:value-of select="parent::event/@desc" />
				</div>
				<div class="evdate">
					<xsl:apply-templates select="parent::event/@date" mode="date" />
				</div>
			</td>
			<xsl:apply-templates select="outcome" mode="outhead" />

			<xsl:if test="$morebets = 1 and parent::event/market[position()=2]">
				<td rowspan="2" class="morebets">
					<a href="CategoryPage.aspx?tab={/eventpage/sporttabs/@selected}&amp;evntid={parent::event/@ID}">
						more bets
					</a>
				</td>
			</xsl:if>
		</tr>
		<tr class="{$class}">
			<xsl:apply-templates select="outcome" mode="oddsTD" />
		</tr>
	</xsl:template>
	
	
	<!-- ==================================================================
                           COUPON - GROUP BY MARKETS
                           US SPORTS TOTALS : STYLE 5
	=================================================================== -->	
	
	<xsl:template match="markettype" mode="market5">
		<xsl:param name="morebets" />
		
		<div class="temp">
			group by market - style 5 (US Sports totals)
		</div>
		
		<xsl:if test="@ID = parent::categorygroupmarkets/@selected or parent::categorygroupmarkets/parent::selectedcategory/@typeID != 3">
			<table class="coupon">
				<xsl:apply-templates select="parent::categorygroupmarkets/parent::selectedcategory/events/event/market[@typeID=current()/@ID]" mode="market5">
					<xsl:with-param name="morebets" select="$morebets" />
				</xsl:apply-templates>
			</table>
		</xsl:if>
	</xsl:template>
	

	<xsl:template match="market" mode="market5">
		<xsl:param name="morebets" />
		
		<!-- title if needed -->
		<xsl:if test="position() = 1 or parent::event/@compID != current()/preceding-sibling::market[position()=1]/parent::event/@compID">
			<tr class="title">
				<th colspan="5">
					<xsl:value-of select="parent::event/@compname" />
					<xsl:text> - </xsl:text>
					<xsl:value-of select="@desc" />
				</th>
			</tr>
			<tr class="col">
				<th>
					Odds
				</th>
				<th colspan="3">
					&#160;
				</th>
				<th>
					Odds
				</th>
			</tr>
		</xsl:if>

		<!-- events -->
		<tr>
			<xsl:call-template name="cssrowclass" />
			
			<xsl:apply-templates select="outcome[position()=1]" mode="oddsTD" />
			<xsl:apply-templates select="outcome[position()=1]" mode="td" />
			
			<td>
				<div class="evname">
					<xsl:value-of select="parent::event/@desc" />
				</div>
				<div class="evdate">
					<xsl:apply-templates select="parent::event/@date" mode="date" />
				</div>
			</td>
			
			<xsl:apply-templates select="outcome[position()=2]" mode="td" />
			<xsl:apply-templates select="outcome[position()=2]" mode="oddsTD" />

			<xsl:if test="$morebets = 1 and parent::event/market[position()=2]">
				<td class="morebets">
					<a href="CategoryPage.aspx?tab={/eventpage/sporttabs/@selected}&amp;evntid={parent::event/@ID}">
						more bets
					</a>
				</td>
			</xsl:if>
		</tr>
	</xsl:template>
	
	
	<!-- ==================================================================
                           COUPON - GROUP BY MARKETS
                           US SPORTS HANDICAP - STYLE 6
                           WIN 2 WAY - STYLE 7
	=================================================================== -->	
	
	<xsl:template match="markettype" mode="market6and7">
		<xsl:param name="morebets" />
		
		<div class="temp">
			Group by Markets - Style 6 (US Sport Handicap) and Style 7 (Win 2 Way)
		</div>
		
		<xsl:if test="@ID = parent::categorygroupmarkets/@selected or parent::categorygroupmarkets/parent::selectedcategory/@typeID != 3">
			<table class="coupon">
				<xsl:apply-templates select="parent::categorygroupmarkets /parent::selectedcategory/events/event/market[@typeID=current()/@ID]" mode="market6and7">
					<xsl:with-param name="morebets" select="$morebets" />
				</xsl:apply-templates>
			</table>
		</xsl:if>
	</xsl:template>
	

	<xsl:template match="market" mode="market6and7">
		<xsl:param name="morebets" />
		
		<!-- title if needed -->
		<xsl:if test="position() = 1 or parent::event/@compID != current()/preceding-sibling::market[position()=1]/parent::event/@compID">
			<tr class="title">
				<th>
					<xsl:attribute name="colspan">
						<xsl:choose>
							<xsl:when test="@styleID=6">
								<xsl:text>5</xsl:text>
							</xsl:when>
							<xsl:otherwise>
								<xsl:text>4</xsl:text>
							</xsl:otherwise>
						</xsl:choose>
					</xsl:attribute>
					<xsl:value-of select="parent::event/@compname" />
					<xsl:text> - </xsl:text>
					<xsl:value-of select="@desc" />
				</th>
			</tr>
			<tr class="col">
				<th>
					Odds
				</th>			
				<xsl:choose>
					<!-- us sport -->
					<xsl:when test="@styleID = 6">
						<th>
							&#160;
						</th>
						<th>
							@
						</th>
						<th>
							&#160;
						</th>
					</xsl:when>
					
					<!-- win 2 way -->
					<xsl:otherwise>
						<th colspan="2">
							&#160;
						</th>
					</xsl:otherwise>
				</xsl:choose>
				<th>
					Odds
				</th>
			</tr>
		</xsl:if>

		<!-- events -->
		<tr>
			<xsl:call-template name="cssrowclass" />
			
			<xsl:apply-templates select="outcome[position()=1]" mode="oddsTD" />
			<td>
				<div class="evname">
					<xsl:value-of select="outcome[position()=1]/@desc" />
				</div>
				<div class="evdate">
					<xsl:apply-templates select="parent::event/@date" mode="date" />
				</div>
			</td>
			
			<xsl:if test="@styleID=6">
				<td>
					&#160;
				</td>
			</xsl:if>

			<xsl:apply-templates select="outcome[position()=2]" mode="td" />
			<xsl:apply-templates select="outcome[position()=2]" mode="oddsTD" />
			
			<xsl:if test="$morebets = 1 and parent::event/market[position()=2]">
				<td class="morebets">
					<a href="CategoryPage.aspx?tab={/eventpage/sporttabs/@selected}&amp;evntid={parent::event/@ID}">
						more bets
					</a>
				</td>
			</xsl:if>
		</tr>
	</xsl:template>

</xsl:stylesheet>
