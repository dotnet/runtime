<xsl:stylesheet xmlns:xsl="http://www.w3.org/1999/XSL/Transform"
              xmlns:msxsl="urn:schemas-microsoft-com:xslt"
              xmlns:user  ="USER"
              xmlns:a  ="USER_A"  
              xmlns:b  ="USER_B"
              xmlns:c  ="USER_C"
                        
              xmlns:userC="http://mycompany.com/mynamespace_CSharp"
              xmlns:userJ="http://mycompany.com/mynamespace_JScript"
              xmlns:userVB="http://mycompany.com/mynamespace_VB"
                         
              xmlns:bk='urn:loc.gov:books'    
              version="1.0">
    <xsl:output method="xml" indent="yes"  omit-xml-declaration="no" encoding="utf-8" version="1.0"/>
    <msxsl:script language="c#" implements-prefix="userC">
        <msxsl:using namespace="System.Threading" />
        <msxsl:using namespace="System.IO" />
        public int writetofile(string path,string s)
        {
        System.IO.StreamWriter sw = new StreamWriter(path);
        sw.WriteLine(s);
        sw.Close();
        return 1;
        }
    </msxsl:script>


    <msxsl:script language="jscript" implements-prefix="userJ">
        <msxsl:using namespace="System.Threading" />
        <msxsl:using namespace="System.IO" />

        function writetofile(path, str)
        {
        var fso, f1
        fso = new ActiveXObject("Scripting.FileSystemObject");
        f1 = fso.CreateTextFile(path, true);
        // Write a line.
        f1.WriteLine(str);
        f1.Close();
        return 2;
        }
    </msxsl:script>
    <msxsl:script language="VISUALBASIC" implements-prefix="userVB">
        <msxsl:using namespace="System.Threading" />
        <msxsl:using namespace="System.IO" />
        Function writetofile(ByVal path As String,ByVal mystr As String)  As Integer
        Dim sw As StreamWriter = New StreamWriter(path)
        sw.WriteLine(mystr)
        sw.Close()
        return 3
        End Function
    </msxsl:script>
    <xsl:template match="/">
        <xsl:variable name="path1" select="'C:\testsecurity\charpfile.xml'"></xsl:variable>
        <xsl:variable name="mystr" select="'hello world'"></xsl:variable>
        <xsl:variable name="myvar1" select="userC:writetofile($path1,$mystr)" />        
        <xsl:variable name="path2" select="'C:\testsecurity\jscriptfile.xml'"></xsl:variable>
        <xsl:variable name="myvar2" select="userJ:writetofile($path2,$mystr)" />        
        <xsl:variable name="path3" select="'C:\testsecurity\vbfile.xml'"></xsl:variable>
        <xsl:variable name="myvar3" select="userVB:writetofile($path3,$mystr)" />       
        <xsl:element name="result">
            <xsl:value-of select="concat($myvar1,$myvar2,$myvar3)"/>
        </xsl:element>
    </xsl:template>
</xsl:stylesheet>
