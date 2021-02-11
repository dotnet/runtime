<?xml version="1.0" encoding="iso-8859-1"?>


<!--
 | make-opcode-def.xsl: Translates opcodes from the CIL-opcodes.xml into
 |                      a spec compliant opcodes.def file
 |                      Converted to XSLT from make-opcodes-def.pl
 |
 | See: Common Language Infrastructure (CLI) Part 5: Annexes
 |
 | Author: Sergey Chaban
 |
 | $Id$
  -->



<xsl:stylesheet version="1.0"
                xmlns:xsl="http://www.w3.org/1999/XSL/Transform">



<xsl:output method="text"/>


<xsl:template match="/">
  <xsl:apply-templates/>
  <xsl:call-template name="print-trailer"/>
</xsl:template>

<xsl:template name="to-upper">
  <xsl:param name="string"/>
  <xsl:value-of select="translate($string,
                        'abcdefghijklmnopqrstuvwxyz.-',
                        'ABCDEFGHIJKLMNOPQRSTUVWXYZ__')"/>
</xsl:template>


<xsl:template name="get-flow">
  <xsl:param name="flow" select="@flow"/>
  <xsl:choose>
    <xsl:when
         test="contains('next call return branch meta cond-branch',$flow)">
      <xsl:call-template name="to-upper">
        <xsl:with-param name="string" select="$flow"/>
      </xsl:call-template>
    </xsl:when>
    <xsl:otherwise><xsl:value-of select="'ERROR'"/></xsl:otherwise>
  </xsl:choose>     
</xsl:template>


<xsl:template match="opdesc/opcode">
  <xsl:variable name="uname">
    <xsl:call-template name="to-upper">
      <xsl:with-param name="string" select="@name"/>
    </xsl:call-template>
  </xsl:variable>
  <xsl:variable name="o1">
    <xsl:call-template name="to-upper">
      <xsl:with-param name="string" select="@o1"/>
    </xsl:call-template>
  </xsl:variable>
  <xsl:variable name="f">
    <xsl:call-template name="get-flow"/>
  </xsl:variable>
  <xsl:variable name="count"
       select="number(not(contains($o1,'FF')))+1"/>OPDEF(CEE_<xsl:value-of
               select="concat($uname,', &#x22;',@name,'&#x22;, ',
                              @input,', ',@output,', ',@args,', X, ',
                              $count,', ',@o1,', ',@o2,', ',$f
                       )"/>)
</xsl:template>


<xsl:template name="print-trailer">
#ifndef OPALIAS
#define _MONO_CIL_OPALIAS_DEFINED_
#define OPALIAS(a,s,r)
#endif

OPALIAS(CEE_BRNULL,     "brnull",    CEE_BRFALSE)
OPALIAS(CEE_BRNULL_S,   "brnull.s",  CEE_BRFALSE_S)
OPALIAS(CEE_BRZERO,     "brzero",    CEE_BRFALSE)
OPALIAS(CEE_BRZERO_S,   "brzero.s",  CEE_BRFALSE_S)
OPALIAS(CEE_BRINST,     "brinst",    CEE_BRTRUE)
OPALIAS(CEE_BRINST_S,   "brinst.s",  CEE_BRTRUE_S)
OPALIAS(CEE_LDIND_U8,   "ldind.u8",  CEE_LDIND_I8)
OPALIAS(CEE_LDELEM_U8,  "ldelem.u8", CEE_LDELEM_I8)
OPALIAS(CEE_LDX_I4_MIX, "ldc.i4.M1", CEE_LDC_I4_M1)
OPALIAS(CEE_ENDFAULT,   "endfault",  CEE_ENDFINALLY)

#ifdef _MONO_CIL_OPALIAS_DEFINED_
#undef OPALIAS
#undef _MONO_CIL_OPALIAS_DEFINED_
#endif
</xsl:template>


</xsl:stylesheet>
