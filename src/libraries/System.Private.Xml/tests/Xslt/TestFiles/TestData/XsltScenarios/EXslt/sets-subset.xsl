<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:set2="http://gotdotnet.com/exslt/sets" 
exclude-result-prefixes="set2">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
            <xsl:variable name="all-cities" select="doc/city"/>
            <xsl:variable name="all-spanish-cities" select="doc/city[@country='Spain']"/>
Set of Spanish cities is a subset of all cities, right? 
            <xsl:value-of select="set2:subset($all-spanish-cities, $all-cities)"/>
Set of all cities is a subset of Spanish cities, right? 
            <xsl:value-of select="set2:subset($all-cities, $all-spanish-cities)"/>
            </test1>
            <test2>
                <xsl:value-of select="set2:subset(doc/city, /no/such/node)"/>
            </test2>
            <test3>
                <xsl:value-of select="set2:subset(/no/such/node, doc/city)"/>
            </test3>            
            <test4>
                <xsl:value-of select="set2:subset(/no/such/node, /no/such/node)"/>
            </test4>
        </out>
    </xsl:template>
</xsl:stylesheet>

  