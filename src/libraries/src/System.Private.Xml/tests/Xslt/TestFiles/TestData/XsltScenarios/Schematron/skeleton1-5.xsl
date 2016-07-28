<?xml version="1.0"?>
<!-- Beta Skeleton Module for the Schematron 1.5 XML Schema Language.
	http://www.ascc.net/xml/schematron/
 
 Copyright (c) 2000,2001 Rick Jelliffe and Academia Sinica Computing Center, Taiwan

 This software is provided 'as-is', without any express or implied warranty. 
 In no event will the authors be held liable for any damages arising from 
 the use of this software.

 Permission is granted to anyone to use this software for any purpose, 
 including commercial applications, and to alter it and redistribute it freely,
 subject to the following restrictions:

 1. The origin of this software must not be misrepresented; you must not claim
 that you wrote the original software. If you use this software in a product, 
 an acknowledgment in the product documentation would be appreciated but is 
 not required.

 2. Altered source versions must be plainly marked as such, and must not be 
 misrepresented as being the original software.

 3. This notice may not be removed or altered from any source distribution.
-->
<!-- 
    Version: 2001-06-12
           * same skeleton now supports namespace or no namespace
           * parameters to handlers updated for all 1.5 attributes 
           * diagnostic hints supported: command-line option diagnose=yes|no
           * phases supported: command-line option phase=#ALL|...
           * abstract rules
           * compile-time error messages
           * 1.6 feature: @match on sch:key  
          
    Contributors: Rick Jelliffe (original), Oliver Becker (architecture), 
             Miloslav Nic (diagnostic, phase, options), Ludwig Svenonius (abstract)
             Uche Ogbuji (misc. bug fixes), Jim Ancona (SAXON workaround),
             Eddie Robertsson (misc. bug fixes)

    XSLT versions tested and working as-is: 
           * MS XML 3
           * Oracle 
           * SAXON + Instant Saxon  
           * XT n.b. key() not available, will die

   XSLT version reliably reported working
           *  FourThought's Python implementation

    XSLT versions tested and requires small workaround from you
           * Sablotron does not support import, so merge meta-stylesheets by hand
           * Xalan for Java 2.0 outputs wrong namespace URI, so alter by hand or script
           * Xalan for C 1.0 has problem with key, so edit by hand. Find "KEY" below  

   If you create your own meta-stylesheet to override this one, it is a
   good idea to have both in the same directory and to run the stylesheet
   from that directory, as many XSLT implementations have ideosyncratic
   handling of URLs: keep it simple.
         
-->
<xsl:stylesheet version="1.0" 
	xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
	xmlns:axsl="http://www.w3.org/1999/XSL/TransformAlias" 
	xmlns:sch="http://www.ascc.net/xml/schematron"
	 >
<!-- Note that this namespace is not version specific.
This program implements schematron 1.5 with some 1.6 extensions -->
<xsl:namespace-alias stylesheet-prefix="axsl" result-prefix="xsl"/>
<!-- Category: top-level-element -->
<xsl:output method="xml" omit-xml-declaration="no" standalone="yes"  indent="yes"/>
<xsl:param name="block"></xsl:param><!-- reserved -->
<xsl:param name="phase">
  <xsl:choose>
    <xsl:when test="//sch:schema/@defaultPhase">
      <xsl:value-of select="//sch:schema/@defaultPhase"/>
    </xsl:when>
    <xsl:otherwise>#ALL</xsl:otherwise>
  </xsl:choose>
</xsl:param>
<xsl:param name="hiddenKey"> key </xsl:param><!-- workaround for Xalan4J 2.0 -->

<!-- SCHEMA -->
<xsl:template match="sch:schema | schema">
	<axsl:stylesheet version="1.0">
		<xsl:for-each select="sch:ns | ns">
			<xsl:attribute name="{concat(@prefix,':dummy-for-xmlns')}" namespace="{@uri}"/>
		</xsl:for-each>
 
		<xsl:if test="count(sch:title/* | title/* )">
			<xsl:message>
				<xsl:text>Warning: </xsl:text>
				<xsl:value-of select="name(.)"/>
				<xsl:text> must not contain any child elements</xsl:text>
			</xsl:message>
		</xsl:if>
 
		<xsl:call-template name="process-prolog"/>
		<!-- utility routine for implementations -->
   		<axsl:template match="*|@*" mode="schematron-get-full-path">

			<axsl:apply-templates select="parent::*" mode="schematron-get-full-path"/>
			<axsl:text>/</axsl:text>
			<axsl:if test="count(. | ../@*) = count(../@*)">@</axsl:if>
			<axsl:value-of select="name()"/>
			<axsl:text>[</axsl:text>
	  		<axsl:value-of select="1+count(preceding-sibling::*[name()=name(current())])"/>
	  		<axsl:text>]</axsl:text>
       	 	</axsl:template>

		<xsl:apply-templates mode="do-keys" 
                select="sch:pattern/sch:rule/sch:key | pattern/rule/key | sch:key | key "/>


		<axsl:template match="/">
			<xsl:call-template name="process-root">
				<xsl:with-param name="fpi" select="@fpi"/>
				<xsl:with-param 	xmlns:sch="http://www.ascc.net/xml/schematron"
				name="title" select="./sch:title | title"/>
				<xsl:with-param name="id" select="@id"/>
				<xsl:with-param name="icon" select="@icon"/>
				<xsl:with-param name="lang" select="@xml:lang"/>
				<xsl:with-param name="version" select="@version" />
				<xsl:with-param name="schemaVersion" select="@schemaVersion" />
				<xsl:with-param name="contents">
					<xsl:apply-templates mode="do-all-patterns"/>
				</xsl:with-param>
			</xsl:call-template>
		</axsl:template>
 
		<xsl:apply-templates/>
		<axsl:template match="text()" priority="-1">
			<!-- strip characters -->
		</axsl:template>
	</axsl:stylesheet>
</xsl:template>

	<!-- ACTIVE -->
	<xsl:template match="sch:active | active">
                <xsl:if test="not(@pattern)">
                    <xsl:message>Markup Error: no pattern attribute in &lt;active></xsl:message>
                </xsl:if>
                <xsl:if test="//sch:rule[@id= current()/@pattern]">
                    <xsl:message>Reference Error: the pattern  "<xsl:value-of select="@pattern"/>" has been activated but is not declared</xsl:message>
                </xsl:if>
        </xsl:template>

	<!-- ASSERT and REPORT -->
	<xsl:template match="sch:assert | assert">
                <xsl:if test="not(@test)">
                    <xsl:message>Markup Error: no test attribute in &lt;assert></xsl:message>
                </xsl:if>
		<axsl:choose>
			<axsl:when test="{@test}"/>
			<axsl:otherwise>
				<xsl:call-template name="process-assert">
					<xsl:with-param name="role" select="@role"/>
					<xsl:with-param name="id" select="@id"/>
					<xsl:with-param name="test" select="normalize-space(@test)" />
					<xsl:with-param name="icon" select="@icon"/>
					<xsl:with-param name="subject" select="@subject"/>
					<xsl:with-param name="diagnostics" select="@diagnostics"/>
				</xsl:call-template>  
			</axsl:otherwise>
		</axsl:choose>
	</xsl:template>
	<xsl:template match="sch:report | report">
                <xsl:if test="not(@test)">
                    <xsl:message>Markup Error: no test attribute in &lt;report></xsl:message>
                </xsl:if>
		<axsl:if test="{@test}">
			<xsl:call-template name="process-report">
				<xsl:with-param name="role" select="@role"/>
				<xsl:with-param name="test" select="normalize-space(@test)" />
				<xsl:with-param name="icon" select="@icon"/>
				<xsl:with-param name="id" select="@id"/>
				<xsl:with-param name="subject" select="@subject"/>
				<xsl:with-param name="diagnostics" select="@diagnostics"/>
			</xsl:call-template>
		</axsl:if>
	</xsl:template>


	<!-- DIAGNOSTIC -->
	<xsl:template match="sch:diagnostic | diagnostic"
              ><xsl:if test="not(@id)"
                    ><xsl:message>Markup Error: no id attribute in &lt;diagnostic></xsl:message
                ></xsl:if><xsl:call-template name="process-diagnostic">
                <xsl:with-param name="id" select="@id" />
               </xsl:call-template>
        </xsl:template>

	<!-- DIAGNOSTICS -->
	<xsl:template match="sch:diagnostics | diagnostics"/>

	<!-- DIR -->
	<xsl:template match="sch:dir | dir"  mode="text"
		><xsl:call-template name="process-dir">
			<xsl:with-param name="value" select="@value"/>
		</xsl:call-template>
	</xsl:template>

	<!-- EMPH -->
	<xsl:template match="sch:emph | emph"  mode="text"
		><xsl:call-template name="process-emph"/>
	</xsl:template>

	<!-- EXTENDS -->
	<xsl:template match="sch:extends | extends">
		<xsl:if test="not(@rule)"
                    ><xsl:message>Markup Error: no rule attribute in &lt;extends></xsl:message
                ></xsl:if>
     		<xsl:if test="not(//sch:rule[@abstract='true'][@id= current()/@rule] )
                    and not(//rule[@abstract='true'][@id= current()/@rule])">
                    <xsl:message>Reference Error: the abstract rule  "<xsl:value-of select="@rule"/>" has been referenced but is not declared</xsl:message>
                </xsl:if>
	        <xsl:call-template name="IamEmpty" />

  		<xsl:if test="//sch:rule[@id=current()/@rule]">
    			<xsl:apply-templates select="//sch:rule[@id=current()/@rule]"
				mode="extends"/>
  		</xsl:if>

	</xsl:template>

	<!-- KEY -->
	<!-- do we need something to test uniqueness too? --> 
	<!-- NOTE: if you get complaint about "key" here (e.g. Xalan4C 1.0) replace
		"key" with "$hiddenKey" -->
	<xsl:template  match="sch:key | key " mode="do-keys" >
                <xsl:if test="not(@name)">
                    <xsl:message>Markup Error: no name attribute in &lt;key></xsl:message>
                </xsl:if>
               <xsl:if test="not(@match) and not(../sch:rule)">
                    <xsl:message>Markup Error:  no match attribute on &lt;key> outside &lt;rule></xsl:message>
                </xsl:if>
                <xsl:if test="not(@path)">
                    <xsl:message>Markup Error: no path attribute in &lt;key></xsl:message>
                </xsl:if>
	        <xsl:call-template name="IamEmpty" />

             <xsl:choose>
			<xsl:when test="@match">
				<axsl:key match="{@match}" name="{@name}" use="{@path}"/>
			</xsl:when>
			<xsl:otherwise>
				<axsl:key name="{@name}" match="{parent::sch:rule/@context}" use="{@path}"/>
			</xsl:otherwise>
		</xsl:choose>
	</xsl:template>

      <xsl:template match="sch:key | key"  /><!-- swallow --> 

	<!-- NAME -->
	<xsl:template match="sch:name | name" mode="text">
		<axsl:text xml:space="preserve"> </axsl:text>
			<xsl:if test="@path"
				><xsl:call-template name="process-name">
					<xsl:with-param name="name" select="concat('name(',@path,')')"/>
					<!-- SAXON needs that instead of  select="'name({@path})'"  -->
				</xsl:call-template>
			</xsl:if>
			<xsl:if test="not(@path)"
				><xsl:call-template name="process-name">
					<xsl:with-param name="name" select="'name(.)'"/>
				</xsl:call-template>
			</xsl:if>
	        	<xsl:call-template name="IamEmpty" />
		<axsl:text xml:space="preserve"> </axsl:text>
	</xsl:template>

	<!-- NS -->
	<xsl:template match="sch:ns | ns"  mode="do-all-patterns" >
               <xsl:if test="not(@uri)">
                    <xsl:message>Markup Error: no uri attribute in &lt;ns></xsl:message>
                </xsl:if>
               <xsl:if test="not(@prefix)">
                    <xsl:message>Markup Error: no prefix attribute in &lt;ns></xsl:message>
                </xsl:if>
	        <xsl:call-template name="IamEmpty" />
		<xsl:call-template name="process-ns" >
			<xsl:with-param name="prefix" select="@prefix"/>
			<xsl:with-param name="uri" select="@uri"/>
		</xsl:call-template>
	</xsl:template>
	<xsl:template match="sch:ns | ns"  /><!-- swallow -->

	<!-- P -->
	<xsl:template match="sch:schema/sch:p | schema/p" mode="do-schema-p" >
		<xsl:call-template name="process-p">
			<xsl:with-param name="class" select="@class"/>
			<xsl:with-param name="icon" select="@icon"/>
			<xsl:with-param name="id" select="@id"/>
			<xsl:with-param name="lang" select="@xml:lang"/>
		</xsl:call-template>
	</xsl:template>
	<xsl:template match="sch:pattern/sch:p | pattern/p" mode="do-pattern-p" >
		<xsl:call-template name="process-p">
			<xsl:with-param name="class" select="@class"/>
			<xsl:with-param name="icon" select="@icon"/>
			<xsl:with-param name="id" select="@id"/>
			<xsl:with-param name="lang" select="@xml:lang"/>
		</xsl:call-template>
	</xsl:template>
	<xsl:template match="sch:phase/sch:p" /><!-- We don't use these -->
	<xsl:template match="sch:p | p" />

	<!-- PATTERN -->
	<xsl:template match="sch:pattern | pattern" mode="do-all-patterns">
	<xsl:if test="($phase = '#ALL') 
	or (../sch:phase[@id= ($phase)]/sch:active[@pattern= current()/@id])
	or (../phase[@id= ($phase)]/active[@id= current()/@id])">
		<xsl:call-template name="process-pattern">
			<xsl:with-param name="name" select="@name"/>
			<xsl:with-param name="id" select="@id"/>
			<xsl:with-param name="see" select="@see"/>
			<xsl:with-param name="fpi" select="@fpi"/>
			<xsl:with-param name="icon" select="@icon"/>
		</xsl:call-template>
		<axsl:apply-templates select="/" mode="M{count(preceding-sibling::*)}"/>
        </xsl:if>
	</xsl:template>
	
	<xsl:template match="sch:pattern | pattern">
        <xsl:if test="($phase = '#ALL') 
	or (../sch:phase[@id= ($phase)]/sch:active[@pattern= current()/@id])
	or (../phase[@id= ($phase)]/active[@id= current()/@id])">
		<xsl:apply-templates/>
		<axsl:template match="text()" priority="-1" mode="M{count(preceding-sibling::*)}">
			<!-- strip characters -->
		</axsl:template>
        </xsl:if>
	</xsl:template>

	<!-- PHASE -->
	<xsl:template match="sch:phase | phase" >
                <xsl:if test="not(@id)">
                    <xsl:message>Markup Error: no id attribute in &lt;phase></xsl:message>
                </xsl:if>
	</xsl:template>

	<!-- RULE -->
	<xsl:template match="sch:rule[not(@abstract='true')] | rule[not(@abstract='true')]">
                <xsl:if test="not(@context)">
                    <xsl:message>Markup Error: no context attribute in &lt;rule></xsl:message>
                </xsl:if>
		<axsl:template match="{@context}" priority="{4000 - count(preceding-sibling::*)}" mode="M{count(../preceding-sibling::*)}">
			<xsl:call-template name="process-rule">
				<xsl:with-param name="id" select="@id"/>
				<xsl:with-param name="context" select="@context"/>
				<xsl:with-param name="role" select="@role"/>
			</xsl:call-template>
			<xsl:apply-templates/>
			<axsl:apply-templates mode="M{count(../preceding-sibling::*)}"/>
		</axsl:template>
	</xsl:template>


	<!-- ABSTRACT RULE -->
	<xsl:template match="sch:rule[@abstract='true'] | rule[@abstract='true']" >
		<xsl:if test=" not(@id)">
                    <xsl:message>Markup Error: no id attribute on abstract &lt;rule></xsl:message>
                </xsl:if>
 		<xsl:if test="@context">
                    <xsl:message>Markup Error: (2) context attribute on abstract &lt;rule></xsl:message>
                </xsl:if>
	</xsl:template>

	<xsl:template match="sch:rule[@abstract='true'] | rule[@abstract='true']"
		mode="extends" >
                <xsl:if test="@context">
                    <xsl:message>Markup Error: context attribute on abstract &lt;rule></xsl:message>
                </xsl:if>
			<xsl:apply-templates/>
	</xsl:template>

	<!-- SPAN -->
	<xsl:template match="sch:span | span" mode="text">
		<xsl:call-template name="process-span"
			><xsl:with-param name="class" select="@class"/>
		</xsl:call-template>
	</xsl:template>

	<!-- TITLE -->
	<!-- swallow -->
	<xsl:template match="sch:title | title" /> 

	<!-- VALUE-OF -->
	<xsl:template match="sch:value-of | value-of" mode="text" >
               <xsl:if test="not(@select)">
                    <xsl:message>Markup Error: no select attribute in &lt;value-of></xsl:message>
                </xsl:if>
	        <xsl:call-template name="IamEmpty" />
		<axsl:text xml:space="preserve"> </axsl:text>
		<xsl:choose>
			<xsl:when test="@select"
				><xsl:call-template name="process-value-of">
					<xsl:with-param name="select" select="@select"/>  
                                   <!-- will saxon have problem with this too?? -->
				</xsl:call-template>
			</xsl:when>
			<xsl:otherwise >
				<xsl:call-template name="process-value-of"
					><xsl:with-param name="select" select="'.'"/>
				</xsl:call-template>
			</xsl:otherwise>
                </xsl:choose>
		<axsl:text xml:space="preserve"> </axsl:text>
	</xsl:template>

<!-- ============================================================== -->
	<!-- Text -->
	<xsl:template match="text()" priority="-1" mode="do-keys">
		<!-- strip characters -->
	</xsl:template>
	<xsl:template match="text()" priority="-1" mode="do-all-patterns">
		<!-- strip characters -->
	</xsl:template>
        <xsl:template match="text()" priority="-1" mode="do-schema-p">
		<!-- strip characters -->
	</xsl:template>
        <xsl:template match="text()" priority="-1" mode="do-pattern-p">
		<!-- strip characters -->
	</xsl:template>
	<xsl:template match="text()" priority="-1">
		<!-- strip characters -->
	</xsl:template>
	<xsl:template match="text()" mode="text">
		<xsl:value-of select="normalize-space(.)"/>
	</xsl:template>

	<xsl:template match="text()" mode="inline-text">
		<xsl:value-of select="."/>
	</xsl:template>

<!-- ============================================================== -->
<!-- utility templates -->
<xsl:template name="IamEmpty">
	<xsl:if test="count( * )">
		<xsl:message>
			<xsl:text>Warning: </xsl:text>
			<xsl:value-of select="name(.)"/>
			<xsl:text> must not contain any child elements</xsl:text>
		</xsl:message>
	</xsl:if>
</xsl:template>

<xsl:template name="diagnosticsSplit">
  <!-- Process at the current point the first of the <diagnostic> elements
       referred to parameter str, and then recurse -->
  <xsl:param name="str"/>
  <xsl:variable name="start">
    <xsl:choose>
      <xsl:when test="contains($str,' ')">
	<xsl:value-of  select="substring-before($str,' ')"/>
      </xsl:when>
      <xsl:otherwise><xsl:value-of select="$str"/></xsl:otherwise>
    </xsl:choose>
  </xsl:variable>

  <xsl:variable name="end">
    <xsl:if test="contains($str,' ')">
      <xsl:value-of select="substring-after($str,' ')"/>
    </xsl:if>
  </xsl:variable>

  <xsl:if test="not(string-length(normalize-space($start)) = 0)
	and not(//sch:diagnostic[@id = ($start)]) and not(//diagnostic[@id = ($start)])">
	<xsl:message>Reference error: A diagnostic "<xsl:value-of select="string($start)"/>" has been referenced but is not declared</xsl:message>
  </xsl:if>

  <xsl:if test="string-length(normalize-space($start)) > 0">
     <xsl:apply-templates 
        select="//sch:diagnostic[@id = ($start) ] | //diagnostic[@id= ($start) ]"/>
  </xsl:if>

  <xsl:if test="not($end='')">
    <xsl:call-template name="diagnosticsSplit">
      <xsl:with-param name="str" select="$end"/>
    </xsl:call-template>
  </xsl:if>
</xsl:template>


<!-- ============================================================== -->

	<xsl:template match="*">
		<xsl:message>
			<xsl:text>Warning: unrecognized element </xsl:text>
			<xsl:value-of select="name(.)"/>
		</xsl:message>
	</xsl:template>
	<xsl:template match="*" mode="text">
		<xsl:message>
			<xsl:text>Warning: unrecognized element </xsl:text>
			<xsl:value-of select="name(.)"/>
		</xsl:message>
	</xsl:template>
<!-- ============================================================== -->
	<!-- Default named templates -->
	<!-- These are the actions that are performed unless overridden -->
	<xsl:template name="process-prolog"/>
	<!-- no params -->
	<xsl:template name="process-root">
		<xsl:param name="contents"/>
		<!-- unused params: fpi, title, id, icon, lang, version, schemaVersion -->
		<xsl:copy-of select="$contents"/>
	</xsl:template>
	<xsl:template name="process-assert">
		<xsl:param name="role"/>
		<xsl:param name="test"/>
		<!-- unused parameters: id, icon, diagnostics, subject -->
		<xsl:call-template name="process-message">
			<xsl:with-param name="pattern" select="$test"/>
			<xsl:with-param name="role" select="$role"/>
		</xsl:call-template>
	</xsl:template>
	<xsl:template name="process-report">
		<xsl:param name="role"/>
		<xsl:param name="test"/>
		<!-- unused parameters: id, icon, diagnostics, subject -->
		<xsl:call-template name="process-message">
			<xsl:with-param name="pattern" select="$test"/>
			<xsl:with-param name="role" select="$role"/>
		</xsl:call-template>
	</xsl:template>
	<xsl:template name="process-diagnostic">
		<!-- params: id -->
		<xsl:apply-templates mode="text"/>
	</xsl:template>
	<xsl:template name="process-dir" 
		><xsl:apply-templates mode="inline-text"/></xsl:template>
	<xsl:template name="process-emph" 
		><xsl:apply-templates mode="inline-text"/></xsl:template>
	<xsl:template name="process-name">
		<xsl:param name="name"
		/><axsl:value-of select="{$name}"/></xsl:template>
	<xsl:template name="process-ns" />
	<!-- unused params: prefix, uri -->
	<!-- note that this is independent of the use of sch:ns by sch:schema -->
	<xsl:template name="process-p"/>
	<!-- unused params: class, id, icon, lang -->
	<xsl:template name="process-pattern"/>
	<!-- unused params: name, id, see, fpi, icon -->
	<xsl:template name="process-rule"/>
	<!-- unused params: id, context, role -->
	<xsl:template name="process-span" 
		><xsl:apply-templates mode="inline-test"/></xsl:template>
	<xsl:template name="process-value-of">
		<xsl:param name="select"
		/><axsl:value-of select="{$select}"/></xsl:template>
	<!-- default output action: the simplest customization is to just override this -->
	<xsl:template name="process-message">
		<!-- params: pattern, role -->
		<xsl:apply-templates mode="text"/>
	</xsl:template>
</xsl:stylesheet>

