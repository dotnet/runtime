<?xml version="1.0" encoding="UTF-8" ?>
<xsl:stylesheet version="1.0" xmlns:xsl="http://www.w3.org/1999/XSL/Transform" 
xmlns:dyn2="http://gotdotnet.com/exslt/dynamic" exclude-result-prefixes="dyn2">
    <xsl:output indent="yes" omit-xml-declaration="yes"/>
    <xsl:template match="data">
        <out>
            <test1>                
                <xsl:value-of select="dyn2:evaluate(., concat('2', '+', '2'))"/>
            </test1>
            <test2>                
                <xsl:copy-of select="dyn2:evaluate(., /data/path)"/>
            </test2>
            <test3>                
                <xsl:value-of select="dyn2:evaluate(., /data/path)/@id"/>
            </test3>
            <test4>                
                <xsl:copy-of select="dyn2:evaluate(., /data/path2)"/>
            </test4>               
            <test5>                
                <xsl:value-of select="dyn2:evaluate(/no/such/node, /data/path2)"/>
            </test5>               
            <test6>                
                <xsl:copy-of select="dyn2:evaluate(., '')"/>
            </test6>
            <test7>                
                <xsl:copy-of select="dyn2:evaluate(., /data/path3)"/>
            </test7>
            <test8>                                
                <xsl:copy-of select="dyn2:evaluate(., 'dyn2:evaluate(., str:concat(path4|path5|path6))')"/>
            </test8>
            <test9>                                
                <xsl:copy-of select="dyn2:evaluate(., 'orders/order[last()]')"/>
            </test9>
            <test10>
                <xsl:variable name="namespaces">xmlns:foo="http://orders.com" xmlns:bar='http://bar.com/'</xsl:variable>                                
                <xsl:copy-of select="dyn2:evaluate(., /data/path7, $namespaces)"/>
            </test10>
            <test11>                
                <xsl:copy-of select="dyn2:evaluate(., /data/path7, '')"/>
            </test11>               
            <test12>
                <xsl:variable name="namespaces">
                    xmlns:bar='http://bar.com/" 
                    xmlns:foo =   "http://orders.com"
                </xsl:variable>                                
                <xsl:copy-of select="dyn2:evaluate(., /data/path7, $namespaces)"/>
            </test12>   
            <test13>
                <xsl:copy-of select="dyn2:evaluate(., 'current()')"/>
            </test13>                    
        </out>
    </xsl:template>
</xsl:stylesheet>

  