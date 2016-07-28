<?xml version="1.0" ?>
<!-- Report Generator for the Schematron XML Schema Language.
	http://www.ascc.net/xml/resource/schematron/schematron.html
   
 Copyright (c) 2000,2001 David Calisle, Oliver Becker,
	 Rick Jelliffe and Academia Sinica Computing Center, Taiwan

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

    1999-10-25  Version for David Carlisle's schematron-report error browser
    1999-11-5   Beta for 1.2 DTD
    1999-12-26  Add code for namespace: thanks DC
    1999-12-28  Version fix: thanks Uche Ogbuji
    2000-03-27  Generate version: thanks Oliver Becker
    2000-10-20  Fix '/' in do-all-patterns: thanks Uche Ogbuji
    2001-02-15  Port to 1.5 code
    2001-03-15  Diagnose test thanks Eddie Robertsson
-->

<!-- Schematron report -->

<xsl:stylesheet
   version="1.0"
   xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
   xmlns:axsl="http://www.w3.org/1999/XSL/TransformAlias">

<xsl:import href="skeleton1-5.xsl"/>
<xsl:param name="diagnose">yes</xsl:param>     

<xsl:template name="process-prolog">
   <axsl:output method="html" />
</xsl:template>

<xsl:template name="process-root">
   <xsl:param name="title" />
   <xsl:param name="icon" />
   <xsl:param name="contents" />
   <html>
      <style>
         a:link    { color: black}
         a:visited { color: gray}
         a:active  { color: #FF0088}
         h3        { background-color:black; color:white;
                     font-family:Arial Black; font-size:12pt; }
         h3.linked { background-color:black; color:white;
                     font-family:Arial Black; font-size:12pt; }
      </style>
      <h2 title="Schematron contact-information is at the end of 
                 this page">
         <xsl:if test="$icon"><img src="{$icon}" /></xsl:if>
         <font color="#FF0080" >Schematron</font> Report
      </h2>
      <h1 title="{@ns} {@fpi}">
         <xsl:value-of select="$title" />
      </h1>
      <div class="errors">
         <ul>
            <xsl:copy-of select="$contents" />
         </ul>
      </div>
      <hr color="#FF0080" />
      <p><font size="2">Schematron Report by David Carlisle.
      <a href="http://www.ascc.net/xml/resource/schematron/schematron.html"
         title="Link to the home page of the Schematron, 
                a tree-pattern schema language">
         <font color="#FF0080" >The Schematron</font>
      </a> by
      <a href="mailto:ricko@gate.sinica.edu.tw"
         title="Email to Rick Jelliffe (pronounced RIK JELIF)"
      >Rick Jelliffe</a>,
      <a href="http://www.sinica.edu.tw"
         title="Link to home page of Academia Sinica"
      >Academia Sinica Computing Centre</a>.
      </font></p>
   </html>
</xsl:template>

<xsl:template name="process-p">
   <xsl:param name="icon" />
<p><xsl:if test="$icon"><img src="{$icon}" /> </xsl:if
><xsl:apply-templates mode="text"/></p>
</xsl:template>

<xsl:template name="process-pattern">
   <xsl:param name="icon" />
   <xsl:param name="name" />
   <xsl:param name="see" />
   <xsl:choose>
      <xsl:when test="$see">
         <a href="{$see}" target="SRDOCO" 
            title="Link to User Documentation:">
            <h3 class="linked">
               <xsl:value-of select="$name" />
            </h3>
         </a>
      </xsl:when>
      <xsl:otherwise>
         <h3><xsl:value-of select="$name" /></h3>
      </xsl:otherwise>
   </xsl:choose>
   <xsl:if test="$icon"><img src="{$icon}" /> </xsl:if>
</xsl:template>

<!-- use default rule for process-name: output name -->

<xsl:template name="process-assert">
   <xsl:param name="icon" />
   <xsl:param name="pattern" />
   <xsl:param name="role" />
   <xsl:param name="diagnostics" />
   <li>
   <xsl:if test="$icon"><img src="{$icon}" /> </xsl:if>
      <a href="schematron-out.html#{{generate-id(.)}}" target="out"
         title="Link to where this pattern was expected">
         <i><xsl:value-of select="$role"/></i>
         <xsl:apply-templates mode="text"/>
         <xsl:if test="$diagnose = 'yes'">
          <b><xsl:call-template name="diagnosticsSplit">
               <xsl:with-param name="str" select="$diagnostics" />
           </xsl:call-template></b>
         </xsl:if>
      </a>
   </li>                    
</xsl:template>

<xsl:template name="process-report">
   <xsl:param name="pattern" />
   <xsl:param name="icon" />
   <xsl:param name="role" />
   <xsl:param name="diagnostics" />
   <li>
   <xsl:if test="$icon"><img src="{$icon}" /> </xsl:if>
      <a href="schematron-out.html#{{generate-id(.)}}" target="out"
         title="Link to where this pattern was found">
         <i><xsl:value-of select="$role"/></i>
         <xsl:apply-templates mode="text"/>
          <b><xsl:call-template name="diagnosticsSplit">
               <xsl:with-param name="str" select="$diagnostics" />
           </xsl:call-template></b>
      </a>
   </li>
</xsl:template>


</xsl:stylesheet>
