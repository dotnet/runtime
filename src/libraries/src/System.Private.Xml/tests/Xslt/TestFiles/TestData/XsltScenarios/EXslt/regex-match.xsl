<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:regexp="http://exslt.org/regular-expressions" exclude-result-prefixes="regexp">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>
                <xsl:for-each select="regexp:match('http://www.bayes.co.uk/xml/index.xml?/xml/utils/rechecker.xml', 
                    '(\w+):\/\/([^/:]+)(:\d*)?([^# ]*)')">
Part <xsl:value-of select="position()" /> = <xsl:value-of select="." />
                </xsl:for-each>              
            </test1>
            <test2>
               <xsl:for-each select="regexp:match('This is a test string', '(\w+)', 'g')">
Part <xsl:value-of select="position()" /> = <xsl:value-of select="." />
                </xsl:for-each> 
            </test2>
            <test3>
                <xsl:for-each select="regexp:match('This is a test string', '([a-z])+ ', 'g')">
Part <xsl:value-of select="position()" /> = <xsl:value-of select="." />
                </xsl:for-each>
            </test3>
            <test4>
                <xsl:for-each select="regexp:match('This is a test string', '([a-z])+ ', 'gi')">
Part <xsl:value-of select="position()" /> = <xsl:value-of select="." />
                </xsl:for-each>
            </test4>
            <test5>
                <xsl:variable name="tokens" 
                select="regexp:match(entry, '(\d{1,2}/\d{1,2}/\d{4})\s+(\d{2}:\d{2})\s+(\w*)\s+(.*)')"/>
                <entry date="{$tokens[2]}" time="{$tokens[3]}" application="{$tokens[4]}" message="{$tokens[5]}"/>
            </test5>
        </out>
    </xsl:template>
</xsl:stylesheet>

  