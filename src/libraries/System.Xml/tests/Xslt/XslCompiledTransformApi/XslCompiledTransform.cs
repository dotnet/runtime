using OLEDB.Test.ModuleCore;
using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Security;
using System.Security.Policy;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Xsl;
using XmlCoreTest.Common;

namespace XsltApiV2
{
    public class ReflectionTestCaseBase : XsltApiTestCaseBase
    {
        //protected static readonly MethodInfo methCompileToType = GetStaticMethod(typeof(XslCompiledTransform), "CompileToType");

        public static MethodInfo GetInstanceMethod(Type type, string methName)
        {
            MethodInfo methInfo = type.GetMethod(methName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            Debug.Assert(methInfo != null, "Instance method " + type.Name + "." + methName + " not found");
            return methInfo;
        }

        public static MethodInfo GetStaticMethod(Type type, string methName)
        {
            MethodInfo methInfo = type.GetMethod(methName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
            Debug.Assert(methInfo != null, "Static method " + type.Name + "." + methName + " not found");
            return methInfo;
        }

        protected static CompilerErrorCollection WCompileToType(
            XmlReader stylesheet,
            XsltSettings settings,
            XmlResolver stylesheetResolver,
            bool debug,
            TypeBuilder typeBuilder,
            string scriptAssemblyPath)
        {
            return XslCompiledTransform.CompileToType(stylesheet, settings, stylesheetResolver, debug, typeBuilder, scriptAssemblyPath);
            //return (CompilerErrorCollection)methCompileToType.Invoke(/*this:*/null,
            //  new object[] { stylesheet, settings, stylesheetResolver, debug, typeBuilder, scriptAssemblyPath });
        }

        protected String scriptTestPath = null;

        protected String ScriptTestPath
        {
            get
            {
                if (scriptTestPath == null) //not thread safe
                {
                    scriptTestPath = _standardTests;

                    if (!scriptTestPath.EndsWith(Path.DirectorySeparatorChar.ToString()))
                        scriptTestPath += Path.DirectorySeparatorChar;

                    scriptTestPath += "Scripting\\";
                }

                return scriptTestPath;
            }
        }

        protected TypeBuilder ATypeBuilder
        {
            get
            {
                AppDomain cd = System.Threading.Thread.GetDomain();
                AssemblyName an = new AssemblyName();
                an.Name = "HelloClass";

                AssemblyBuilder ab = cd.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);
                ModuleBuilder mb = ab.DefineDynamicModule("HelloModule", "HelloModule.dll", true);
                TypeBuilder tb = mb.DefineType("Hello", TypeAttributes.Class | TypeAttributes.Public);
                return tb;
            }
        }

        protected MethodInfo AMethodInfo
        {
            get
            {
                String asmPath = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\bftBaseLine.dll");
                String type = "bftBaseLine";

                Assembly asm = Assembly.LoadFrom(asmPath);
                Type t = asm.GetType(type);

                return GetStaticMethod(t, "Execute");
            }
        }

        protected void WLoad(XslCompiledTransform instance, MethodInfo meth, Byte[] bytes, Type[] types)
        {
            instance.Load(meth, bytes, types);
            /*
            Type[] paramTypes = new Type[3] { typeof(MethodInfo), typeof(Byte[]), typeof(Type[]) };

            MethodInfo m = typeof(XslCompiledTransform).GetMethod("Load",
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
                null,
                paramTypes,
                null);

            m.Invoke(instance, new Object[] { meth, bytes, types });
            */
        }

        protected void TestWLoad(XslCompiledTransform xslt, String asmPath, String type)
        {
            Assembly asm = Assembly.LoadFrom(asmPath);
            Type t = asm.GetType(type);

            MethodInfo meth = GetStaticMethod(t, "Execute");
            Byte[] staticData = (Byte[])t.GetField("staticData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(t);
            Type[] ebTypes = (Type[])t.GetField("ebTypes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(t);

            WLoad(xslt, meth, staticData, ebTypes);
        }
    }

    [TestCase(Name = "CompileToType tests", Desc = "This testcase tests XslCompiledTransform private CompileToType method via Reflection. Same method is also exercised by xsltc.exe")]
    public class CCompileToTypeTest : ReflectionTestCaseBase
    {
        [Variation("CompileToType(XmlReader = null)", Pri = 1)]
        public int Var1()
        {
            try
            {
                WCompileToType((XmlReader)null, XsltSettings.Default, null, false, ATypeBuilder, String.Empty);
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());

                if (e is ArgumentNullException || e.InnerException is ArgumentNullException)
                    return TEST_PASS;

                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("CompileToType(TypeBuilder= null)", Pri = 1)]
        public int Var2()
        {
            try
            {
                WCompileToType(XmlReader.Create(FullFilePath("identity.xsl")), XsltSettings.Default, null, false, null, String.Empty);
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());

                if (e is ArgumentNullException || e.InnerException is ArgumentNullException)
                    return TEST_PASS;

                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("CompileToType(AsmPath= No Extension file name)", Pri = 1)]
        public int Var3()
        {
            AppDomain cd = System.Threading.Thread.GetDomain();
            AssemblyName an = new AssemblyName();
            an.Name = "HelloClass";

            AssemblyBuilder ab = cd.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder mb = ab.DefineDynamicModule("HelloModule", "HelloModule.dll", true);
            TypeBuilder tb = mb.DefineType("Hello", TypeAttributes.Class | TypeAttributes.Public);

            try
            {
                String xslPath = ScriptTestPath + "Scripting28.xsl";
                CompilerErrorCollection errors;
                using (XmlReader reader = XmlReader.Create(xslPath))
                {
                    errors = WCompileToType(reader, XsltSettings.TrustedXslt, null, false, tb, "ScName");
                }

                // Print errors and warnings
                bool hasError = false;
                foreach (CompilerError error in errors)
                {
                    CError.WriteLine(error);
                    hasError = true;
                }

                if (hasError) return TEST_FAIL;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                return TEST_FAIL;
            }

            return TEST_PASS;
        }

        [Variation("CompileToType(Valid case with scripts)", Pri = 1)]
        public int Var4()
        {
            AppDomain cd = System.Threading.Thread.GetDomain();
            AssemblyName an = new AssemblyName();
            an.Name = "HelloClass4";

            AssemblyBuilder ab = cd.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder mb = ab.DefineDynamicModule("HelloModule4", "HelloModule4.dll", true);
            TypeBuilder tb = mb.DefineType("Hello4", TypeAttributes.Class | TypeAttributes.Public);

            try
            {
                String xslPath = ScriptTestPath + "Scripting28.xsl";
                CompilerErrorCollection errors;
                using (XmlReader reader = XmlReader.Create(xslPath))
                {
                    errors = WCompileToType(reader, XsltSettings.TrustedXslt, null, false, tb, "SomeAsm4.dll");
                }

                // Print errors and warnings
                bool hasError = false;
                foreach (CompilerError error in errors)
                {
                    CError.WriteLine(error);
                    hasError = true;
                }

                if (hasError) return TEST_FAIL;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                return TEST_FAIL;
            }

            return TEST_PASS;
        }

        // Q>       In CompileToType when scriptAssemblyPath is null, should we really throw ArgumentNullExc
        //          if the stylesheet does not have any scripts (but the settings are enabled)?
        //
        // Anton>   Sergey and I think it’s acceptable behavior. Implementing the other way will require extra
        //          code churn in 3 files, which we tried to avoid.

        [Variation("CompileToType(ScriptPath = null, sytlesheet with no scripts)", Pri = 1)]
        public int Var5()
        {
            AppDomain cd = System.Threading.Thread.GetDomain();
            AssemblyName an = new AssemblyName();
            an.Name = "HelloClass5";

            AssemblyBuilder ab = cd.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder mb = ab.DefineDynamicModule("HelloModule5", "HelloModule5.dll", true);
            TypeBuilder tb = mb.DefineType("Hello5", TypeAttributes.Class | TypeAttributes.Public);

            try
            {
                String xslPath = FullFilePath("identity.xsl");
                CompilerErrorCollection errors;
                using (XmlReader reader = XmlReader.Create(xslPath))
                {
                    errors = WCompileToType(reader, XsltSettings.TrustedXslt, null, false, tb, null);
                }

                // Print errors and warnings
                bool hasError = false;
                foreach (CompilerError error in errors)
                {
                    CError.WriteLine(error);
                    hasError = true;
                }

                if (hasError) return TEST_FAIL;
            }
            catch (ArgumentNullException e)
            {
                CError.WriteIgnore(e.ToString());
                return TEST_PASS;
            }

            return TEST_FAIL;
        }

        [Variation("CompileToType(ScriptPath = null, stylesheet with scripts)", Pri = 1)]
        public int Var6()
        {
            AppDomain cd = System.Threading.Thread.GetDomain();
            AssemblyName an = new AssemblyName();
            an.Name = "HelloClass6";

            AssemblyBuilder ab = cd.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder mb = ab.DefineDynamicModule("HelloModule6", "HelloModule6.dll", true);
            TypeBuilder tb = mb.DefineType("Hello6", TypeAttributes.Class | TypeAttributes.Public);

            try
            {
                String xslPath = ScriptTestPath + "Scripting28.xsl";
                CompilerErrorCollection errors;
                using (XmlReader reader = XmlReader.Create(xslPath))
                {
                    errors = WCompileToType(reader, XsltSettings.TrustedXslt, null, false, tb, null);
                }

                // Print errors and warnings
                bool hasError = false;
                foreach (CompilerError error in errors)
                {
                    CError.WriteLine(error);
                    hasError = true;
                }

                if (hasError) return TEST_FAIL;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                return TEST_PASS;
            }

            return TEST_FAIL;
        }

        [Variation("CompileToType(ScriptPath = null, XsltSettings = null)", Pri = 1)]
        public int Var7()
        {
            AppDomain cd = System.Threading.Thread.GetDomain();
            AssemblyName an = new AssemblyName();
            an.Name = "HelloClass7";

            AssemblyBuilder ab = cd.DefineDynamicAssembly(an, AssemblyBuilderAccess.RunAndSave);
            ModuleBuilder mb = ab.DefineDynamicModule("HelloModule7", "HelloModule7.dll", true);
            TypeBuilder tb = mb.DefineType("Hello7", TypeAttributes.Class | TypeAttributes.Public);

            try
            {
                CompilerErrorCollection errors;
                using (XmlReader reader = XmlReader.Create(FullFilePath("identity.xsl")))
                {
                    errors = WCompileToType(reader, null, null, false, tb, null);
                }

                // Print errors and warnings
                bool hasError = false;
                foreach (CompilerError error in errors)
                {
                    CError.WriteLine(error);
                    hasError = true;
                }

                if (!hasError) return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                return TEST_FAIL;
            }

            return TEST_FAIL;
        }
    }

    [TestCase(Name = "Load(MethodInfo, ByteArray, TypeArray) tests", Desc = "This testcase tests private Load method via Reflection. This method is used by sharepoint")]
    public class CLoadMethInfoTest : ReflectionTestCaseBase
    {
        [Variation("Load(MethodInfo = null, ByteArray, TypeArray)", Pri = 1)]
        public int Var1()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            try
            {
                WLoad(xslt, (MethodInfo)null, new Byte[5], new Type[5]);
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());

                if (e is ArgumentNullException || e.InnerException is ArgumentNullException)
                    return TEST_PASS;

                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Load(MethodInfo, ByteArray = null, TypeArray)", Pri = 1)]
        public int Var2()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            try
            {
                WLoad(xslt, AMethodInfo, null, new Type[5]);
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());

                if (e is ArgumentException || e.InnerException is ArgumentException)
                    return TEST_PASS;

                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Valid Load after error case Load(MethodInfo, ByteArray, TypeArray)", Pri = 1)]
        public int Var3()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            try
            {
                //error case
                WLoad(xslt, AMethodInfo, new Byte[5], null);
            }
            catch (Exception e)
            {
                /*
                 * Anton> If staticData array is malformed, you end up with weird exceptions.
                 * We didn’t expect this method to be public and didn’t implement any range/sanity
                 * checks in deserialization code path, besides CLR implicit range checks for array indices.
                 * You have found one such place, but there are dozens of them. Fixing all of them will cause
                 * code churn in many files and another round of [code review/test sign-off/approval] process.
                 * Right now it works according to the “Garbage In, Garbage Out” principle.
                 *
                 */
                CError.WriteIgnore(e.ToString());
            }

            String asmPath = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\bftBaseLine.dll");
            String type = "bftBaseLine";
            String xml = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\sft1.xml");
            TestWLoad(xslt, asmPath, type);
            xslt.Transform(xml, "errout.txt");
            return TEST_PASS;
        }

        [Variation("Multiple Loads Load(MethodInfo, ByteArray, TypeArray)", Pri = 1)]
        public int Var7()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();

            String asmPath = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\bftBaseLine.dll");
            String type = "bftBaseLine";
            String xml = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\sft1.xml");

            Assembly asm = Assembly.LoadFrom(asmPath);
            Type t = asm.GetType(type);

            MethodInfo meth = GetStaticMethod(t, "Execute");
            Byte[] staticData = (Byte[])t.GetField("staticData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(t);
            Type[] ebTypes = (Type[])t.GetField("ebTypes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(t);

            asmPath = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\Scripting28.dll");
            type = "Scripting28";

            asm = Assembly.LoadFrom(asmPath);
            t = asm.GetType(type);

            MethodInfo meth2 = GetStaticMethod(t, "Execute");
            Byte[] staticData2 = (Byte[])t.GetField("staticData", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(t);
            Type[] ebTypes2 = (Type[])t.GetField("ebTypes", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static).GetValue(t);

            for (int i = 0; i < 100; i++)
            {
                WLoad(xslt, meth, staticData, ebTypes);
                WLoad(xslt, meth2, staticData2, ebTypes2);
                xslt.Transform(xml, "out.txt");
                xslt.Transform(xml, "out1.txt");
            }

            return TEST_PASS;
        }

        [Variation("Load(MethodInfo, ByteArray, TypeArray) simple assembly", Pri = 1)]
        public int Var4()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            String asmPath = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\bftBaseLine.dll");
            String type = "bftBaseLine";
            String xml = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\sft1.xml");

            try
            {
                TestWLoad(xslt, asmPath, type);
                xslt.Transform(xml, "out.txt");
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
            }
            return TEST_FAIL;
        }

        [Variation("Load(MethodInfo, ByteArray, TypeArray) assembly with scripts", Pri = 1)]
        public int Var5()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            String asmPath = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\Scripting28.dll");
            String type = "Scripting28";
            String xml = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\sft1.xml");

            try
            {
                TestWLoad(xslt, asmPath, type);
                xslt.Transform(xml, "out.txt");
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
            }
            return TEST_FAIL;
        }

        [Variation("Load(MethodInfo, ByteArray, TypeArray) old xsltc assembly", Pri = 1)]
        public int Var6()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            String asmPath = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\CTT1.dll");
            String type = "CCT1";
            String xml = Path.Combine(FilePathUtil.GetTestDataPath(), @"xsltc\precompiled\sft1.xml");

            try
            {
                TestWLoad(xslt, asmPath, type);
                xslt.Transform(xml, "out.txt");
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
            }
            return TEST_FAIL;
        }
    }

    [TestCase(Name = "Null argument tests", Desc = "This testcase passes NULL arguments to all XslCompiledTransform methods")]
    public class CNullArgumentTest : XsltApiTestCaseBase
    {
        [Variation("Load(string = null)", Pri = 1)]
        public int Var0()
        {
            try
            {
                new XslCompiledTransform().Load((string)null);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Load(IXPathNavigable = null)", Pri = 1)]
        public int Var1()
        {
            try
            {
                new XslCompiledTransform().Load((IXPathNavigable)null);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Load(XmlReader = null)", Pri = 1)]
        public int Var2()
        {
            try
            {
                new XslCompiledTransform().Load((XmlReader)null);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Load(IXPathNavigable = null, XmlResolver = null)", Pri = 1)]
        public int Var3()
        {
            try
            {
                new XslCompiledTransform().Load((IXPathNavigable)null, XsltSettings.TrustedXslt, (XmlResolver)null);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Load(XmlReader = null, XmlResolver = null)", Pri = 1)]
        public int Var4()
        {
            try
            {
                new XslCompiledTransform().Load((XmlReader)null, XsltSettings.TrustedXslt, (XmlResolver)null);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Load(IXPathNavigable = null, XmlResolver = null)", Pri = 1)]
        public int Var5()
        {
            try
            {
                new XslCompiledTransform().Load((IXPathNavigable)null, XsltSettings.TrustedXslt, (XmlResolver)null);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Load(XmlReader = null, XmlResolver = null)", Pri = 1)]
        public int Var6()
        {
            try
            {
                new XslCompiledTransform().Load((XmlReader)null, XsltSettings.TrustedXslt, (XmlResolver)null);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Transform(IXPathNavigable = null, XsltArgumentList = null)", Pri = 1)]
        public int Var7()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                StringWriter sw = new StringWriter();
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, sw);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Transform(IXPathNavigable = null, XsltArgumentList = null)", Pri = 1)]
        public int Var8()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                StringWriter sw = new StringWriter();
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, sw);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Transform(IXPathNavigable = null, XsltArgumentList = null, TextWriter = null)", Pri = 1)]
        public int Var9()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, (TextWriter)null);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Transform(IXPathNavigable = null, XsltArgumentList = null, TextWriter = null)", Pri = 1)]
        public int Var10()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, (TextWriter)null);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Transform(IXPathNavigable = null, XsltArgumentList = null, Stream = null)", Pri = 1)]
        public int Var11()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, (Stream)null);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Transform(IXPathNavigable = null, XsltArgumentList = null, Stream = null)", Pri = 1)]
        public int Var12()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, (Stream)null);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Transform(IXPathNavigable = null, XsltArgumentList = null, XmlWriter = null)", Pri = 1)]
        public int Var13()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, (XmlWriter)null);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }

        [Variation("Transform(IXPathNavigable = null, XsltArgumentList = null, XmlWriter = null)", Pri = 1)]
        public int Var14()
        {
            try
            {
                XslCompiledTransform xslt = new XslCompiledTransform();
                xslt.Load(FullFilePath("identity.xsl"));
                xslt.Transform((IXPathNavigable)null, (XsltArgumentList)null, (XmlWriter)null);
            }
            catch (ArgumentNullException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteIgnore(e.ToString());
                CError.WriteLine("Did not throw ArgumentNullException");
            }
            return TEST_FAIL;
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Resolver - Integrity              */
    /***********************************************************/

    [TestCase(Name = "XslCompiledTransform.XmlResolver : Reader, Reader", Desc = "READER,READER")]
    [TestCase(Name = "XslCompiledTransform.XmlResolver : Reader, Stream", Desc = "READER,STREAM")]
    [TestCase(Name = "XslCompiledTransform.XmlResolver : Reader, Writer", Desc = "READER,WRITER")]
    [TestCase(Name = "XslCompiledTransform.XmlResolver : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    [TestCase(Name = "XslCompiledTransform.XmlResolver : URI, Reader", Desc = "URI,READER")]
    [TestCase(Name = "XslCompiledTransform.XmlResolver : URI, Stream", Desc = "URI,STREAM")]
    [TestCase(Name = "XslCompiledTransform.XmlResolver : URI, Writer", Desc = "URI,WRITER")]
    [TestCase(Name = "XslCompiledTransform.XmlResolver : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    [TestCase(Name = "XslCompiledTransform.XmlResolver : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    [TestCase(Name = "XslCompiledTransform.XmlResolver : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    [TestCase(Name = "XslCompiledTransform.XmlResolver : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    [TestCase(Name = "XslCompiledTransform.XmlResolver : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CXmlResolverTest : XsltApiTestCaseBase
    {
        [Variation(id = 1, Desc = "Set XmlResolver property to null, load style sheet with import/include, should not affect transform")]
        public int XmlResolver1()
        {
            try
            {
                LoadXSL("xmlResolver_main.xsl", null);
                return TEST_FAIL;
            }
            catch (XsltException e1)
            {
                CError.WriteLine(e1);
                return TEST_PASS;
            }
            catch (ArgumentNullException e2)
            {
                CError.WriteLine(e2);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                    return TEST_FAIL;
                }
            }
        }

        [Variation(id = 2, Desc = "Set XmlResolver property to null, load style sheet with document function, should not resolve during load")]
        public int XmlResolver2()
        {
            try
            {
                LoadXSL("xmlResolver_main.xsl", null);
                CError.WriteLine("No exception was thrown");
                return TEST_FAIL;
            }
            catch (XsltException e1)
            {
                CError.WriteLine(e1);
                return TEST_PASS;
            }
            catch (ArgumentNullException e2)
            {
                CError.WriteLine(e2);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                    return TEST_FAIL;
                }
            }
        }

        [Variation(id = 3, Desc = "Default XmlResolver, load style sheet with document function, should resolve during transform", Pri = 1, Param = "DefaultResolver.txt")]
        public int XmlResolver3()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();

            if (LoadXSL("xmlResolver_document_function.xsl") == TEST_PASS)
            {
                if ((Transform("fruits.xml") == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                    return TEST_PASS;
            }
            else
            {
                CError.WriteLine("Problem loading stylesheet with document function and default resolver!");
                return TEST_FAIL;
            }
            return TEST_FAIL;
        }

        [Variation(id = 4, Desc = "Set to null, then to proper cred., then transform with style sheet that requires cred.")]
        public int XmlResolver4()
        {
            if (LoadXSL("xmlResolver_cred.xsl") == TEST_PASS)
            {
                if ((Transform("fruits.xml") == TEST_PASS) && (CheckResult(377.8217373898) == TEST_PASS))
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("Failed to use XmlResolver property to resolve document function");
                    return TEST_FAIL;
                }
            }
            else
            {
                CError.WriteLine("Failed to load style sheet!");
                return TEST_FAIL;
            }
        }

        [Variation(id = 5, Desc = "Set to null, then to proper cred., then again to null, then transform with style sheet that requires cred., should not resolve document()", Param = "xmlResolver_cred.txt")]
        public int XmlResolver5()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();

            if (LoadXSL("xmlResolver_cred.xsl") == TEST_PASS)
            {
                if ((Transform("fruits.xml") == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("Should fail to resolve the main stylesheet with null resolver!");
                    return TEST_FAIL;
                }
            }
            else
            {
                CError.WriteLine("Failed to load style sheet!");
                return TEST_FAIL;
            }
        }

        /*
                [Variation("Set to NULL many times in a loop, then to proper cred.")]
                public int XmlResolver6()
                {
                    // Skip this test for Load(URI)
                    // Reason: When style sheet URI = Intranet zone, XmlSecureResolver does not resolve document function

                    if(LoadXSL("xmlResolver_cred.xsl") == TEST_PASS)
                    {
                        for(int i=0; i<100; i++)
                            xslt.XmlResolver = null;

                        for(int i=0; i<100; i++)
                            xslt.XmlResolver = GetDefaultCredResolver();

                        if ((Transform("fruits.xml") == TEST_PASS) && (CheckResult(377.8217373898) == TEST_PASS))
                            return TEST_PASS;
                        else
                        {
                            CError.WriteLine("Failed to use XmlResolver property to resolve document function");
                            return TEST_FAIL;
                        }
                    }
                    else
                    {
                        CError.WriteLine("Failed to load style sheet!");
                        return TEST_FAIL;
                    }
                }
        */

        [Variation(id = 7, Desc = "document() has absolute URI", Pri = 0)]
        public int XmlResolver7()
        {
            // copy file on the local machine (this is now done with createAPItestfiles.js, see Oasys scenario.)
            if (LoadXSL("xmlResolver_document_function_absolute_uri.xsl") == TEST_PASS)
            {
                if ((Transform("fruits.xml") == TEST_PASS) && (CheckResult(377.8217373898) == TEST_PASS))
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("Failed to resolve document function with absolute URI.");
                    return TEST_FAIL;
                }
            }
            else
            {
                CError.WriteLine("Failed to load style sheet!");
                return TEST_FAIL;
            }
        }

        [Variation(id = 8, Desc = "document() has file:// URI")]
        public int XmlResolver8()
        {
            if (LoadXSL("xmlResolver_document_function_file_uri.xsl") == TEST_PASS)
            {
                if ((Transform("fruits.xml") == TEST_PASS) && (CheckResult(377.8217373898) == TEST_PASS))
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("Failed to resolve document function with file:// URI.");
                    return TEST_FAIL;
                }
            }
            else
            {
                CError.WriteLine("Failed to load style sheet!");
                return TEST_FAIL;
            }
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Load - Integrity                  */
    /***********************************************************/

    [TestCase(Name = "XslCompiledTransform.Load() - Integrity : Reader, Reader", Desc = "READER,READER")]
    [TestCase(Name = "XslCompiledTransform.Load() - Integrity : Reader, Stream", Desc = "READER,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Load() - Integrity : Reader, Writer", Desc = "READER,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Load() - Integrity : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    [TestCase(Name = "XslCompiledTransform.Load() - Integrity : URI, Reader", Desc = "URI,READER")]
    [TestCase(Name = "XslCompiledTransform.Load() - Integrity : URI, Stream", Desc = "URI,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Load() - Integrity : URI, Writer", Desc = "URI,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Load() - Integrity : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    [TestCase(Name = "XslCompiledTransform.Load() - Integrity : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    [TestCase(Name = "XslCompiledTransform.Load() - Integrity : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Load() - Integrity : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Load() - Integrity : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CLoadTest : XsltApiTestCaseBase
    {
        [Variation(id = 1, Desc = "Call Load with null value")]
        public int LoadGeneric1()
        {
            try
            {
                LoadXSL(null);
            }
            catch (System.ArgumentException)
            {
                // System.Xml.XmlUrlResolver.ResolveUri(Uri baseUri, String relativeUri) throws System.ArgumentException here for null
                return TEST_PASS;
            }
            CError.WriteLine("Exception not generated for null parameter name");
            return TEST_FAIL;
        }

        [Variation(id = 2, Desc = "Load with valid, then invalid, then valid again")]
        public int LoadGeneric2()
        {
            if (LoadXSL("showParam.xsl") == TEST_PASS)
            {
                try
                {
                    LoadXSL("IDontExist.xsl");
                }
                catch (System.IO.FileNotFoundException)
                {
                    try
                    {
                        Transform("fruits.xml");
                    }
                    catch (System.InvalidOperationException e)
                    {
                        return CheckExpectedError(e, "System.xml", "Xslt_NoStylesheetLoaded", new string[] { "" });
                    }
                }
            }
            else
            {
                CError.WriteLine("Failed to load style sheet!");
                return TEST_FAIL;
            }
            return TEST_FAIL;
        }

        [Variation(id = 3, Desc = "Load an invalid, then a valid and transform", Param = "showParam.txt")]
        public int LoadGeneric3()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            try
            {
                LoadXSL("IDontExist.xsl");
            }
            catch (System.IO.FileNotFoundException)
            {
                if ((LoadXSL("ShowParam.xsl") == TEST_PASS) && (Transform("fruits.xml") == TEST_PASS)
                && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                    return TEST_PASS;
            }
            CError.WriteLine("Exception not generated for non-existent file name");
            return TEST_FAIL;
        }

        [Variation(id = 4, Desc = "Call several overloaded functions", Param = "showParam.txt")]
        public int LoadGeneric4()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            if (MyXslInputType() != XslInputType.Reader)
                LoadXSL("showParamLongName.xsl", XslInputType.Reader, new XmlUrlResolver());
            if (MyXslInputType() != XslInputType.URI)
                LoadXSL("showParamLongName.xsl", XslInputType.URI, new XmlUrlResolver());
            if (MyXslInputType() != XslInputType.Navigator)
                LoadXSL("showParamLongName.xsl", XslInputType.Navigator, new XmlUrlResolver());

            if ((LoadXSL("ShowParam.xsl") == TEST_FAIL) || (Transform("fruits.xml") == TEST_FAIL)
                    || (VerifyResult(Baseline, _strOutFile) == TEST_FAIL))
                return TEST_FAIL;

            if (MyXslInputType() != XslInputType.Navigator)
                LoadXSL("showParamLongName.xsl", XslInputType.Navigator, new XmlUrlResolver());
            if (MyXslInputType() != XslInputType.URI)
                LoadXSL("showParamLongName.xsl", XslInputType.URI, new XmlUrlResolver());
            if (MyXslInputType() != XslInputType.Reader)
                LoadXSL("showParamLongName.xsl", XslInputType.Reader, new XmlUrlResolver());

            if ((LoadXSL("ShowParam.xsl") == TEST_PASS) && (Transform("fruits.xml") == TEST_PASS)
                    && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;

            return TEST_FAIL;
        }

        [Variation(id = 5, Desc = "Call same overloaded Load() many times then transform", Param = "showParam.txt")]
        public int LoadGeneric5()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            for (int i = 0; i < 100; i++)
            {
                if (LoadXSL("showParam.xsl") != TEST_PASS)
                {
                    CError.WriteLine("Failed to load stylesheet showParam.xsl on the {0} attempt", i);
                    return TEST_FAIL;
                }
            }
            if ((LoadXSL("ShowParam.xsl") == TEST_PASS) && (Transform("fruits.xml") == TEST_PASS)
                && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;

            return TEST_FAIL;
        }

        [Variation(id = 6, Desc = "Call load with non-existing stylesheet")]
        public int LoadGeneric6()
        {
            try
            {
                LoadXSL("IDontExist.xsl");
            }
            catch (System.IO.FileNotFoundException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Exception not generated for non-existent file parameter name");
            return TEST_FAIL;
        }

        [Variation(id = 7, Desc = "Verify that style sheet is closed properly after Load - Shared Read Access")]
        public int LoadGeneric7()
        {
            FileStream s2;

            // check immediately after load and after transform
            if (LoadXSL("XmlResolver_main.xsl") == TEST_PASS)
            {
                s2 = new FileStream(FullFilePath("XmlResolver_main.xsl"), FileMode.Open, FileAccess.Read, FileShare.Read);
                s2.Close();
                if ((Transform("fruits.xml") == TEST_PASS) && (CheckResult(428.8541842246) == TEST_PASS))
                {
                    s2 = new FileStream(FullFilePath("XmlResolver_main.xsl"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    s2.Close();
                    return TEST_PASS;
                }
            }
            return TEST_FAIL;
        }

        /*
        [Variation(id =8, Desc ="Verify that style sheet is closed properly after Load - ReadWrite Access")]
        public int LoadGeneric8()
        {
            FileStream s2;

            // check immediately after load and after transform

            if(LoadXSL("XmlResolver_main.xsl") == TEST_PASS)
            {
                s2 = new FileStream(FullFilePath("XmlResolver_main.xsl"), FileMode.Open, FileAccess.ReadWrite);
                s2.Close();
                if((Transform("fruits.xml") == TEST_PASS) && (CheckResult(428.8541842246)== TEST_PASS))
                {
                    s2 = new FileStream(FullFilePath("XmlResolver_main.xsl"), FileMode.Open, FileAccess.ReadWrite);
                    s2.Close();
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Appeared to not close style sheet file properly after loading.");
            return TEST_FAIL;
        }
        */

        [Variation(id = 9, Desc = "Verify that included files are closed properly after Load - Read Access")]
        public int LoadGeneric9()
        {
            FileStream s2;

            // check immediately after load and after transform
            if (LoadXSL("XmlResolver_main.xsl") == TEST_PASS)
            {
                s2 = new FileStream(FullFilePath("XmlResolver_sub.xsl"), FileMode.Open, FileAccess.Read);
                s2.Close();
                if ((Transform("fruits.xml") == TEST_PASS) && (CheckResult(428.8541842246) == TEST_PASS))
                {
                    s2 = new FileStream(FullFilePath("XmlResolver_Include.xsl"), FileMode.Open, FileAccess.Read, FileShare.Read);
                    s2.Close();
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Appeared to not close file properly after loading.");
            return TEST_FAIL;
        }

        /*
        [Variation(id =10, Desc ="Verify that included files are closed properly after Load - ReadWrite Access")]
        public int LoadGeneric10()
        {
            FileStream s2;

            // check immediately after load and after transform
            if(LoadXSL("XmlResolver_main.xsl") == TEST_PASS)
            {
                s2 = new FileStream(FullFilePath("XmlResolver_sub.xsl"), FileMode.Open, FileAccess.ReadWrite);
                s2.Close();
                if((Transform("fruits.xml") == TEST_PASS) && (CheckResult(428.8541842246)== TEST_PASS))
                {
                    s2 = new FileStream(FullFilePath("XmlResolver_Include.xsl"), FileMode.Open, FileAccess.ReadWrite);
                    s2.Close();
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Appeared to not close file properly after loading.");
            return TEST_FAIL;
        }
        */

        [Variation(id = 11, Desc = "Load stylesheet with entity reference: Bug #68450 ")]
        public int LoadGeneric11()
        {
            if (MyDocType().ToString() == "DataDocument")
                // Skip the test for DataDocument
                return TEST_PASS;
            else
            {
                if (LoadXSL("books_entity_ref.xsl", XslInputType.Reader, new XmlUrlResolver()) != TEST_PASS)
                {
                    CError.WriteLine("Failed to load stylesheet books_entity_ref.xsl");
                    return TEST_FAIL;
                }
                if ((LoadXSL("books_entity_ref.xsl") == TEST_PASS) && (Transform("books_entity_ref.xml") == TEST_PASS)
                    && (CheckResult(371.4148215954) == TEST_PASS))
                    return TEST_PASS;
                return TEST_FAIL;
            }
        }

        [Variation(id = 12, Desc = "Load with invalid stylesheet and verify that file is closed properly")]
        public int LoadGeneric12()
        {
            Stream strmTemp;

            try
            {
                int i = LoadXSL("xslt_error.xsl");
            }
            catch (XsltException)
            {
                // Try to open the xsl file
                try
                {
                    strmTemp = new FileStream(FullFilePath("xslt_error.xsl"), FileMode.Open, FileAccess.Read);
                }
                catch (Exception ex)
                {
                    CError.WriteLine("Did not close stylesheet properly after load");
                    CError.WriteLine(ex);
                    return TEST_FAIL;
                }
                return TEST_PASS;
            }
            CError.WriteLine("Did not throw compile exception for stylesheet");
            return TEST_FAIL;
        }
    }

    /**************************************************************************/
    /*          XslCompiledTransform.Load(XmlResolver) - Integrity   */
    /**************************************************************************/

    [TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Reader, Reader", Desc = "READER,READER")]
    [TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Reader, Stream", Desc = "READER,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Reader, Writer", Desc = "READER,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    [TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    [TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    [TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : URI, Reader", Desc = "URI,READER")]
    [TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : URI, Stream", Desc = "URI,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : URI, Writer", Desc = "URI,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Load(XmlResolver) - Integrity : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    public class CLoadXmlResolverTest : XsltApiTestCaseBase
    {
        [Variation("Call Load with null source value and null resolver")]
        public int LoadGeneric1()
        {
            try
            {
                LoadXSL_Resolver(null, null);
            }
            catch (System.ArgumentNullException e)
            {
                CError.WriteLine(e);
                return TEST_PASS;
            }
            CError.WriteLine("Passing null argument should have thrown ArgumentNullException");
            return TEST_FAIL;
        }

        [Variation("Call Load with null source value and valid resolver")]
        public int LoadGeneric2()
        {
            try
            {
                LoadXSL_Resolver(null, new XmlUrlResolver());
            }
            catch (System.ArgumentNullException e)
            {
                CError.WriteLine(e);
                return TEST_PASS;
            }
            CError.WriteLine("Passing null stylesheet should have thrown ArgumentNullException");
            return TEST_FAIL;
        }

        [Variation("Call Load with null XmlResolver, style sheet does not have include/import, URI should throw ArgumentNullException and the rest shouldn't error", Param = "showParam.txt")]
        public int LoadGeneric3()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            try
            {
                LoadXSL_Resolver("ShowParam.xsl", null);
                Transform("fruits.xml");
                return VerifyResult(Baseline, _strOutFile);
            }
            catch (ArgumentNullException e)
            {
                CError.WriteLine(e);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                    return TEST_FAIL;
                }
            }
        }

        [Variation("Call Load with null XmlResolver and stylesheet has import/include, URI should throw ArgumentNullException, rest throw XsltException")]
        public int LoadGeneric4()
        {
            try
            {
                LoadXSL_Resolver("xmlResolver_main.xsl", null);
                CError.WriteLine("No exception was thrown when a null resolver is passed");
                return TEST_FAIL;
            }
            catch (XsltException e1)
            {
                CError.WriteLine(e1);
                return TEST_PASS;
            }
            catch (ArgumentNullException e2)
            {
                CError.WriteLine(e2);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                    return TEST_FAIL;
                }
            }
        }

        [Variation("Call Load with null custom resolver and style sheet has import/include, should throw Exception")]
        public int LoadGeneric5()
        {
            CustomNullResolver myResolver = new CustomNullResolver();

            try
            {
                LoadXSL_Resolver("xmlResolver_main.xsl", myResolver);
                CError.WriteLine("No exception is thrown");
                return TEST_FAIL;
            }
            catch (XsltException e1)
            {
                CError.WriteLine(e1);
                return TEST_PASS;
            }
            catch (ArgumentNullException e2)
            {
                CError.WriteLine(e2);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                    return TEST_FAIL;
                }
            }
            catch (XmlException e3)
            {
                CError.WriteLine(e3);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("XmlException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                    return TEST_FAIL;
                }
            }
        }

        [Variation("Call Load with null custom resolver and style sheet has no import/include, should error for URI only", Param = "ShowParam.txt")]
        public int LoadGeneric6()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            CustomNullResolver myResolver = new CustomNullResolver();
            try
            {
                LoadXSL_Resolver("ShowParam.xsl", myResolver);
                Transform("fruits.xml");
                return VerifyResult(Baseline, _strOutFile);
            }
            catch (ArgumentNullException e)
            {
                CError.WriteLine(e);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                    return TEST_FAIL;
                }
            }
            catch (XsltException e3)
            {
                CError.WriteLine(e3);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("XmlException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                    return TEST_FAIL;
                }
            }
        }

        [Variation("Style sheet has import/include, call Load first with custom null resolver and then default resolver, should not fail", Param = "XmlResolverTestMain.txt")]
        public int LoadGeneric7()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            CustomNullResolver myResolver = new CustomNullResolver();

            try
            {
                LoadXSL_Resolver("xmlResolver_main.xsl", myResolver);
            }
            //For input types != URI
            catch (System.Xml.Xsl.XsltException e1)
            {
                // The lovely thing about this test is that the stylesheet, XmlResolver_main.xsl, has an include. GetEntity is therefore called twice. We need to have both these
                // checks here to ensure that both the XmlResolver_main.xsl and XmlResolver_Include.xsl GetEntity() calls are handled.
                if (CheckExpectedError(e1, "System.Data.SqlXml", "Xslt_CannotLoadStylesheet", new string[] { new Uri(FullFilePath("XmlResolver_Include.xsl")).ToString(), "null" }) == TEST_PASS ||
                    CheckExpectedError(e1, "System.Data.SqlXml", "Xslt_CannotLoadStylesheet", new string[] { new Uri(FullFilePath("xmlResolver_main.xsl")).ToString(), "null" }) == TEST_PASS)
                {
                    if (LoadXSL("xmlResolver_main.xsl") == TEST_PASS)
                    {
                        if ((Transform("fruits.xml") == TEST_PASS) && (CheckResult(428.8541842246) == TEST_PASS))
                            return TEST_PASS;
                        else
                            return TEST_FAIL;
                    }
                    else
                    {
                        CError.WriteLine("Failed to load stylesheet using default resolver");
                        return TEST_FAIL;
                    }
                }
                else
                {
                    CError.WriteLine("Exception not thrown for null Resolver");
                    return TEST_FAIL;
                }
            }

            //For URI
            catch (System.ArgumentNullException e2)
            {
                if (CheckExpectedError(e2, "System.Data.SqlXml", "Xslt_CannotLoadStylesheet", new string[] { new Uri(FullFilePath("XmlResolver_Include.xsl")).ToString(), "null" }) == TEST_PASS ||
                    CheckExpectedError(e2, "System.Data.SqlXml", "Xslt_CannotLoadStylesheet", new string[] { new Uri(FullFilePath("xmlResolver_main.xsl")).ToString(), "null" }) == TEST_PASS)
                {
                    if (LoadXSL("xmlResolver_main.xsl") == TEST_PASS)
                    {
                        if ((Transform("fruits.xml") == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                            return TEST_PASS;
                        else
                            return TEST_FAIL;
                    }
                    else
                    {
                        CError.WriteLine("Failed to load stylesheet using default resolver");
                        return TEST_FAIL;
                    }
                }
                else
                {
                    CError.WriteLine("Exception not thrown for null Resolver");
                    return TEST_FAIL;
                }
            }
            catch (XmlException e3)
            {
                CError.WriteLine(e3);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("XmlException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                    return TEST_FAIL;
                }
            }

            return TEST_FAIL;
        }

        [Variation("Style sheet has import/include, call Load first with default resolver and then with custom null resolver, should fail", Param = "XmlResolverTestMain.txt")]
        public int LoadGeneric8()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            CustomNullResolver myResolver = new CustomNullResolver();

            if ((LoadXSL("xmlResolver_main.xsl") == TEST_PASS))
            {
                try
                {
                    LoadXSL_Resolver("xmlResolver_main.xsl", myResolver);
                }
                catch (System.Xml.Xsl.XsltException e1)
                {
                    // The lovely thing about this test is that the stylesheet, XmlResolver_main.xsl, has an include. GetEntity is therefore called twice. We need to have both these
                    // checks here to ensure that both the XmlResolver_main.xsl and XmlResolver_Include.xsl GetEntity() calls are handled.
                    // Yes, this is effetively the same test as LoadGeneric7, in that we use the NullResolver to return null from a GetEntity call.
                    if (CheckExpectedError(e1, "System.Data.SqlXml", "Xslt_CannotLoadStylesheet", new string[] { new Uri(FullFilePath("XmlResolver_Include.xsl")).ToString(), "null" }) == TEST_PASS)
                        return TEST_PASS;
                    else if (CheckExpectedError(e1, "System.Data.SqlXml", "Xslt_CannotLoadStylesheet", new string[] { new Uri(FullFilePath("xmlResolver_main.xsl")).ToString(), "null" }) == TEST_PASS)
                        return TEST_PASS;
                    else
                        return TEST_FAIL;
                }
                catch (ArgumentNullException e2)
                {
                    CError.WriteLine(e2);
                    if (MyXslInputType() == XslInputType.URI)
                        return TEST_PASS;
                    else
                    {
                        CError.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                        return TEST_FAIL;
                    }
                }
                catch (XmlException e3)
                {
                    CError.WriteLine(e3);
                    if (MyXslInputType() == XslInputType.URI)
                        return TEST_PASS;
                    else
                    {
                        CError.WriteLine("XmlException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                        return TEST_FAIL;
                    }
                }
                CError.WriteLine("No exception generated when loading with an invalid resolver after loading with valid resolver");
                return TEST_FAIL;
            }
            CError.WriteLine("Could not load style sheet with default resolver");
            return TEST_FAIL;
        }

        [Variation("Load with resolver with credentials, then load XSL that does not need cred.")]
        public int LoadGeneric9()
        {
            if ((LoadXSL_Resolver("xmlResolver_Main.xsl", GetDefaultCredResolver()) == TEST_PASS))
            {
                if ((LoadXSL("xmlResolver_Main.xsl") == TEST_PASS) && (Transform("fruits.xml") == TEST_PASS)
                    && (CheckResult(428.8541842246) == TEST_PASS))
                    return TEST_PASS;
            }
            else
            {
                CError.WriteLine("Failed to load!");
                return TEST_FAIL;
            }
            return TEST_FAIL;
        }

        /* This test doesn't make sense coz loading a stylesheet with null resolver will throw ArgumentNullException
        [Variation("Call Load() many times with null resolver then perform a transform")]
        public int LoadGeneric10()
        {
            for(int i=0; i < 100; i++)
            {
                if(LoadXSL_Resolver("showParam.xsl", null) != TEST_PASS)
                {
                    CError.WriteLine("Failed to load stylesheet showParam.xsl on the {0} attempt", i);
                    return TEST_FAIL;
                }
            }
            if((LoadXSL_Resolver("showParam.xsl", null) == TEST_PASS) && (Transform("fruits.xml") == TEST_PASS)
                && (VerifyResult(Baseline, _strOutFile)== TEST_PASS))
                return TEST_PASS;
            return TEST_FAIL;
        }
        */

        [Variation("Call Load with null Resolver, file does not exist")]
        public int LoadGeneric11()
        {
            try
            {
                LoadXSL_Resolver("IDontExist.xsl", null);
                CError.WriteLine("No exception was thrown");
                return TEST_FAIL;
            }
            catch (FileNotFoundException e1)
            {
                CError.WriteLine(e1);
                return TEST_PASS;
            }
            catch (ArgumentNullException e2)
            {
                CError.WriteLine(e2);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                    return TEST_FAIL;
                }
            }
        }

        /* This test doesn't make sense anymore coz passing null resolver throws ArgumentNullException
         * right on the first Load on showParam.xsl
         *
        [Variation("Load non existing stylesheet with null resolver and try to transform")]
        public int LoadGeneric12()
        {
            if(LoadXSL_Resolver("showParam.xsl", null) == TEST_PASS)
            {
                try
                {
                    LoadXSL_Resolver("IDontExist.xsl", null);
                }
                catch(System.IO.FileNotFoundException)
                {
                    //no stylesheet loaded, should throw error
                    try
                    {
                        Transform("fruits.xml");
                    }
                    catch(System.InvalidOperationException e2)
                    {
                        return CheckExpectedError(e2, "system.xml", "Xslt_NoStylesheetLoaded", new string[] { "IDontExist.xsl" });
                    }
                }
                CError.WriteLine("Exception not generated for non-existent file parameter name");
            }
            else
            {
                CError.WriteLine("Errors loading initial file");
                return TEST_FAIL;
            }
            return TEST_FAIL;
        }
        */

        [Variation("Load using resolver with cred., included file requires cred., should pass")]
        public int LoadGeneric13()
        {
            if ((LoadXSL_Resolver("Resolver_test_include.xsl", GetDefaultCredResolver()) == TEST_PASS) && (Transform("Resolver_test.xml") == TEST_PASS)
                && (CheckResult(468.6061088583) == TEST_PASS))
                return TEST_PASS;

            CError.WriteLine("Could not resolve included file");
            return TEST_FAIL;
        }

        [Variation("Load using null resolver, included file requires cred., should fail")]
        public int LoadGeneric14()
        {
            try
            {
                LoadXSL_Resolver("Resolver_test_include.xsl", null);
                CError.WriteLine("Null resolver should have thrown exception");
                return TEST_FAIL;
            }
            catch (XsltException e1)
            {
                CError.WriteLine(e1);
                return TEST_PASS;
            }
            catch (ArgumentNullException e2)
            {
                CError.WriteLine(e2);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                    return TEST_FAIL;
                }
            }
        }

        [Variation("Load using resolver with cred., imported file requires cred., should pass")]
        public int LoadGeneric15()
        {
            if ((LoadXSL_Resolver("Resolver_test_import.xsl", GetDefaultCredResolver()) == TEST_PASS) && (Transform("Resolver_test.xml") == TEST_PASS)
                && (CheckResult(468.6061088583) == TEST_PASS))
                return TEST_PASS;

            CError.WriteLine("Could not resolve included file");
            return TEST_FAIL;
        }

        [Variation("Load using null resolver, imported file requires cred., should fail")]
        public int LoadGeneric16()
        {
            try
            {
                LoadXSL_Resolver("Resolver_test_import.xsl", null);
                CError.WriteLine("Null resolver should have thrown an exception");
                return TEST_FAIL;
            }
            catch (XsltException e1)
            {
                CError.WriteLine(e1);
                return TEST_PASS;
            }
            catch (ArgumentNullException e2)
            {
                CError.WriteLine(e2);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                    return TEST_FAIL;
                }
            }
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Load(Url, Resolver)               */
    /***********************************************************/

    [TestCase(Name = "XslCompiledTransform.Load(Url, Resolver) : URI, Reader", Desc = "URI,READER")]
    [TestCase(Name = "XslCompiledTransform.Load(Url, Resolver) : URI, Stream", Desc = "URI,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Load(Url, Resolver) : URI, Writer", Desc = "URI,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Load(Url, Resolver) : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    public class CLoadUrlResolverTest : XsltApiTestCaseBase
    {
        [Variation("Basic check for usage of credentials on resolver, load XSL that needs cred. with correct resolver", Param = "XmlResolverTestMain.txt")]
        public int LoadUrlResolver1()
        {
            // XsltResolverTestMain.xsl is placed in IIS virtual directory
            // which requires integrated Windows NT authentication
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            if ((LoadXSL_Resolver(FullHttpPath("XmlResolver/XmlResolverTestMain.xsl"), GetDefaultCredResolver()) == TEST_PASS) &&
                (Transform("fruits.xml") == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;

            return TEST_FAIL;
        }

        [Variation("Load XSL that needs cred. with null resolver, should fail", Param = "XmlResolverTestMain.txt")]
        public int LoadUrlResolver2()
        {
            try
            {
                LoadXSL_Resolver(FullHttpPath("XmlResolver/XmlResolverTestMain.xsl"), null);
            }
            catch (XsltException e)
            {
                CError.WriteLine(e);
                return TEST_PASS;
            }
            CError.WriteLine("Passing null resolver should have thrown XsltException");
            return TEST_FAIL;
        }

        [Variation("Call Load with null source value")]
        public int LoadUrlResolver3()
        {
            try
            {
                LoadXSL_Resolver(null, new XmlUrlResolver());
            }
            catch (ArgumentNullException e)
            {
                CError.WriteLine(e);
                return TEST_PASS;
            }
            CError.WriteLine("Passing null stylesheet parameter should have thrown ArgumentNullException");
            return TEST_FAIL;
        }

        [Variation("Load using resolver with cred., included file requires cred., should pass")]
        public int LoadUrlResolver4()
        {
            if ((LoadXSL_Resolver("Resolver_test_include.xsl", GetDefaultCredResolver()) == TEST_PASS) && (Transform("Resolver_test.xml") == TEST_PASS)
                && (CheckResult(468.6061088583) == TEST_PASS))
                return TEST_PASS;

            CError.WriteLine("Could not resolve included file");
            return TEST_FAIL;
        }

        [Variation("Load using resolver with cred., imported file requires cred., should pass")]
        public int LoadUrlResolver5()
        {
            if ((LoadXSL_Resolver("Resolver_test_import.xsl", GetDefaultCredResolver()) == TEST_PASS) && (Transform("Resolver_test.xml") == TEST_PASS)
                && (CheckResult(468.6061088583) == TEST_PASS))
                return TEST_PASS;

            CError.WriteLine("Could not resolve included file");
            return TEST_FAIL;
        }
    }

    /****************************************************************************************/
    /*          XslCompiledTransform.Load(Reader/Navigator,XmlResolver,Evidence) - Integrity        */
    /****************************************************************************************/

    [TestCase(Name = "XslCompiledTransform.Load(,XmlResolver,Evidence) : Reader, Reader", Desc = "READER,READER")]
    [TestCase(Name = "XslCompiledTransform.Load(,XmlResolver,Evidence) : Reader, Stream", Desc = "READER,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Load(,XmlResolver,Evidence) : Reader, Writer", Desc = "READER,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Load(,XmlResolver,Evidence) : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    [TestCase(Name = "XslCompiledTransform.Load(,XmlResolver,Evidence) : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    [TestCase(Name = "XslCompiledTransform.Load(,XmlResolver,Evidence) : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Load(,XmlResolver,Evidence) : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Load(,XmlResolver,Evidence) : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CLoadReaderResolverEvidenceTest : XsltApiTestCaseBase
    {
        [Variation("Call Load with null source value, null evidence")]
        public int LoadGeneric1()
        {
            try
            {
                LoadXSL_Resolver_Evidence(null, new XmlUrlResolver(), null);
            }
            catch (ArgumentNullException e)
            {
                CError.WriteLine(e);
                return TEST_PASS;
            }
            CError.WriteLine("Passing null stylesheet parameter should have thrown ArgumentNullException");
            return TEST_FAIL;
        }

        [Variation("Call Load with style sheet that has script, pass null evidence, should throw security exception")]
        public int LoadGeneric2()
        {
            /*
            try
            {
                LoadXSL_Resolver_Evidence("scripting_unsafe_object.xsl", new XmlUrlResolver(), null);
            }
            catch(System.Security.Policy.PolicyException e)
            {
                CError.WriteLine(e);
                return TEST_PASS;
            }
            CError.WriteLine("Did not throw a security exception for null evidence!");
            return TEST_FAIL;
            */
            return TEST_SKIPPED;
        }

        [Variation("Call Load with style sheet that has script, pass correct evidence")]
        public int LoadGeneric3()
        {
            if (_isInProc)
                return TEST_SKIPPED;

            Evidence evidence = new Evidence();
            evidence.AddHost(new Zone(SecurityZone.MyComputer));
            try
            {
                LoadXSL_Resolver_Evidence("scripting_unsafe_object.xsl", new XmlUrlResolver(), evidence);
            }
            catch (System.Security.Policy.PolicyException)
            {
                CError.WriteLine("Should not throw a security exception for correct evidence!");
                return TEST_FAIL;
            }
            return TEST_PASS;
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Load(Url)                         */
    /***********************************************************/

    [TestCase(Name = "XslCompiledTransform.Load(Url) Integrity : URI, Stream", Desc = "URI,STREAM")]
    public class CLoadStringTest : XsltApiTestCaseBase
    {
        [Variation("Call Load with an invalid uri")]
        public int LoadUrl1()
        {
            try
            {
                LoadXSL("IDontExist.xsl", XslInputType.URI, new XmlUrlResolver());
                CError.WriteLine("No exception was thrown");
                return TEST_FAIL;
            }
            catch (FileNotFoundException e1)
            {
                CError.WriteLine(e1);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("FileNotFoundException is supposed to be thrown");
                    return TEST_FAIL;
                }
            }
            catch (ArgumentNullException e2)
            {
                CError.WriteLine(e2);
                if (MyXslInputType() == XslInputType.URI)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("ArgumentNullException is not supposed to be thrown for the input type '" + MyXslInputType() + "'");
                    return TEST_FAIL;
                }
            }
        }

        [Variation("Load file with empty string")]
        public int LoadUrl2()
        {
            try
            {
                LoadXSL(szEmpty, XslInputType.URI, new XmlUrlResolver());
            }
            catch (System.ArgumentException)
            {
                // System.Xml.XmlUrlResolver.ResolveUri(Uri baseUri, String relativeUri) throws ArgumentException
                return TEST_PASS;
            }
            CError.WriteLine("Exception not generated for an empty string filename");
            return TEST_FAIL;
        }

        [Variation("Load with \".\"")]
        public int LoadUrl3()
        {
            try
            {
                LoadXSL(".", XslInputType.URI, new XmlUrlResolver());
            }
            catch (System.UnauthorizedAccessException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Exception not generated for non-existent file parameter name");
            return TEST_FAIL;
        }

        [Variation("Load with \"..\"")]
        public int LoadUrl()
        {
            try
            {
                LoadXSL("..", XslInputType.URI, new XmlUrlResolver());
            }
            catch (System.UnauthorizedAccessException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Exception not generated for non-existent file parameter name");
            return TEST_FAIL;
        }

        [Variation("Load with \"\\\\\"")]
        public int LoadUrl5()
        {
            try
            {
                LoadXSL("\\\\", XslInputType.URI, new XmlUrlResolver());
            }
            catch (System.ArgumentException)
            {
                // System.Xml.XmlUrlResolver.ResolveUri(Uri baseUri, String relativeUri) throws ArgumentException
                return TEST_PASS;
            }
            CError.WriteLine("Exception not generated for non-existent file parameter name");
            return TEST_FAIL;
        }

        /*

        [Variation("Call Load with style sheet that has script, pass Url which does not have correct evidence, should fail")]
        public int LoadUrl6()
        {
            try
            {
                LoadXSL_Resolver(FullHttpPath("XmlResolver/scripting_unsafe_object.xsl"), GetDefaultCredResolver());
            }
            catch(System.Security.Policy.PolicyException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Should throw a security exception for incorrect evidence!");
            return TEST_FAIL;
        }

        [Variation("Call Load with style sheet that has script, pass Url which has correct evidence, should pass")]
        public int LoadUrl7()
        {
            try
            {
                LoadXSL("scripting_unsafe_object.xsl");
            }
            catch(System.Security.Policy.PolicyException)
            {
                CError.WriteLine("Should not throw a security exception for correct evidence!");
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        */
    }

    /***********************************************************/
    /*          XslCompiledTransform.Load(IXPathNavigable)             */
    /***********************************************************/

    [TestCase(Name = "XslCompiledTransform .Load(IXPathNavigable) : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CLoadXPathNavigableTest : XsltApiTestCaseBase
    {
        [Variation("Basic Verification Test", Param = "showParam.txt")]
        public int LoadNavigator1()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            xslt = new XslCompiledTransform();
            String _strXslFile = "showParam.xsl";

            _strXslFile = FullFilePath(_strXslFile);
            CError.WriteLine("Compiling {0}", _strXslFile);

            XmlReader xrLoad = XmlReader.Create(_strXslFile);
            XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
            xrLoad.Close();
            xslt.Load(xdTemp);

            if ((Transform("fruits.xml") == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            return TEST_FAIL;
        }

        [Variation("Create Navigator and navigate away from root", Param = "showParam.txt")]
        public int LoadNavigator2()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            xslt = new XslCompiledTransform();
            XmlReader xrLoad = XmlReader.Create(FullFilePath("showParam.xsl"));
            XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
            xrLoad.Close();
            XPathNavigator xP = ((IXPathNavigable)xdTemp).CreateNavigator();

            xP.MoveToNext();
            xslt.Load(xP);

            if ((Transform("fruits.xml") == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            return TEST_FAIL;
        }

        [Variation("Basic check for usage of credentials on resolver, load XSL that needs cred. with correct resolver", Param = "XmlResolverTestMain.txt")]
        public int LoadNavigator3()
        {
            xslt = new XslCompiledTransform();
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            XmlReader xrLoad = XmlReader.Create(FullHttpPath("XmlResolver/XmlResolverTestMain.xsl"));
            //xrLoad.XmlResolver = GetDefaultCredResolver();

            XPathDocument xdTemp = new XPathDocument(xrLoad, XmlSpace.Preserve);
            XPathNavigator xP = ((IXPathNavigable)xdTemp).CreateNavigator();

            xslt.Load(xP, XsltSettings.TrustedXslt, GetDefaultCredResolver());
            if ((Transform("fruits.xml") == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;

            return TEST_FAIL;
        }

        [Variation("Regression case for bug 80768")]
        public int LoadNavigator4()
        {
            if (_isInProc)
                return TEST_SKIPPED;

            xslt = new XslCompiledTransform();
            XmlReader xrLoad = XmlReader.Create(FullFilePath("Bug80768.xsl"));
            XPathDocument xd = new XPathDocument(xrLoad, XmlSpace.Preserve);

            xslt.Load(xd, XsltSettings.TrustedXslt, new XmlUrlResolver());

            FileStream fs = new FileStream(_strOutFile, FileMode.Create, FileAccess.ReadWrite);
            XPathNavigator xn = new MyNavigator(FullFilePath("foo.xml"));
            xslt.Transform(xn, null, fs);
            fs.Close();

            if (CheckResult(383.0855503831) == TEST_PASS)
                return TEST_PASS;
            else
                return TEST_FAIL;
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Load(Reader)                      */
    /***********************************************************/

    [TestCase(Name = "XslCompiledTransform.Load(Reader) : Reader, Stream", Desc = "READER,STREAM")]
    public class CLoadReaderTest : XsltApiTestCaseBase
    {
        [Variation("Basic Verification Test", Param = "showParam.txt")]
        public int LoadXmlReader1()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            Boolean fTEST_FAIL = false;
            xslt = new XslCompiledTransform();

            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ProhibitDtd = false;
            XmlReader xrTemp = XmlReader.Create(FullFilePath("showParam.xsl"));

            try
            {
                xslt.Load(xrTemp);
            }
            catch (Exception ex)
            {
                fTEST_FAIL = true;
                throw (ex);
            }
            finally
            {
                xrTemp.Close();
            }
            if (fTEST_FAIL)
                return TEST_FAIL;
            if ((Transform("fruits.xml") == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            return TEST_FAIL;
        }

        [Variation("Calling with a closed reader, should throw exception")]
        public int LoadXmlReader2()
        {
            xslt = new XslCompiledTransform();

            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ProhibitDtd = false;
            XmlReader xrTemp = XmlReader.Create(FullFilePath("showParam.xsl"), xrs);
            xrTemp.Close();

            try
            {
                xslt.Load(xrTemp);
            }
            catch (System.Xml.Xsl.XsltException e)
            {
                return CheckExpectedError(e, "system.data.sqlxml", "Xslt_WrongStylesheetElement", new string[] { "" });
            }
            CError.WriteLine("No exception thrown for a loading a closed reader!");
            return TEST_FAIL;
        }

        [Variation("Verify Reader isn't closed after Load")]
        public int LoadXmlReader3()
        {
            Boolean fTEST_FAIL = false;
            xslt = new XslCompiledTransform();

            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ProhibitDtd = false;
            XmlReader xrTemp = XmlReader.Create(FullFilePath("showParam.xsl"), xrs);

            try
            {
                xslt.Load(xrTemp);
            }
            catch (Exception ex)
            {
                fTEST_FAIL = true;
                throw (ex);
            }
            finally
            {
                if (!fTEST_FAIL || (xrTemp.ReadState != ReadState.Closed))
                    fTEST_FAIL = false;
                xrTemp.Close();
            }
            if (fTEST_FAIL)
            {
                CError.WriteLine("Appear to have accidently closed the Reader");
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Verify position of node in Reader is at EOF after Load")]
        public int LoadXmlReader4()
        {
            Boolean fTEST_FAIL = false;
            xslt = new XslCompiledTransform();

            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ProhibitDtd = false;
            XmlReader xrTemp = XmlReader.Create(FullFilePath("showParam.xsl"), xrs);
            try
            {
                xslt.Load(xrTemp);
            }
            catch (Exception ex)
            {
                fTEST_FAIL = true;
                throw (ex);
            }
            finally
            {
                if (!fTEST_FAIL && (!xrTemp.EOF))
                    fTEST_FAIL = false;
                xrTemp.Close();
            }
            if (fTEST_FAIL)
            {
                CError.WriteLine("Reader does not appear to be at the end of file.");
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Load with reader position at EOF, should throw exception")]
        public int LoadXmlReader5()
        {
            Boolean fTEST_FAIL = false;
            xslt = new XslCompiledTransform();

            XmlReaderSettings xrs = new XmlReaderSettings();
            xrs.ProhibitDtd = false;
            XmlReader xrTemp = XmlReader.Create(FullFilePath("showParam.xsl"), xrs);
            xslt.Load(xrTemp);
            try
            {
                xslt.Load(xrTemp);  // should now be at end and should give exception
                fTEST_FAIL = true;
            }
            catch (System.Xml.Xsl.XsltException e)
            {
                if (CheckExpectedError(e, "system.data.sqlxml", "Xslt_WrongStylesheetElement", new string[] { "" }) == TEST_FAIL)
                    fTEST_FAIL = true;
            }
            finally
            {
                xrTemp.Close();
            }
            if (fTEST_FAIL)
                return TEST_FAIL;
            return TEST_PASS;
        }

        [Variation("Load with NULL reader, should throw System.ArgumentNullException")]
        public int LoadXmlReader6()
        {
            xslt = new XslCompiledTransform();

            XmlTextReader xrTemp = null;

            try
            {
                xslt.Load(xrTemp);  // should now be at end and should give exception
            }
            catch (System.ArgumentNullException)
            {
                return TEST_PASS;
            }
            CError.WriteLine("Failed to throw System.ArgumentNullException for NULL reader input");
            return TEST_FAIL;
        }

        [Variation("Basic check for usage of credentials on resolver, load XSL that needs cred. with correct resolver", Param = "XmlResolverTestMain.txt")]
        public int LoadXmlReader7()
        {
            xslt = new XslCompiledTransform();
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            XmlReader xrLoad = XmlReader.Create(FullHttpPath("XmlResolver/XmlResolverTestMain.xsl"));
            //xrLoad.XmlResolver = GetDefaultCredResolver();
            xslt.Load(xrLoad, XsltSettings.TrustedXslt, GetDefaultCredResolver());
            xrLoad.Close();

            if ((Transform("fruits.xml") == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;

            return TEST_FAIL;
        }

        [Variation("bug 380138 NRE during XSLT compilation")]
        public int Bug380138()
        {
            string xsl = @"<?xml version='1.0' encoding='utf-8'?>
<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'
    xmlns:ms='urn:schemas-microsoft-com:xslt' exclude-result-prefixes='ms'>
  <xsl:template match='asf'><xsl:value-of select=""ms:namespace-uri('ms:b')""/></xsl:template>
</xsl:stylesheet>";
            XslCompiledTransform xslt = new XslCompiledTransform();
            try
            {
                xslt.Load(XmlReader.Create(new StringReader(xsl)));
            }
            catch (NullReferenceException nre)
            {
                CError.WriteLine(nre.Message);
                return TEST_FAIL;
            }

            return TEST_PASS;
        }
    }

    public class SimpleWrapperNavigator : XPathNavigator
    {
        private XPathNavigator innerNavigator;
        private XmlNameTable nt;

        public SimpleWrapperNavigator(XPathNavigator nav)
        {
            this.innerNavigator = nav;
            this.nt = new NameTable();
        }

        public SimpleWrapperNavigator(XPathNavigator nav, XmlNameTable nt)
        {
            this.innerNavigator = nav;
            this.nt = nt;
        }

        public override string BaseURI
        {
            get { return innerNavigator.BaseURI; }
        }

        public override XPathNavigator Clone()
        {
            return new SimpleWrapperNavigator(innerNavigator.Clone(), nt);
        }

        public override bool IsEmptyElement
        {
            get { return innerNavigator.IsEmptyElement; }
        }

        public override bool IsSamePosition(XPathNavigator other)
        {
            if (other is SimpleWrapperNavigator)
            {
                return innerNavigator.IsSamePosition((other as SimpleWrapperNavigator).innerNavigator);
            }
            else
            {
                return innerNavigator.IsSamePosition(other);
            }
        }

        public override string LocalName
        {
            get { return nt.Add(innerNavigator.LocalName); }
        }

        public override bool MoveTo(XPathNavigator other)
        {
            SimpleWrapperNavigator nav = other as SimpleWrapperNavigator;
            if (nav != null)
            {
                return innerNavigator.MoveTo(nav.innerNavigator);
            }
            return false;
        }

        public override bool MoveToFirstAttribute()
        {
            return innerNavigator.MoveToFirstAttribute();
        }

        public override bool MoveToFirstChild()
        {
            return innerNavigator.MoveToFirstChild();
        }

        public override bool MoveToFirstNamespace(XPathNamespaceScope namespaceScope)
        {
            return innerNavigator.MoveToFirstNamespace(namespaceScope);
        }

        public override bool MoveToId(string id)
        {
            return innerNavigator.MoveToId(id);
        }

        public override bool MoveToNext()
        {
            return innerNavigator.MoveToNext();
        }

        public override bool MoveToNextAttribute()
        {
            return innerNavigator.MoveToNextAttribute();
        }

        public override bool MoveToNextNamespace(XPathNamespaceScope namespaceScope)
        {
            return innerNavigator.MoveToNextNamespace(namespaceScope);
        }

        public override bool MoveToParent()
        {
            return innerNavigator.MoveToParent();
        }

        public override bool MoveToPrevious()
        {
            return innerNavigator.MoveToPrevious();
        }

        public override string Name
        {
            get { return innerNavigator.Name; }
        }

        public override XmlNameTable NameTable
        {
            get { return nt; }
        }

        public override string NamespaceURI
        {
            get { return nt.Add(innerNavigator.NamespaceURI); }
        }

        public override XPathNodeType NodeType
        {
            get { return innerNavigator.NodeType; }
        }

        public override string Prefix
        {
            get { return nt.Add(innerNavigator.Prefix); }
        }

        public override string Value
        {
            get { return innerNavigator.Value; }
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Transform - Integrity     */
    /***********************************************************/

    [TestCase(Name = "XslCompiledTransform.Transform() Integrity : Reader , Reader", Desc = "READER,READER")]
    [TestCase(Name = "XslCompiledTransform.Transform() Integrity : Reader, Stream", Desc = "READER,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Transform() Integrity : Reader, Writer", Desc = "READER,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Transform() Integrity : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    [TestCase(Name = "XslCompiledTransform.Transform() Integrity : URI, Reader", Desc = "URI,READER")]
    [TestCase(Name = "XslCompiledTransform.Transform() Integrity : URI, Stream", Desc = "URI,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Transform() Integrity : URI, Writer", Desc = "URI,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Transform() Integrity : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    [TestCase(Name = "XslCompiledTransform.Transform() Integrity : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    [TestCase(Name = "XslCompiledTransform.Transform() Integrity : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Transform() Integrity : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Transform() Integrity : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CTransformTestGeneric : XsltApiTestCaseBase
    {
        [Variation("Basic Verification Test", Param = "showParam.txt")]
        public int TransformGeneric1()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform("fruits.xml") == TEST_PASS) &&
                 (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Load and Transform multiple times", Param = "showParam.txt")]
        public int TransformGeneric2()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            for (int i = 0; i < 5; i++)
            {
                if ((LoadXSL("showParam.xsl") != TEST_PASS) || (Transform("fruits.xml") != TEST_PASS) ||
                     (VerifyResult(Baseline, _strOutFile) != TEST_PASS))
                    return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Load once, Transform many times", Param = "showParam.txt")]
        public int TransformGeneric3()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            if (LoadXSL("showParam.xsl") == TEST_PASS)
            {
                for (int i = 0; i < 100; i++)
                {
                    if ((Transform("fruits.xml") != TEST_PASS) || (VerifyResult(Baseline, _strOutFile) != TEST_PASS))
                    {
                        CError.WriteLine("Test failed to transform after {0} iterations", i);
                        return TEST_FAIL;
                    }
                }
                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("Call Transform without loading")]
        public int TransformGeneric4()
        {
            xslt = new XslCompiledTransform();
            try
            {
                Transform("fruits.xml");
            }
            catch (System.InvalidOperationException e)
            {
                return CheckExpectedError(e, "system.xml", "Xslt_NoStylesheetLoaded", new string[] { "" });
            }
            CError.WriteLine("Exception not given for a transform that didn't have a Load method instantiated");
            return TEST_FAIL;
        }

        [Variation("Closing XSL and XML files used in transform, Read access")]
        public int TransformGeneric5()
        {
            FileStream s2;

            if ((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform("fruits.xml") == TEST_PASS))
            {
                s2 = new FileStream(FullFilePath("showParam.xsl"), FileMode.Open, FileAccess.Read);
                s2.Close();

                s2 = new FileStream(FullFilePath("fruits.xml"), FileMode.Open, FileAccess.Read);
                s2.Close();

                return TEST_PASS;
            }
            CError.WriteLine("Encountered errors performing transform and could not verify if files were closed");
            return TEST_FAIL;
        }

        /*
        [Variation("Closing XSL and XML files used in transform, ReadWrite access")]
        public int TransformGeneric6()
        {
            FileStream s2;

            if((LoadXSL("showParam.xsl") == TEST_PASS) && (Transform("fruits.xml") == TEST_PASS))
            {
                s2 = new FileStream(FullFilePath("showParam.xsl"), FileMode.Open, FileAccess.ReadWrite);
                s2.Close();

                s2 = new FileStream(FullFilePath("fruits.xml"), FileMode.Open, FileAccess.ReadWrite);
                s2.Close();

                return TEST_PASS;
            }
            CError.WriteLine("Encountered errors performing transform and could not verify if files were closed");
            return TEST_FAIL;
        }
        */

        [Variation("Bug20003707 - InvalidProgramException for 2.0 stylesheets in forwards-compatible mode")]
        public int TransformGeneric7()
        {
            if (_isInProc)
                return TEST_SKIPPED;

            try
            {
                LoadXSL("ForwardComp.xsl");
                Transform("data.xml");
            }
            catch (XsltException e)
            {
                return CheckExpectedError(e, "system.data.sqlxml", "XPath_ScientificNotation", new string[] { "" });
            }
            CError.WriteLine("XsltException (XPath_ScientificNotation) was expected");
            return TEST_FAIL;
        }

        [Variation("Bug382506 - Loading stylesheet from custom navigator with enableDebug = true causes ArgumentOutOfRangeException")]
        public int TransformGeneric8()
        {
            xslt = new XslCompiledTransform();
            xslt.Load(new SimpleWrapperNavigator(new XPathDocument(FullFilePath("CustomNav.xsl")).CreateNavigator()));

            return TEST_PASS;
        }

        [Variation("Bug378293 - Incorrect error message when an attribute is added to a root node")]
        public int TransformGeneric9()
        {
            if (_isInProc)
                return TEST_SKIPPED;

            try
            {
                LoadXSL("RootNodeAtt.xsl");
                Transform("data.xml");
            }
            catch (XsltException e)
            {
                return CheckExpectedError(e, "system.data.sqlxml", "XmlIl_BadXmlState", new string[] { "Attribute", "Root" });
            }
            CError.WriteLine("XslTransformException (XmlIl_BadXmlState) was expected");
            return TEST_FAIL;
        }

        [Variation("Bug349757 - document() function does not work when stylesheet was loaded from a stream or reader or constructed DOM")]
        public int TransformGeneric10()
        {
            xslt = new XslCompiledTransform();
            XmlDocument doc = new XmlDocument();
            doc.LoadXml("<doc xsl:version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'>" + "<xsl:copy-of select='document(\"test.xml\")'/>" + "</doc>");

            xslt.Load(doc, XsltSettings.TrustedXslt, new XmlUrlResolver());
            return TEST_PASS;
        }

        [Variation("Bug369463 - Invalid XPath exception in forward compatibility mode should render lineNumber linePosition")]
        public int TransformGeneric11()
        {
            if (_isInProc)
                return TEST_SKIPPED;

            try
            {
                LoadXSL("Bug369463.xsl");
                Transform("data.xml");
            }
            catch (XsltException e)
            {
                return CheckExpectedError(e, "system.data.sqlxml", "XPath_UnexpectedToken", new string[] { "+" });
            }
            CError.WriteLine("XslException (XPath_UnexpectedToken) was expected");
            return TEST_FAIL;
        }
    }

    /*************************************************************/
    /*          XslCompiledTransform(Resolver) - Integrity       */
    /*************************************************************/

    [TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Reader, Reader", Desc = "READER,READER")]
    [TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Reader, Stream", Desc = "READER,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Reader, Writer", Desc = "READER,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Reader, TextWriter", Desc = "READER,TEXTWRITER")]
    [TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : URI, Reader", Desc = "URI,READER")]
    [TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : URI, Stream", Desc = "URI,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : URI, Writer", Desc = "URI,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : URI, TextWriter", Desc = "URI,TEXTWRITER")]
    [TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Navigator, Reader", Desc = "NAVIGATOR,READER")]
    [TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Navigator, Stream", Desc = "NAVIGATOR,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Navigator, Writer", Desc = "NAVIGATOR,WRITER")]
    [TestCase(Name = "XslCompiledTransform.Transform(XmlResolver) : Navigator, TextWriter", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CTransformResolverTest : XsltApiTestCaseBase
    {
        [Variation("Pass null XmlResolver, load style sheet with import/include, should not affect transform")]
        public int XmlResolver1()
        {
            try
            {
                if (LoadXSL("xmlResolver_main.xsl") == TEST_PASS)
                {
                    if ((TransformResolver("fruits.xml", null) == TEST_PASS) && (CheckResult(428.8541842246) == TEST_PASS))
                        return TEST_PASS;
                    else
                        return TEST_FAIL;
                }
            }
            catch (Exception e)
            {
                CError.WriteLine(e);
                return TEST_FAIL;
            }
            return TEST_FAIL;
        }

        [Variation("Pass null XmlResolver, load style sheet with document function, should not resolve during transform", Param = "xmlResolver_document_function.txt")]
        public int XmlResolver2()
        {
            // "xmlResolver_document_function.xsl" contains
            // <xsl:for-each select="document('xmlResolver_document_function.xml')//elem">
            string Baseline = "baseline\\" + CurVariation.Param.ToString();

            if (LoadXSL("xmlResolver_document_function.xsl") == TEST_PASS)
            {
                if ((TransformResolver("fruits.xml", null) == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                    return TEST_PASS;
            }
            else
            {
                CError.WriteLine("Problem loading stylesheet!");
                return TEST_FAIL;
            }
            return TEST_FAIL;
        }

        [Variation("Default XmlResolver, load style sheet with document function, should resolve during transform", Param = "DefaultResolver.txt")]
        public int XmlResolver3()
        {
            // "xmlResolver_document_function.xsl" contains
            // <xsl:for-each select="document('xmlResolver_document_function.xml')//elem">
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            if (LoadXSL("xmlResolver_document_function.xsl") == TEST_PASS)
            {
                if ((Transform("fruits.xml") == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                    return TEST_PASS;
            }
            else
            {
                CError.WriteLine("Problem loading stylesheet with document function and default resolver!");
                return TEST_FAIL;
            }
            return TEST_FAIL;
        }

        [Variation("Transform with null and then correct resolver successively", Param = "xmlResolver_cred.txt")]
        public int XmlResolver4()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            if (LoadXSL("xmlResolver_cred.xsl") == TEST_PASS)
            {
                // Pass null
                if ((TransformResolver("fruits.xml", null) == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                {
                    // Pass correct resolver
                    if ((TransformResolver("fruits.xml", GetDefaultCredResolver()) == TEST_PASS) && (VerifyResult(Baseline, _strOutFile) == TEST_PASS))
                        return TEST_PASS;
                    else
                        return TEST_FAIL;
                }
                else
                {
                    CError.WriteLine("Failed to use XmlResolver property to resolve document function");
                    return TEST_FAIL;
                }
            }
            else
            {
                CError.WriteLine("Failed to load style sheet!");
                return TEST_FAIL;
            }
        }

        [Variation("document() has absolute URI")]
        public int XmlResolver5()
        {
            // copy file on the local machine
            try
            {
                string curDir = Directory.GetCurrentDirectory();
                string testFile = curDir + "\\" + "xmlResolver_document_function.xml";
                if (File.Exists(testFile))
                {
                    File.SetAttributes(testFile, FileAttributes.Normal);
                    File.Delete(testFile);
                }
                string xmlFile = FullFilePath("xmlResolver_document_function.xml");
                File.Copy(xmlFile, testFile, true);
            }
            catch (Exception e)
            {
                CError.WriteLine(e);
                CError.WriteLine("Could not copy file to local. Some other issues prevented this test from running");
                return TEST_SKIPPED;
            }

            if (LoadXSL("xmlResolver_document_function_absolute_uri.xsl") == TEST_PASS)
            {
                if ((TransformResolver("fruits.xml", new XmlUrlResolver()) == TEST_PASS) && (CheckResult(377.8217373898) == TEST_PASS))
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("Failed to resolve document function with absolute URI.");
                    return TEST_FAIL;
                }
            }
            else
            {
                CError.WriteLine("Failed to load style sheet!");
                return TEST_FAIL;
            }
        }

        [Variation("document() has file:// URI")]
        public int XmlResolver6()
        {
            if (LoadXSL("xmlResolver_document_function_file_uri.xsl") == TEST_PASS)
            {
                if ((TransformResolver("fruits.xml", new XmlUrlResolver()) == TEST_PASS) && (CheckResult(377.8217373898) == TEST_PASS))
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("Failed to resolve document function with file:// URI.");
                    return TEST_FAIL;
                }
            }
            else
            {
                CError.WriteLine("Failed to load style sheet!");
                return TEST_FAIL;
            }
        }

        [Variation("Pass null resolver but stylesheet doesn't have any include/imports")]
        public int XmlResolver7()
        {
            LoadXSL("Bug382198.xsl");
            // Pass null
            TransformResolver("fruits.xml", null);
            return TEST_PASS;
        }
    }

    /***********************************************************/
    /*          XslCompiledTransform.Transform - (String, String)                    */
    /***********************************************************/

    [TestCase(Name = "XslCompiledTransform.Transform(String, String) : Reader , String", Desc = "READER,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Transform(String, String) : URI, String", Desc = "URI,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Transform(String, String) : Navigator, String", Desc = "NAVIGATOR,STREAM")]
    public class CTransformStrStrTest : XsltApiTestCaseBase
    {
        [Variation("Basic Verification Test", Param = "showParam.txt")]
        public int TransformStrStr1()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            String szFullFilename = FullFilePath("fruits.xml");

            if (LoadXSL("showParam.xsl") == TEST_PASS)
            {
                xslt.Transform(szFullFilename, _strOutFile);
                if (VerifyResult(Baseline, _strOutFile) == TEST_PASS)
                    return TEST_PASS;
            }
            return TEST_FAIL;
        }

        [Variation("Input is null")]
        public int TransformStrStr2()
        {
            if (LoadXSL("showParam.xsl") == TEST_PASS)
            {
                try
                {
                    xslt.Transform(null, _strOutFile);
                }
                catch (System.ArgumentException)
                { return TEST_PASS; }
            }
            CError.WriteLine("Exception not generated for null input filename");
            return TEST_FAIL;
        }

        [Variation("Output file is null")]
        public int TransformStrStr3()
        {
            String szFullFilename = FullFilePath("fruits.xml");

            if (LoadXSL("showParam.xsl") == TEST_PASS)
            {
                try
                {
                    xslt.Transform(szFullFilename, (string)null);
                }
                catch (System.ArgumentException)
                {
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Exception not generated for null output filename");
            return TEST_FAIL;
        }

        [Variation("Input is nonexisting file")]
        public int TransformStrStr4()
        {
            if (LoadXSL("showParam.xsl") == TEST_PASS)
            {
                try
                {
                    xslt.Transform("IDontExist.xsl", _strOutFile);
                }
                catch (System.IO.FileNotFoundException)
                {
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Exception not generated for invalid input file");
            return TEST_FAIL;
        }

        [Variation("Output file is invalid")]
        public int TransformStrStr5()
        {
            String szFullFilename = FullFilePath("fruits.xml");

            if (LoadXSL("showParam.xsl") == TEST_PASS)
            {
                try
                {
                    xslt.Transform(szFullFilename, szInvalid);
                }
                catch (System.ArgumentException)
                {
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Exception not generated for invalid output destination");
            return TEST_FAIL;
        }

        [Variation("Input is empty string")]
        public int TransformStrStr6()
        {
            if (LoadXSL("showParam.xsl") == TEST_PASS)
            {
                try
                {
                    xslt.Transform(szEmpty, _strOutFile);
                }
                catch (System.ArgumentException)
                {
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Exception not generated for empty string input file");
            return TEST_FAIL;
        }

        [Variation("Output file is empty string")]
        public int TransformStrStr7()
        {
            String szFullFilename = FullFilePath("fruits.xml");

            if (LoadXSL("showParam.xsl") == TEST_PASS)
            {
                try
                {
                    xslt.Transform(szFullFilename, szEmpty);
                }
                catch (System.ArgumentException)
                {
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Exception not generated for empty output file name");
            return TEST_FAIL;
        }

        [Variation("Call Transform many times", Param = "showParam.txt")]
        public int TransformStrStr8()
        {
            string Baseline = "baseline\\" + CurVariation.Param.ToString();
            String szFullFilename = FullFilePath("fruits.xml");

            for (int i = 0; i < 50; i++)
            {
                if (LoadXSL("showParam.xsl") == TEST_PASS)
                {
                    xslt.Transform(szFullFilename, _strOutFile);
                    if (VerifyResult(Baseline, _strOutFile) != TEST_PASS)
                    {
                        CError.WriteLine("Failed to process Load after calling {0} times", i);
                        return TEST_FAIL;
                    }
                }
            }
            return TEST_PASS;
        }

        [Variation("Call without loading")]
        public int TransformStrStr9()
        {
            xslt = new XslCompiledTransform();
            SetExpectedError("Xslt_NoStylesheetLoaded");
            try
            {
                xslt.Transform(FullFilePath("fruits.xml"), _strOutFile);
            }
            catch (System.InvalidOperationException e)
            {
                return CheckExpectedError(e, "System.xml", "Xslt_NoStylesheetLoaded", new string[] { "" });
            }
            CError.WriteLine("Exception attempting a transform without loading an XSL file");
            return TEST_FAIL;
        }

        [Variation("Output to unreachable destination")]
        public int TransformStrStr10()
        {
            if (LoadXSL("showParam.xsl") == TEST_PASS)
            {
                try
                {
                    xslt.Transform("fruits.xml", "http://www.IdontExist.com/index.xml");
                }
                catch (System.Exception e)
                {
                    CError.WriteLine(e);
                    return TEST_PASS;
                }
            }
            CError.WriteLine("Exception not generated for invalid output destination");
            return TEST_FAIL;
        }

        [Variation("Input filename is \'.\', \'..\', and \'\\\\\'")]
        public int TransformStrStr11()
        {
            int iCount = 0;
            if (LoadXSL("showParam.xsl") == TEST_PASS)
            {
                SetExpectedError("Xml_ResolveUrl");
                try
                {
                    xslt.Transform("..", _strOutFile);
                }
                catch (System.Exception)
                {
                    iCount++;
                }

                try
                {
                    xslt.Transform(".", _strOutFile);
                }
                catch (System.Exception)
                {
                    iCount++;
                }

                try
                {
                    xslt.Transform("\\\\", _strOutFile);
                }
                catch (System.Exception)
                {
                    iCount++;
                }
            }

            if (iCount.Equals(3))
                return TEST_PASS;

            CError.WriteLine("Exception not generated for invalid input sources");
            return TEST_FAIL;
        }

        [Variation("Output filename is \'.\', \'..\', and \'\\\\\'")]
        public int TransformStrStr12()
        {
            String szFullFilename = FullFilePath("fruits.xml");
            int iCount = 0;
            if (LoadXSL("showParam.xsl") == TEST_PASS)
            {
                try
                {
                    xslt.Transform(szFullFilename, "..");
                }
                catch (System.Exception)
                {
                    iCount++;
                }

                try
                {
                    xslt.Transform(szFullFilename, ".");
                }
                catch (System.Exception)
                {
                    iCount++;
                }

                try
                {
                    xslt.Transform(szFullFilename, "\\\\");
                }
                catch (System.Exception)
                {
                    iCount++;
                }
            }

            if (iCount.Equals(3))
                return TEST_PASS;
            CError.WriteLine("Exception not generated for invalid ouput destinations");
            return TEST_FAIL;
        }

        [Variation("Closing files after transform")]
        public int TransformStrStr13()
        {
            String szFullFilename = FullFilePath("fruits.xml");
            Stream strmTemp;

            if (LoadXSL("showParam.xsl") == TEST_PASS)
            {
                xslt.Transform(szFullFilename, _strOutFile);
                StreamReader fs = null;

                // check if I can open and close the xml file
                fs = new StreamReader(szFullFilename);
                fs.Close();

                strmTemp = new FileStream(szFullFilename, FileMode.Open, FileAccess.Read);
                strmTemp.Close();

                // check if I can open and close the output file
                fs = new StreamReader(_strOutFile);
                fs.Close();

                strmTemp = new FileStream(_strOutFile, FileMode.Open, FileAccess.Read);
                strmTemp.Close();

                return TEST_PASS;
            }
            return TEST_FAIL;
        }

        /*
        [Variation("Transform(test.xml, test.xml)")]
        public int TransformStrStr14()
        {
            String szFullFilename = FullFilePath("Bug75295.xml");

            // Copy this file to current directory
            File.Delete("out.xml");
            File.Copy(szFullFilename, "out.xml");

            if(LoadXSL("Bug75295.xsl") == TEST_PASS)
            {
                xslt.Transform("out.xml", "out.xml");

                if (CheckResult(270.5223692973) == TEST_PASS)
                    return TEST_PASS;
            }
            return TEST_FAIL;
        }
        */
    }

    /***********************************************************/
    /*          XslCompiledTransform.Transform - (String, String, Resolver)          */
    /***********************************************************/

    [TestCase(Name = "XslCompiledTransform.Transform(String, String, Resolver) : Reader , String", Desc = "READER,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Transform(String, String, Resolver) : URI, String", Desc = "URI,STREAM")]
    [TestCase(Name = "XslCompiledTransform.Transform(String, String, Resolver) : Navigator, String", Desc = "NAVIGATOR,STREAM")]
    public class CTransformStrStrResolverTest : XsltApiTestCaseBase
    {
        [Variation("Pass null XmlResolver to Transform, load style sheet with import/include, should not affect transform")]
        public int TransformStrStrResolver1()
        {
            String szFullFilename = FullFilePath("fruits.xml");
            try
            {
                if (LoadXSL("xmlResolver_main.xsl", new XmlUrlResolver()) == TEST_PASS)
                {
                    XmlTextReader xr = new XmlTextReader(szFullFilename);
                    XmlTextWriter xw = new XmlTextWriter("out.xml", Encoding.Unicode);
                    xslt.Transform(xr, null, xw, null);
                    xr.Close();
                    xw.Close();
                    if (CheckResult(403.7784431795) == TEST_PASS)
                        return TEST_PASS;
                    else
                        return TEST_FAIL;
                }
            }
            catch (Exception e)
            {
                CError.WriteLine(e);
                return TEST_FAIL;
            }
            return TEST_FAIL;
        }

        [Variation("Pass null XmlResolver, load style sheet with document function, should not resolve during transform")]
        public int TransformStrStrResolver2()
        {
            // "xmlResolver_document_function.xsl" contains
            // <xsl:for-each select="document('xmlResolver_document_function.xml')//elem">

            String szFullFilename = FullFilePath("fruits.xml");

            if (LoadXSL("xmlResolver_document_function.xsl") == TEST_PASS)
            {
                xslt.Transform(szFullFilename, "out.xml");
                if (CheckResult(422.3877210723) == TEST_PASS)
                    return TEST_PASS;
            }
            else
            {
                CError.WriteLine("Problem loading stylesheet!");
                return TEST_FAIL;
            }
            return TEST_FAIL;
        }

        [Variation("Pass XmlUrlResolver, load style sheet with document function, should resolve during transform", Param = "xmlResolver_document_function.txt")]
        public int TransformStrStrResolver3()
        {
            // "xmlResolver_document_function.xsl" contains
            // <xsl:for-each select="document('xmlResolver_document_function.xml')//elem">

            String szFullFilename = FullFilePath("fruits.xml");
            string Baseline = "baseline\\" + CurVariation.Param.ToString();

            if (LoadXSL("xmlResolver_document_function.xsl") == TEST_PASS)
            {
                xslt.Transform(szFullFilename, "out.xml");
                if (VerifyResult(Baseline, _strOutFile) == TEST_PASS)
                    return TEST_PASS;
            }
            else
            {
                CError.WriteLine("Problem loading stylesheet with document function and default resolver!");
                return TEST_FAIL;
            }
            return TEST_FAIL;
        }

        [Variation("document() has file:// URI")]
        public int TransformStrStrResolver4()
        {
            String szFullFilename = FullFilePath("fruits.xml");

            if (LoadXSL("xmlResolver_document_function_file_uri.xsl") == TEST_PASS)
            {
                xslt.Transform(szFullFilename, "out.xml");
                if (CheckResult(377.8217373898) == TEST_PASS)
                    return TEST_PASS;
                else
                {
                    CError.WriteLine("Failed to resolve document function with file:// URI.");
                    return TEST_FAIL;
                }
            }
            else
            {
                CError.WriteLine("Failed to load style sheet!");
                return TEST_FAIL;
            }
        }
    }

    [TestCase(Name = "XslCompiledTransform.Transform(IXPathNavigable, XsltArgumentList, XmlWriter, XmlResolver)", Desc = "Constructor Tests", Param = "IXPathNavigable")]
    [TestCase(Name = "XslCompiledTransform.Transform(XmlReader, XsltArgumentList, XmlWriter, XmlResolver)", Desc = "Constructor Tests", Param = "XmlReader")]
    public class CTransformConstructorWihtFourParametersTest : XsltApiTestCaseBase
    {
        internal class CustomXmlResolver : XmlUrlResolver
        {
            private string baseUri;

            public CustomXmlResolver(string baseUri)
            {
                this.baseUri = baseUri;
            }

            public override Uri ResolveUri(Uri baseUri, string relativeUri)
            {
                if (baseUri == null)
                    return base.ResolveUri(new Uri(this.baseUri), relativeUri);
                return base.ResolveUri(baseUri, relativeUri);
            }
        }

        [Variation("Document Function has file:// URI, CustomXmlResolver", Pri = 0, Params = new object[] { "xmlResolver_document_function_file_uri.xsl", "fruits.xml", "xmlResolver_cred.txt", "CustomXmlResolver", true })]
        [Variation("Document Function 2, CustomXmlResolver", Pri = 0, Params = new object[] { "xmlResolver_cred.xsl", "fruits.xml", "xmlResolver_cred.txt", "CustomXmlResolver", true })]
        [Variation("Document function 1, CustomXmlResolver", Pri = 0, Params = new object[] { "xmlResolver_document_function.xsl", "fruits.xml", "xmlResolver_document_function.txt", "CustomXmlResolver", true })]
        [Variation("Import/Include, CustomXmlResolver", Pri = 0, Params = new object[] { "xmlResolver_main.xsl", "fruits.xml", "xmlResolver_main.txt", "CustomXmlResolver", true })]
        [Variation("No Import/Include, CustomXmlResolver", Pri = 0, Params = new object[] { "Bug382198.xsl", "fruits.xml", "Bug382198.txt", "CustomXmlResolver", true })]
        [Variation("Document Function has file:// URI, XmlUrlResolver", Pri = 0, Params = new object[] { "xmlResolver_document_function_file_uri.xsl", "fruits.xml", "xmlResolver_cred.txt", "XmlUrlResolver", true })]
        [Variation("Document Function 2, XmlUrlResolver", Pri = 0, Params = new object[] { "xmlResolver_cred.xsl", "fruits.xml", "xmlResolver_cred.txt", "XmlUrlResolver", true })]
        [Variation("Document function 1, XmlUrlResolver", Pri = 0, Params = new object[] { "xmlResolver_document_function.xsl", "fruits.xml", "xmlResolver_document_function.txt", "XmlUrlResolver", true })]
        [Variation("Import/Include, XmlUrlResolver", Pri = 0, Params = new object[] { "xmlResolver_main.xsl", "fruits.xml", "xmlResolver_main.txt", "XmlUrlResolver", true })]
        [Variation("No Import/Include, XmlUrlResolver", Pri = 0, Params = new object[] { "Bug382198.xsl", "fruits.xml", "Bug382198.txt", "XmlUrlResolver", true })]
        [Variation("Document Function has file:// URI, NullResolver", Pri = 0, Params = new object[] { "xmlResolver_document_function_file_uri.xsl", "fruits.xml", "xmlResolver_cred.txt", "NullResolver", false })]
        [Variation("Document Function 2, NullResolver", Pri = 0, Params = new object[] { "xmlResolver_cred.xsl", "fruits.xml", "xmlResolver_cred.txt", "NullResolver", false })]
        [Variation("Document function 1, NullResolver", Pri = 0, Params = new object[] { "xmlResolver_document_function.xsl", "fruits.xml", "xmlResolver_document_function.txt", "NullResolver", false })]
        [Variation("Import/Include, NullResolver", Pri = 0, Params = new object[] { "xmlResolver_main.xsl", "fruits.xml", "xmlResolver_main.txt", "NullResolver", false })]
        [Variation("No Import/Include, NullResolver", Pri = 0, Params = new object[] { "Bug382198.xsl", "fruits.xml", "Bug382198.txt", "NullResolver", true })]
        public int ValidCases()
        {
            string xslFile = FullFilePath(CurVariation.Params[0] as string);
            string xmlFile = FullFilePath(CurVariation.Params[1] as string);
            string baseLineFile = @"\baseline\" + CurVariation.Params[2] as string;
            bool expectedResult = (bool)CurVariation.Params[4];
            bool actualResult = false;

            XmlReader xmlReader = XmlReader.Create(xmlFile);
            //Let's select randomly how to create navigator
            IXPathNavigable navigator = null;
            Random randGenerator = new Random((int)DateTime.Now.Ticks);
            switch (randGenerator.Next(2))
            {
                case 0:
                    CError.WriteLine("Using XmlDocument.CreateNavigator()");
                    XmlDocument xmlDoc = new XmlDocument();
                    xmlDoc.Load(xmlFile);
                    navigator = xmlDoc.CreateNavigator();
                    break;

                case 1:
                    CError.WriteLine("Using XPathDocument.CreateNavigator()");
                    XPathDocument xpathDoc;
                    using (XmlReader reader = XmlReader.Create(xmlFile))
                    {
                        xpathDoc = new XPathDocument(reader);
                        navigator = xpathDoc.CreateNavigator();
                    }
                    break;

                default:
                    break;
            }

            XmlResolver resolver = null;
            switch (CurVariation.Params[3] as string)
            {
                case "NullResolver":
                    break;

                case "XmlUrlResolver":
                    resolver = new XmlUrlResolver();
                    break;

                case "CustomXmlResolver":
                    resolver = new CustomXmlResolver(Path.Combine(FilePathUtil.GetTestDataPath(), @"XsltApiV2\"));
                    break;

                default:
                    break;
            }

            try
            {
                XslCompiledTransform localXslt = new XslCompiledTransform();
                XsltSettings settings = new XsltSettings(true, true);
                using (XmlReader xslReader = XmlReader.Create(xslFile))
                    localXslt.Load(xslReader, settings, resolver);

                using (XmlWriter writer = XmlWriter.Create("outputFile.txt"))
                {
                    if (this.Param as string == "XmlReader")
                        localXslt.Transform(xmlReader, null, writer, resolver);
                    else
                        localXslt.Transform(navigator, null, writer, resolver);
                }
                actualResult = VerifyResult(baseLineFile, "outputFile.txt") == TEST_PASS ? true : false;
            }
            catch (Exception ex)
            {
                CError.WriteLine(ex);
                actualResult = false;
            }

            if (actualResult != expectedResult)
                return TEST_FAIL;

            return TEST_PASS;
        }

        [Variation("Invalid Arguments: null, valid, valid, valid", Pri = 0, Params = new object[] { 1, false })]
        [Variation("Invalid Arguments: valid, null, valid, valid", Pri = 0, Params = new object[] { 2, true })]
        [Variation("Invalid Arguments: valid, valid, null, valid", Pri = 0, Params = new object[] { 3, false })]
        [Variation("Invalid Arguments: valid, valid, valid, null", Pri = 0, Params = new object[] { 4, true })]
        public int InValidCases()
        {
            int argumentNumber = (int)CurVariation.Params[0];
            bool expectedResult = (bool)CurVariation.Params[1];
            bool actualResult = false;

            XslCompiledTransform localXslt = new XslCompiledTransform();
            string stylesheet = @"<xsl:stylesheet version=""1.0"" xmlns:xsl=""http://www.w3.org/1999/XSL/Transform"" />";
            using (XmlReader xslReader = XmlReader.Create(new StringReader(stylesheet)))
            {
                localXslt.Load(xslReader);
            }

            string xmlString = "<root />";
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlString);
            XmlReader xmlReader = XmlReader.Create(new StringReader(xmlString));
            XPathNavigator nav = xmlDoc.CreateNavigator();
            object[] testInput = new object[] { xmlReader, nav, new XsltArgumentList(), XmlWriter.Create(Stream.Null), new XmlUrlResolver() };
            if (argumentNumber == 1)
                testInput[0] = null;
            testInput[argumentNumber] = null;

            try
            {
                if (this.Param as string == "XmlReader")
                    localXslt.Transform(testInput[0] as XmlReader, testInput[2] as XsltArgumentList, testInput[3] as XmlWriter, testInput[4] as XmlResolver);
                else
                    localXslt.Transform(testInput[1] as IXPathNavigable, testInput[2] as XsltArgumentList, testInput[3] as XmlWriter, testInput[4] as XmlResolver);
                actualResult = true;
            }
            catch (ArgumentNullException ex)
            {
                CError.WriteLine(ex);
                actualResult = false;
            }

            if (actualResult != expectedResult)
                return TEST_FAIL;

            return TEST_PASS;
        }
    }

    // This testcase is for bugs 109429, 111075 and 109644 fixed in Everett SP1
    [TestCase(Name = "NDP1_1SP1 Bugs (URI,STREAM)", Desc = "URI,STREAM")]
    [TestCase(Name = "NDP1_1SP1 Bugs (NAVIGATOR,TEXTWRITER)", Desc = "NAVIGATOR,TEXTWRITER")]
    public class CNDP1_1SP1Test : XsltApiTestCaseBase
    {
        [Variation("Local parameter gets overwritten with global param value", Pri = 1)]
        public int var1()
        {
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("param1", string.Empty, "global-param1-arg");

            if ((LoadXSL("paramScope.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) && (CheckResult(473.4644857331) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Local parameter gets overwritten with global variable value", Pri = 1)]
        public int var2()
        {
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddParam("param1", string.Empty, "global-param1-arg");

            if ((LoadXSL("varScope.xsl") == TEST_PASS) && (Transform_ArgList("fruits.xml") == TEST_PASS) && (CheckResult(473.4644857331) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Subclassed XPathNodeIterator returned from an extension object or XsltFunction is not accepted by XPath", Pri = 1)]
        public int var3()
        {
            m_xsltArg = new XsltArgumentList();
            m_xsltArg.AddExtensionObject("http://foo.com", new MyXsltExtension());

            if ((LoadXSL("Bug111075.xsl") == TEST_PASS) && (Transform_ArgList("Bug111075.xml") == TEST_PASS) && (CheckResult(441.288076277) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Iterator using for-each over a variable is not reset correctly while using msxsl:node-set()", Pri = 1)]
        public int var4()
        {
            if ((LoadXSL("Bug109644.xsl") == TEST_PASS) && (Transform("foo.xml") == TEST_PASS) && (CheckResult(417.2501860011) == TEST_PASS))
                return TEST_PASS;
            else
                return TEST_FAIL;
        }
    }

    [TestCase(Name = "XslCompiledTransform Regression Tests for API", Desc = "XslCompiledTransform Regression Tests")]
    public class CTransformRegressionTest : XsltApiTestCaseBase
    {
        [Variation("Bug398968 - Globalization is broken for document() function")]
        public int RegressionTest1()
        {
            // <SQL BU Defect Tracking 410060>
            if (_isInProc)
                return TEST_SKIPPED;
            // </SQL BU Defect Tracking 410060>

            string testFile = "Stra\u00DFe.xml";

            // Create the file.
            using (FileStream fs = File.Open(testFile, FileMode.Open))
            {
                Byte[] info = new UTF8Encoding(true).GetBytes("<PASSED/>");
                fs.Write(info, 0, info.Length);
            }

            LoadXSL("398968repro.xsl");
            Transform("data.xml");
            return TEST_PASS;
        }

        [Variation("Bug410158 - Debug flag on XslCompiledTransform contaminates XsltSettings")]
        public int RegressionTest2()
        {
            // <SQL BU Defect Tracking 410060>
            // This test uses Reflection, which doenst work inproc
            if (_isInProc)
                return TEST_SKIPPED;
            // </SQL BU Defect Tracking 410060>

            string stylesheet = "<xsl:stylesheet version='1.0' xmlns:xsl='http://www.w3.org/1999/XSL/Transform'/>";
            XsltSettings settings = new XsltSettings();
            XmlResolver resolver = new XmlUrlResolver();

            int debuggableCnt1 = 0;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string asmName = asm.GetName().Name;
                if (asmName.StartsWith("System.Xml.Xsl.CompiledQuery.", StringComparison.Ordinal))
                {
                    object[] debuggableAttrs = asm.GetCustomAttributes(typeof(DebuggableAttribute), false);
                    if (debuggableAttrs.Length > 0 && ((DebuggableAttribute)debuggableAttrs[0]).IsJITTrackingEnabled)
                    {
                        debuggableCnt1++;
                    }
                }
            }
            CError.WriteLine("Number of debuggable XSLT stylesheets loaded (before load): " + debuggableCnt1);

            XslCompiledTransform xct1 = new XslCompiledTransform(true);
            xct1.Load(XmlReader.Create(new StringReader(stylesheet)), settings, resolver);

            XslCompiledTransform xct2 = new XslCompiledTransform(false);
            xct2.Load(XmlReader.Create(new StringReader(stylesheet)), settings, resolver);

            int debuggableCnt2 = 0;
            foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                string asmName = asm.GetName().Name;
                if (asmName.StartsWith("System.Xml.Xsl.CompiledQuery.", StringComparison.Ordinal))
                {
                    object[] debuggableAttrs = asm.GetCustomAttributes(typeof(DebuggableAttribute), false);
                    if (debuggableAttrs.Length > 0 && ((DebuggableAttribute)debuggableAttrs[0]).IsJITTrackingEnabled)
                    {
                        debuggableCnt2++;
                    }
                }
            }
            CError.WriteLine("Number of debuggable XSLT stylesheets loaded (after load): " + debuggableCnt2);

            if (debuggableCnt2 - debuggableCnt1 == 1)
                return TEST_PASS;
            else
                return TEST_FAIL;
        }

        [Variation("Bug412703 - Off-by-one errors for XSLT loading error column")]
        public int RegressionTest3()
        {
            try
            {
                LoadXSL("bug370868.xsl");
            }
            catch (System.Xml.Xsl.XsltException e)
            {
                // Should be 3,2
                if (e.LineNumber == 3 && e.LinePosition == 2)
                    return TEST_PASS;
                else
                    CError.WriteLine("412703: LineNumber and position were incorrect. Expected {0}, {1}. Actual {2}, {3}", 3, 2, e.LineNumber, e.LinePosition);
            }
            return TEST_FAIL;
        }

        [Variation("Bug423641 - XslCompiledTransform.Load() [retail] throws a NullReferenceException when scripts are prohibited")]
        public int RegressionTest4()
        {
            XslCompiledTransform xslt = new XslCompiledTransform();
            // Should not throw
            xslt.Load(FullFilePath("XSLTFilewithscript.xslt"), XsltSettings.Default, new XmlUrlResolver());
            return TEST_PASS;
        }

        [Variation("Bug423641 - XslCompiledTransform.Load() [debug] throws a NullReferenceException when scripts are prohibited")]
        public int RegressionTest5()
        {
            XslCompiledTransform xslt = new XslCompiledTransform(true);
            // Should not throw
            xslt.Load(FullFilePath("XSLTFilewithscript.xslt"), XsltSettings.Default, new XmlUrlResolver());
            return TEST_PASS;
        }

        // TODO: DENY is deprecated
        //[Variation("Bug429365 & SRZ070601000889 - XslCompiledTransform.Transform() fails on x64 when caller does not have UnmanagedCode privilege")]
        //public int RegressionTest6()
        //{
        //    // Should not throw
        //    ExecuteTransform();
        //    return TEST_PASS;
        //}

        [Variation("Bug469781 - Replace shouldn't relax original type 'assertion failure'")]
        public int RegressionTest7()
        {
            string xslString = "<xsl:stylesheet version=\"1.0\" xmlns:xsl=\"http://www.w3.org/1999/XSL/Transform\" xmlns:user=\"urn:user\">"
                + "<xsl:template match=\"/\">"
                + "<xsl:variable name=\"foo\"/>"
                + "<xsl:for-each select=\"user:func()\">"
                + "<xsl:variable name=\"bar\"/>"
                + "<xsl:if test=\"self::node()[0]\"/>"
                + "</xsl:for-each>"
                + "</xsl:template>"
                + "</xsl:stylesheet>";

            try
            {
                XmlReader r = XmlReader.Create(new StringReader(xslString));
                XslCompiledTransform trans = new XslCompiledTransform();
                trans.Load(r);
            }
            catch (XsltException e)
            {
                CError.WriteLine(e);
                return TEST_FAIL;
            }
            return TEST_PASS;
        }

        [Variation("Bug737816 - Dynamic method will have declaring type == null")]
        public int RegressionTest8()
        {
            try
            {
                DynamicMethod hello = new DynamicMethod("Hello",
                                typeof(int),
                                new Type[] { },
                                typeof(string).Module);

                ILGenerator il = hello.GetILGenerator(256);
                il.Emit(OpCodes.Ret);

                // Load into XslCompiledTransform
                var xslt = new XslCompiledTransform();
                xslt.Load(hello, new byte[] { }, new Type[] { });

                // Run the transformation
                xslt.Transform(XmlReader.Create(new StringReader("<Root><Price>9.50</Price></Root>")), (XsltArgumentList)null, Console.Out);
            }
            catch (ArgumentException)
            {
                return TEST_PASS;
            }
            catch (Exception e)
            {
                CError.WriteLine(e);
            }

            return TEST_FAIL;
        }

        //[SecurityPermission(SecurityAction.Deny, UnmanagedCode = true)]
        //private void ExecuteTransform()
        //{
        //    XslCompiledTransform xslCompiledTransform = new XslCompiledTransform();
        //    xslCompiledTransform.Load(FullFilePath("xslt_sortnames.xsl"));

        //    MemoryStream ms = new MemoryStream();
        //    StreamReader stmRead = new StreamReader(ms, true);
        //    xslCompiledTransform.Transform(FullFilePath("xslt_names.xml"), null, ms);
        //    ms.Seek(0, new SeekOrigin());
        //    string foo = stmRead.ReadToEnd();
        //    CError.WriteLine(foo);
        //}
    }

    internal class MyArrayIterator : XPathNodeIterator
    {
        protected ArrayList array;
        protected int index;

        public MyArrayIterator(ArrayList array)
        {
            this.array = array;
            this.index = 0;
        }

        public MyArrayIterator(MyArrayIterator it)
        {
            this.array = it.array;
            this.index = it.index;
        }

        public override XPathNodeIterator Clone()
        {
            return new MyArrayIterator(this);
        }

        public override bool MoveNext()
        {
            if (index < array.Count)
            {
                index++;
                return true;
            }
            return false;
        }

        public override XPathNavigator Current
        {
            get
            {
                return (index > 0) ? (XPathNavigator)array[index - 1] : null;
            }
        }

        public override int CurrentPosition
        {
            get
            {
                return index;
            }
        }

        public override int Count
        {
            get
            {
                return array.Count;
            }
        }

        public void Reset()
        {
            index = 0;
        }

        // BUGBUG: DCR 104760
        public override IEnumerator GetEnumerator()
        {
            MyArrayIterator it = (MyArrayIterator)this.Clone();
            it.Reset();
            return it.array.GetEnumerator();
        }
    }

    internal class MyXsltExtension
    {
        public XPathNodeIterator distinct(XPathNodeIterator nodeset)
        {
            Hashtable nodelist = new Hashtable();
            while (nodeset.MoveNext())
            {
                if (!nodelist.Contains(nodeset.Current.Value))
                {
                    nodelist.Add(nodeset.Current.Value, nodeset.Current);
                }
            }
            return new MyArrayIterator(new ArrayList(nodelist.Values));
        }
    }
}