using OLEDB.Test.ModuleCore;
using System;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using System.Security.Policy;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;

namespace XsltApiV2
{
    [TestCase(Name = "Semi-Trusted XSLT Security tests", Desc = "This testcase tests partially trusted issues and basic XSLT operations with semi trusted app domains and limited permissions")]
    public class XslSemiTrustTest : XsltApiTestCaseBase
    {
        // Sanity
        /// Retail Mode
        [Variation("Semi-trusted App Domain: Load(IXPathNavigable = null) Special permissions sanity check, Retail", Pri = 1, Params = new object[] { "Special", "XsltApiV2.Var1Class", "false" })]
        [Variation("Semi-trusted App Domain: Load(IXPathNavigable = null) LocalIntranet sanity check, Retail", Pri = 1, Params = new object[] { "LocalIntranet", "XsltApiV2.Var1Class", "false" })]
        [Variation("Semi-trusted App Domain: Load(IXPathNavigable = null) Internet sanity check, Retail", Pri = 1, Params = new object[] { "Internet", "XsltApiV2.Var1Class", "false" })]
        //XslCompiledTransform should be able to transform an existing Stream,TextReader,XmlReader,XPathNavigator (with no includes/imports/scriptblocks/document() function) against an existing Stream,TextReader,XmlReader,XPathNavigator and write the results to an existing Stream, TextWriter, or XmlWriter.
        [Variation("Semi-trusted App Domain: Special: Xsl stylesheet with nothing special, Transform() with no permissions, Retail", Pri = 1, Params = new object[] { "Special", "XsltApiV2.Var2Class", "false" })]
        [Variation("Semi-trusted App Domain: LocalIntranet: Xsl stylesheet with nothing special, Transform() with no permissions, Retail", Pri = 1, Params = new object[] { "LocalIntranet", "XsltApiV2.Var2Class", "false" })]
        [Variation("Semi-trusted App Domain: Internet: Xsl stylesheet with nothing special, Transform() with no permissions, Retail", Pri = 1, Params = new object[] { "Internet", "XsltApiV2.Var2Class", "false" })]
        [Variation("Semi-trusted App Domain: FullTrust : Xsl stylesheet with script block, Transform() should succeed in FullTrust, Retail", Pri = 1, Params = new object[] { "FullTrust", "XsltApiV2.Var3Class", "false" })]
        [Variation("Semi-trusted App Domain: FullTrust : Xsl stylesheet with document() function, Transform() should succeed in Full Trust, Retail", Pri = 1, Params = new object[] { "FullTrust", "XsltApiV2.Var4Class", "false" })]
        [Variation("Semi-trusted App Domain: FullTrust : Xsl stylesheet with import, Transform() should succeed in Full Trust, Retail", Pri = 1, Params = new object[] { "FullTrust", "XsltApiV2.Var5Class", "false" })]
        [Variation("Semi-trusted App Domain: FullTrust : Xml document with DTD external entity, should succeed in Full Trust, Retail", Pri = 1, Params = new object[] { "FullTrust", "XsltApiV2.Var6Class", "false" })]

        /// Debug Mode
        [Variation("Semi-trusted App Domain: Load(IXPathNavigable = null) Special permissions sanity check, Debug.", Pri = 1, Params = new object[] { "Special", "XsltApiV2.Var1Class", "true" })]
        [Variation("Semi-trusted App Domain: Load(IXPathNavigable = null) LocalIntranet sanity check, Debug.", Pri = 1, Params = new object[] { "LocalIntranet", "XsltApiV2.Var1Class", "true" })]
        [Variation("Semi-trusted App Domain: Load(IXPathNavigable = null) Internet sanity check, Debug.", Pri = 1, Params = new object[] { "Internet", "XsltApiV2.Var1Class", "true" })]
        //XslCompiledTransform should be able to transform an existing Stream,TextReader,XmlReader,XPathNavigator (with no includes/imports/scriptblocks/document() function) against an existing Stream,TextReader,XmlReader,XPathNavigator and write the results to an existing Stream, TextWriter, or XmlWriter.
        [Variation("Semi-trusted App Domain: Special: Xsl stylesheet with nothing special, Transform() with no permissions, Debug.", Pri = 1, Params = new object[] { "Special", "XsltApiV2.Var2Class", "true" })]
        [Variation("Semi-trusted App Domain: LocalIntranet: Xsl stylesheet with nothing special, Transform() with no permissions, Debug.", Pri = 1, Params = new object[] { "LocalIntranet", "XsltApiV2.Var2Class", "true" })]
        [Variation("Semi-trusted App Domain: Internet: Xsl stylesheet with nothing special, Transform() with no permissions, Debug.", Pri = 1, Params = new object[] { "Internet", "XsltApiV2.Var2Class", "true" })]
        [Variation("Semi-trusted App Domain: FullTrust : Xsl stylesheet with script block, Transform() should succeed in FullTrust, Debug.", Pri = 1, Params = new object[] { "FullTrust", "XsltApiV2.Var3Class", "true" })]
        [Variation("Semi-trusted App Domain: FullTrust : Xsl stylesheet with document() function, Transform() should succeed in Full Trust, Debug.", Pri = 1, Params = new object[] { "FullTrust", "XsltApiV2.Var4Class", "true" })]
        [Variation("Semi-trusted App Domain: FullTrust : Xsl stylesheet with import, Transform() should succeed in Full Trust, Debug.", Pri = 1, Params = new object[] { "FullTrust", "XsltApiV2.Var5Class", "true" })]
        [Variation("Semi-trusted App Domain: FullTrust : Xml document with DTD external entity, should succeed in Full Trust, Debug.", Pri = 1, Params = new object[] { "FullTrust", "XsltApiV2.Var6Class", "true" })]

        // Var1() For variations expected to pass.
        public int Var1()
        {
            if (_isInProc) return TEST_SKIPPED;
            var retVal = false;
            var domain = GetSemiTrustedAppDomainForVariation(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());
            retVal = domain.badPlugin.Run(_standardTests, Boolean.Parse(CurVariation.Params[2].ToString()));
            if (retVal == true) return TEST_PASS;
            else return TEST_FAIL;
        }

        [Variation("Semi-trusted App Domain: Special: Xsl stylesheet with script block, Transform() should fail, Retail", Pri = 1, Params = new object[] { "Special", "XsltApiV2.Var3Class", "false" })]
        [Variation("Semi-trusted App Domain: Special: Xsl stylesheet with document() function, Transform() should fail, Retail", Pri = 1, Params = new object[] { "Special", "XsltApiV2.Var4Class", "false" })]
        [Variation("Semi-trusted App Domain: Special: Xsl stylesheet with import, Transform() should fail, Retail", Pri = 1, Params = new object[] { "Special", "XsltApiV2.Var5Class", "false" })]
        [Variation("Semi-trusted App Domain: Special: Xml document with DTD external entity, should fail, Retail", Pri = 1, Params = new object[] { "Special", "XsltApiV2.Var6Class", "false" })]
        [Variation("Semi-trusted App Domain: LocalIntranet : Xsl stylesheet with script block, Transform() should fail, Retail", Pri = 1, Params = new object[] { "LocalIntranet", "XsltApiV2.Var3Class", "false" })]
        [Variation("Semi-trusted App Domain: LocalIntranet : Xsl stylesheet with document() function, Transform() should fail, Retail", Pri = 1, Params = new object[] { "LocalIntranet", "XsltApiV2.Var4Class", "false" })]
        [Variation("Semi-trusted App Domain: LocalIntranet : Xsl stylesheet with import, Transform() should fail, Retail", Pri = 1, Params = new object[] { "LocalIntranet", "XsltApiV2.Var5Class", "false" })]
        [Variation("Semi-trusted App Domain: LocalIntranet : Xml document with DTD external entity, should fail, Retail", Pri = 1, Params = new object[] { "LocalIntranet", "XsltApiV2.Var6Class", "false" })]
        [Variation("Semi-trusted App Domain: Internet : Xsl stylesheet with script block, Transform() should fail, Retail", Pri = 1, Params = new object[] { "Internet", "XsltApiV2.Var3Class", "false" })]
        [Variation("Semi-trusted App Domain: Internet : Xsl stylesheet with document() function, Transform() should fail, Retail", Pri = 1, Params = new object[] { "Internet", "XsltApiV2.Var4Class", "false" })]
        [Variation("Semi-trusted App Domain: Internet : Xsl stylesheet with import, Transform() should fail, Retail", Pri = 1, Params = new object[] { "Internet", "XsltApiV2.Var5Class", "false" })]
        [Variation("Semi-trusted App Domain: Internet : Xml document with DTD external entity, should fail, Retail", Pri = 1, Params = new object[] { "Internet", "XsltApiV2.Var6Class", "false" })]
        //      [Variation("Semi-trusted App Domain: MyComputer : Xsl stylesheet with script block, Transform() should fail",          Pri=1, Params = new object[]{"MyComputer","XsltApiV2.Var3Class"})]
        //      [Variation("Semi-trusted App Domain: MyComputer : Xsl stylesheet with document() function, Transform() should fail",   Pri=1, Params = new object[]{"MyComputer","XsltApiV2.Var4Class"})]
        //      [Variation("Semi-trusted App Domain: MyComputer : Xsl stylesheet with import, Transform() should fail",                Pri=1, Params = new object[]{"MyComputer","XsltApiV2.Var5Class"})]
        //      [Variation("Semi-trusted App Domain: MyComputer : Xml document with DTD external entity, should fail",                 Pri=1, Params = new object[]{"MyComputer","XsltApiV2.Var6Class"})]
        [Variation("Semi-trusted App Domain: Special: Xsl stylesheet with script block, Transform() should fail, Debug.", Pri = 1, Params = new object[] { "Special", "XsltApiV2.Var3Class", "true" })]
        [Variation("Semi-trusted App Domain: Special: Xsl stylesheet with document() function, Transform() should fail, Debug.", Pri = 1, Params = new object[] { "Special", "XsltApiV2.Var4Class", "true" })]
        [Variation("Semi-trusted App Domain: Special: Xsl stylesheet with import, Transform() should fail, Debug.", Pri = 1, Params = new object[] { "Special", "XsltApiV2.Var5Class", "true" })]
        [Variation("Semi-trusted App Domain: Special: Xml document with DTD external entity, should fail, Debug.", Pri = 1, Params = new object[] { "Special", "XsltApiV2.Var6Class", "true" })]
        [Variation("Semi-trusted App Domain: LocalIntranet : Xsl stylesheet with script block, Transform() should fail, Debug.", Pri = 1, Params = new object[] { "LocalIntranet", "XsltApiV2.Var3Class", "true" })]
        [Variation("Semi-trusted App Domain: LocalIntranet : Xsl stylesheet with document() function, Transform() should fail, Debug.", Pri = 1, Params = new object[] { "LocalIntranet", "XsltApiV2.Var4Class", "true" })]
        [Variation("Semi-trusted App Domain: LocalIntranet : Xsl stylesheet with import, Transform() should fail, Debug.", Pri = 1, Params = new object[] { "LocalIntranet", "XsltApiV2.Var5Class", "true" })]
        [Variation("Semi-trusted App Domain: LocalIntranet : Xml document with DTD external entity, should fail, Debug.", Pri = 1, Params = new object[] { "LocalIntranet", "XsltApiV2.Var6Class", "true" })]
        [Variation("Semi-trusted App Domain: Internet : Xsl stylesheet with script block, Transform() should fail, Debug.", Pri = 1, Params = new object[] { "Internet", "XsltApiV2.Var3Class", "true" })]
        [Variation("Semi-trusted App Domain: Internet : Xsl stylesheet with document() function, Transform() should fail, Debug.", Pri = 1, Params = new object[] { "Internet", "XsltApiV2.Var4Class", "true" })]
        [Variation("Semi-trusted App Domain: Internet : Xsl stylesheet with import, Transform() should fail, Debug.", Pri = 1, Params = new object[] { "Internet", "XsltApiV2.Var5Class", "true" })]
        [Variation("Semi-trusted App Domain: Internet : Xml document with DTD external entity, should fail, Debug.", Pri = 1, Params = new object[] { "Internet", "XsltApiV2.Var6Class", "true" })]

        // Var2() For variations expected to fail due to security exception
        public int Var2()
        {
            if (_isInProc) return TEST_SKIPPED;
            var retVal = false;
            var domain = GetSemiTrustedAppDomainForVariation(CurVariation.Params[0].ToString(), CurVariation.Params[1].ToString());
            try
            {
                retVal = domain.badPlugin.Run(_standardTests, Boolean.Parse(CurVariation.Params[2].ToString()));
            }
            catch (SecurityException se)
            {
                var sb = new StringBuilder();
                MyAppDomain.AppendError(se, sb);
                Console.WriteLine(sb.ToString());
                retVal = true;
            }
            catch (XsltException xle)
            {
                var sb = new StringBuilder();
                if (xle.InnerException != null && xle.InnerException.GetType().Equals(typeof(SecurityException)))
                {
                    MyAppDomain.AppendError(xle.InnerException, sb);
                    Console.WriteLine(sb.ToString());
                    retVal = true;
                }
            }
            if (retVal == true) return TEST_PASS;
            else return TEST_FAIL;
        }

        //XmlWriter should be able to write a document with no entities to an existing Stream, TextWriter, XmlWriter
        [Variation("Semi-trusted: XmlWriter writes a document to an existing XmlWriter", Pri = 1)]
        public int Var3()
        {
            var permSet = new PermissionSet(PermissionState.None);
            permSet.PermitOnly();
            var writer = new XmlTextWriter(Console.Out);
            var xmlFoo = "<?xml version=\"1.0\"?><doc>foo</doc>";
            var xml = new XmlTextReader(new StringReader(xmlFoo), new NameTable());
            writer.WriteNode(xml, false);
            return TEST_PASS;
        }

        //XmlReader should be able to read a document with no entities from an existing Stream, TextReader, XmlReader
        [Variation("Semi-trusted: XmlReader reads a document with no entities from an existing Stream, TextReader", Pri = 1)]
        public int Var4()
        {
            var permSet = new PermissionSet(PermissionState.None);
            permSet.PermitOnly();
            var xmlFoo = "<?xml version=\"1.0\"?><doc>foo</doc>";
            var xml = new XmlTextReader(new StringReader(xmlFoo), new NameTable());
            return TEST_PASS;
        }

        //XmlDocument should be able to read an xml document with no DTDs from an existing Stream, TextReader, XmlReader
        [Variation("Semi-trusted: XmlDocument should be able to read an xml document with no DTDs from a Stream, TextReader", Pri = 1)]
        public int Var5()
        {
            var permSet = new PermissionSet(PermissionState.None);
            permSet.PermitOnly();
            var xmlFoo = "<?xml version=\"1.0\"?><doc>foo</doc>";
            var xml = new XmlDocument();
            xml.Load(new StringReader(xmlFoo));
            return TEST_PASS;
        }

        //XmlDocument should be able to write an xml document with no DTDs to an existing Stream, TextWrtier, or XmlWriter
        [Variation("Semi-trusted: XmlDocument should be able to write an xml document with no DTDs to an existing XmlWriter", Pri = 1)]
        public int Var6()
        {
            var permSet = new PermissionSet(PermissionState.None);
            permSet.PermitOnly();
            var xmlFoo = "<?xml version=\"1.0\"?><doc>foo</doc>";
            var xml = new XmlTextReader(new StringReader(xmlFoo), new NameTable());
            var writer = new XmlTextWriter(Console.Out);
            var xmlDoc = new XmlDocument();
            xmlDoc.WriteTo(writer);
            return TEST_PASS;
        }

        //XmlDocument should be able to write an xml document with no DTDs to an existing Stream, TextWrtier, or XmlWriter
        [Variation("Semi-trusted: XmlDocument - external entities (DTD) should fail in lower trusted scenarios", Pri = 1)]
        public int Var7()
        {
            var permSet = new PermissionSet(PermissionState.None);
            permSet.PermitOnly();
            var xmlFoo = "<!-- key-dtd.xml --><!DOCTYPE orders [<!ELEMENT orders (#PCDATA | order)*> " +
                            "<!ELEMENT order EMPTY> <!ATTLIST order  id CDATA #IMPLIED defattr CDATA #FIXED \"val\"> " +
                            " <!ENTITY external-orders SYSTEM \"./inc/external-orders.ent\"> ]> " +
                            "<orders><order id=\"ORD1000\"/> &external-orders; <order id=\"ORD1004\"/> </orders>";
            try
            {
                var xml = new XmlDocument { XmlResolver = new XmlUrlResolver() };
                xml.Load(new StringReader(xmlFoo));
            }
            catch (SecurityException)
            {
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        //XmlDocument should be able to write an xml document with no DTDs to an existing Stream, TextWrtier, or XmlWriter
        [Variation("Semi-trusted: XmlDocument - DTD with no external entities are OK in lower trusted scenarios", Pri = 1)]
        public int Var8()
        {
            var permSet = new PermissionSet(PermissionState.None);
            permSet.PermitOnly();
            var xmlFoo = "<!-- key-dtd.xml --><!DOCTYPE orders [<!ELEMENT orders (#PCDATA | order)*> " +
                            "<!ELEMENT order EMPTY> <!ATTLIST order  id CDATA #IMPLIED defattr CDATA #FIXED \"val\"> " +
                            "  ]> " +
                            "<orders><order id=\"ORD1000\"/> <order id=\"ORD1004\"/> </orders>";
            var xml = new XmlDocument();
            xml.Load(new StringReader(xmlFoo));
            return TEST_PASS;
        }

        [Variation("Semi-trusted: Transform() with xsl from file loaded before PermitOnly", Pri = 1, Param = "false")]
        [Variation("Semi-trusted: Transform() with xsl from file loaded before PermitOnly", Pri = 1, Param = "true")]
        public int Var9()
        {
            var xmlFoo = "<?xml version=\"1.0\"?><doc>foo</doc>";
            var xslt = new XslCompiledTransform(Boolean.Parse(CurVariation.Param.ToString()));
            xslt.Load(FullFilePath("identity.xsl"));

            var permSet = new PermissionSet(PermissionState.None);
            permSet.PermitOnly();

            var xml = new XmlDocument();
            xml.Load(new StringReader(xmlFoo));
            xslt.Transform(xml, null, Console.Out);
            return TEST_PASS;
        }

        [Variation("Semi-trusted: XsltArgumentList", Pri = 1, Param = "false")]
        [Variation("Semi-trusted: XsltArgumentList", Pri = 1, Param = "true")]
        public int Var10()
        {
            var xslBar = "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\"> " +
                           "   <xsl:param name=\"param1\" select=\"'global-param1-default'\" /><!-- 'global-param1-arg' will be given --> " +
                           "   <xsl:param name=\"param2\" select=\"'global-param2-default'\" /><!-- this parameter won't be specified --> " +
                           "   <xsl:template match=\"/\"> " +
                           "   <out> " +
                           "     <xsl:call-template name=\"Test\"> " +
                           "       <xsl:with-param name=\"param1\" select=\"'local-param1-arg'\" /> " +
                           "       <xsl:with-param name=\"param2\" select=\"'local-param2-arg'\" /> " +
                           "     </xsl:call-template> " +
                           "   </out> " +
                           "   </xsl:template> " +
                           "   <xsl:template name=\"Test\"> " +
                           "     <xsl:param name=\"param1\" select=\"'local-param1-default'\" /> " +
                           "     <xsl:param name=\"param2\" select=\"'local-param2-default'\" /> " +
                           " <xsl:text> " +
                           " param1 (correct answer is 'local-param1-arg'): </xsl:text><xsl:value-of select=\"$param1\" /><xsl:text> " +
                           " param2 (correct answer is 'local-param2-arg'): </xsl:text><xsl:value-of select=\"$param2\" /><xsl:text> " +
                           " </xsl:text> " +
                           "   </xsl:template> " +
                           " </xsl:stylesheet>";
            var xmlFoo = "<?xml version=\"1.0\"?><doc>foo</doc>";

            var permSet = new PermissionSet(PermissionState.None);
            permSet.PermitOnly();

            var xslt = new XslCompiledTransform(Boolean.Parse(CurVariation.Param.ToString()));
            var xml = new XmlDocument();
            xml.Load(new StringReader(xmlFoo));
            var argList = new XsltArgumentList();
            argList.AddParam("param1", string.Empty, "global-param1-arg");
            var trTemp = new XmlTextReader(new StringReader(xslBar));
            xslt.Load(trTemp, XsltSettings.TrustedXslt, null);
            xslt.Transform(xml, null, Console.Out);
            return TEST_PASS;
        }

        [Variation("Semi-trusted: 415650 Regression Test, XslCompiledTransform retail", Pri = 1, Param = "false")]
        [Variation("Semi-trusted: 415650 Regression Test, XslCompiledTransform retail", Pri = 1, Param = "true")]
        public int Var11()
        {
            new PermissionSet(PermissionState.None).PermitOnly();
            new XslCompiledTransform(Boolean.Parse(CurVariation.Param.ToString())).Load(XmlReader.Create(new StringReader("<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'/>")));
            return TEST_PASS;
        }

        private MyAppDomain GetSemiTrustedAppDomainForVariation(string permissionSetName, string varName)
        {
            var plugin = typeof(MyAppDomain).Assembly.GetName();
            var name = varName;

            var policyLevel = PolicyLevel.CreateAppDomainLevel();

            PermissionSet partialTrust;
            if (permissionSetName.Equals("Special"))
            {
                partialTrust = new PermissionSet(PermissionState.None);
                partialTrust.AddPermission(new FileIOPermission(FileIOPermissionAccess.AllAccess, System.Environment.CurrentDirectory + "\\XslCompTransformApiTests.dll"));
                partialTrust.AddPermission(new SecurityPermission(SecurityPermissionFlag.Execution));
            }
            else
                partialTrust = policyLevel.GetNamedPermissionSet(permissionSetName);

            var domain = new MyAppDomain(plugin, name);
            domain.LoadDomain(partialTrust);

            return domain;
        }
    }

    public interface IXslAppDomPlugin
    {
        bool Run(string rootPath, bool debug);
    }

    // XslCompiledTransform Var1() - sanity case
    // Note: do not use CError inside this type. It causes an UnmanagedCode demand.
    public class Var1Class : MarshalByRefObject, IXslAppDomPlugin
    {
        public Var1Class()
        {
        }

        public bool Run(string rootPath, bool debug)
        {
            try
            {
                new XslCompiledTransform(debug).Load((IXPathNavigable)null);
            }
            catch (ArgumentNullException)
            {
                return true;
            }
            return false;
        }
    }

    // vanilla stylesheet no scripts no import no document() - should tx successfully with 0 permissions
    // Note: do not use CError inside this type. It causes an UnmanagedCode demand.
    public class Var2Class : MarshalByRefObject, IXslAppDomPlugin
    {
        public Var2Class()
        {
        }

        public bool Run(string rootPath, bool debug)
        {
            var Identityxsl = "<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"><xsl:template match=\"/\"><xsl:copy-of select=\".\"/></xsl:template></xsl:stylesheet>";
            var xmlFoo = "<foo />";
            var xslt = new XslCompiledTransform(debug);
            var xsl = new XmlTextReader(new StringReader(Identityxsl), new NameTable());
            var xml = new XmlTextReader(new StringReader(xmlFoo), new NameTable());
            xslt.Load(xsl);
            var sw = new StringWriter();
            xslt.Transform(xml, (XsltArgumentList)null, sw);
            return true;
        }
    }

    // stylesheet with script so ensure scripts dont work
    // Note: do not use CError inside this type. It causes an UnmanagedCode demand.
    public class Var3Class : MarshalByRefObject, IXslAppDomPlugin
    {
        public Var3Class()
        {
        }

        public bool Run(string rootPath, bool debug)
        {
            var xslBar = "<xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version = \"1.0\" xmlns:msxsl=\"urn:schemas-microsoft-com:xslt\" xmlns:user=\"http://mycompany.com/mynamespace\">" +
                            "  <msxsl:script language=\"JScript\" implements-prefix=\"user\">function xml(nodelist) {  return 'true'; } </msxsl:script>" +
                            "    <xsl:template match=\"/\"> " +
                            "     <xsl:value-of select=\"user:xml(.)\"/>b " +
                            "    <xsl:apply-templates/> " +
                            "    </xsl:template></xsl:stylesheet>";
            var xmlFoo = "<foo />";
            var xslt = new XslCompiledTransform(debug);
            var xsl = new XmlTextReader(new StringReader(xslBar), new NameTable());
            var xml = new XmlTextReader(new StringReader(xmlFoo), new NameTable());

            // XsltSettings.EnableDocumentFunction needed
            var settings = new XsltSettings(false, true);

            xslt.Load(xsl, settings, new XmlUrlResolver());

            var sw = new StringWriter();
            xslt.Transform(xml, (XsltArgumentList)null, sw);
            return true;
        }
    }

    // Note: do not use CError inside this type. It causes an UnmanagedCode demand.
    // stylesheet with document() dont work
    public class Var4Class : MarshalByRefObject, IXslAppDomPlugin
    {
        public Var4Class()
        {
        }

        public bool Run(string rootPath, bool debug)
        {
            var xslBar = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><xsl:stylesheet xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" version=\"1.0\"> " +
                            " <xsl:template match=\"doc\"><out><xsl:copy-of select=\"document(a)//body\"/></out></xsl:template></xsl:stylesheet>";
            var xmlFoo = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><doc><a>" + rootPath + "XsltFunctions/mdocs04a.xml</a> <!-- Hello --> <y/><z/></doc>";
            var xslt = new XslCompiledTransform(debug);
            var xsl = new XmlTextReader(new StringReader(xslBar), new NameTable());
            var xml = new XmlTextReader(new StringReader(xmlFoo), new NameTable());

            // XsltSettings.EnableDocumentFunction needed
            var settings = new XsltSettings(true, false);

            xslt.Load(xsl, settings, new XmlUrlResolver());
            var sw = new StringWriter();
            xslt.Transform(xml, (XsltArgumentList)null, sw);
            return true;
        }
    }

    // stylesheet with import dont work
    public class Var5Class : MarshalByRefObject, IXslAppDomPlugin
    {
        public Var5Class()
        {
        }

        public bool Run(string rootPath, bool debug)
        {
            var xslBar = "<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\"><xsl:import href=\"" + rootPath + "Import/10000a.xsl\"/><xsl:output method=\"text\" omit-xml-declaration=\"yes\"/><xsl:template match=\"/\"><xsl:value-of select=\"'Hello from 10000'\"/>,<xsl:call-template name=\"a10000\"/></xsl:template></xsl:stylesheet>";
            var xmlFoo = "<?xml version=\"1.0\" encoding=\"ISO-8859-1\"?><books><book /></books>";
            var xslt = new XslCompiledTransform(debug);
            var xsl = new XmlTextReader(new StringReader(xslBar), new NameTable());
            var xml = new XmlTextReader(new StringReader(xmlFoo), new NameTable());

            xslt.Load(xsl);
            var sw = new StringWriter();
            xslt.Transform(xml, (XsltArgumentList)null, sw);
            return true;
        }
    }

    public class Var6Class : MarshalByRefObject, IXslAppDomPlugin
    {
        public Var6Class()
        {
        }

        public bool Run(string rootPath, bool debug)
        {
            var xmlFoo = "<!-- key-dtd.xml --><!DOCTYPE orders [<!ELEMENT orders (#PCDATA | order)*> " +
                            "<!ELEMENT order EMPTY> <!ATTLIST order  id CDATA #IMPLIED defattr CDATA #FIXED \"val\"> " +
                            " <!ENTITY external-orders SYSTEM \"" + rootPath + "BVTs/inc/external-orders.ent\"> ]> " +
                            "<orders><order id=\"ORD1000\"/> &external-orders; <order id=\"ORD1004\"/> </orders>";
            var xml = new XmlDocument();
            xml.Load(new StringReader(xmlFoo));
            return true;
        }
    }

    public class MyAppDomain
    {
        public AppDomain domain;
        public IXslAppDomPlugin badPlugin;
        private AssemblyName plugin;
        private string typeName;

        public MyAppDomain(AssemblyName name, string type)
        {
            this.plugin = name;
            this.typeName = type;
        }

        public void LoadDomain(PermissionSet permSet)
        {
            var info = new AppDomainSetup();
            var baseUri = new Uri(typeof(MyAppDomain).Assembly.Location);
            baseUri = new Uri(baseUri, ".");
            var basePath = baseUri.GetComponents(UriComponents.AbsoluteUri, UriFormat.SafeUnescaped);
            info.ApplicationBase = basePath;
            info.ApplicationName = "My Domain";
            info.PrivateBinPath = baseUri.LocalPath;
            info.PrivateBinPathProbe = ".;xml";
            info.DynamicBase = basePath;

            try
            {
                // Create app domain with minimum permissions then extend the policy.
                this.domain = AppDomain.CreateDomain("My Domain", null, info,
                    permSet, new StrongName[] { GetStrongName(typeof(MyAppDomain).Assembly) });

                this.badPlugin = (IXslAppDomPlugin)domain.CreateInstanceAndUnwrap(plugin.FullName, typeName);
            }
            finally
            {
                //current.AssemblyResolve -= assemblyResolver;
            }
        }

        private StrongName GetStrongName(Assembly a)
        {
            var name = a.GetName();
            var publicToken = name.GetPublicKey();
            return new StrongName(new StrongNamePublicKeyBlob(publicToken), name.Name, name.Version);
        }

        // Utility method to check permissions when a call fails due to security exception
        public static void AppendError(Exception e, StringBuilder sb)
        {
            var name = e.GetType().Name;
            sb.Append(name);
            sb.Append("\r\n");
            sb.Append(new string('-', name.Length));
            sb.Append("\r\n");
            sb.Append(e.Message);
            sb.Append("\r\n");

            if (e is System.Security.SecurityException)
            {
                var se = (System.Security.SecurityException)e;
                sb.Append("\r\n" + "SECURITY EXCEPTION" + "\r\n");
                if (se.FirstPermissionThatFailed != null)
                {
                    sb.Append("\r\nFirstPermissionThatFailed\r\n");
                    sb.Append("\r\n");
                    sb.Append(se.FirstPermissionThatFailed.ToString());
                    sb.Append("\r\n");
                }
                if (se.Demanded != null)
                {
                    sb.Append("\r\nDemanded\r\n");
                    sb.Append("\r\n");
                    sb.Append(se.Demanded.ToString());
                    sb.Append("\r\n");
                }
                if (se.GrantedSet != null)
                {
                    sb.Append("\r\nGrantedSet\r\n");
                    sb.Append("\r\n");
                    sb.Append(se.GrantedSet);
                    sb.Append("\r\n");
                }
            }
            sb.Append("\r\n");
        }
    }
}