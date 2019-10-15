<xsl:stylesheet version='1.0' 
     xmlns:xsl='http://www.w3.org/1999/XSL/Transform'
     xmlns:msxsl='urn:schemas-microsoft-com:xslt'
     xmlns:theScript='urn:CustomScript'>
   
  <msxsl:script implements-prefix='theScript' language='C#'>
  <![CDATA[
  public string HelloName(string name)
  {
    return "Hello " + name;
  }
  ]]>
  </msxsl:script>
  
  <xsl:template match='/'>
    <foo>
      Script Result: <xsl:value-of select='theScript:HelloName("Foo")' />
    </foo>
  </xsl:template>
  
</xsl:stylesheet>