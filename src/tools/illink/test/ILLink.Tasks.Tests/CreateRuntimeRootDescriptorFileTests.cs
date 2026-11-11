// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Xml.Linq;
using Microsoft.Build.Utilities;
using Xunit;

namespace ILLink.Tasks.Tests
{
    public class CreateRuntimeRootDescriptorFileTests
    {
        [Fact]
        public void TestCoreLibClassGen()
        {
            File.WriteAllLines("corelib.h", new string[] {
                "#ifndef TESTDEF",
                "#define TESTDEF",
                "#endif",
                "DEFINE_CLASS(TESTCLASS, TestNS, TestClass)",
                "DEFINE_METHOD(TESTCLASS, TESTMETHOD, TestMethod, 0)",
                "#ifdef FEATURE_ON",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODIFON, TestMethodIfOn, 1)",
                "#endif",
                "#ifdef FEATURE_OFF",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODIFOFF, TestMethodIfOff, 2)",
                "#endif",
                "#ifndef FEATURE_BOTH // Comment",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODIFBOTH, TestMethodIfBoth, 3)",
                "#if FOR_ILLINK",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODIFBOTH, TestMethodIfBothForILLink, 3)",
                "#endif",
                "#else",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODIFNOTBOTH, TestMethodIfNotBoth, 4)",
                "#if FOR_ILLINK",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODIFNOTBOTH, TestMethodIfNotBothForILLink, 4)",
                "#endif",
                "#endif // FEATURE_BOTH",
                "#if FOR_ILLINK",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODFORILLINK, TestMethodForILLink, 5)",
                "#endif",
                "DEFINE_CLASS(NESTEDTESTCLASS, TestNS, TestClass+Nested)",
                "DEFINE_METHOD(NESTEDTESTCLASS, TESTMETHOD, TestMethod, 0)"
                });

            File.WriteAllText("namespace.h",
                "#define g_TestNS \"TestNS\"" + Environment.NewLine);

            File.WriteAllLines("cortypeinfo.h", Array.Empty<string>());

            File.WriteAllLines("rexcep.h", new string[] {
                "DEFINE_EXCEPTION(g_TestNS, TestAlwaysException, false, C)",
                "#ifdef FEATURE_ON",
                "DEFINE_EXCEPTION(g_TestNS, TestFeatureOnException, false, C)",
                "#endif",
                "#ifdef FEATURE_OFF",
                "DEFINE_EXCEPTION(g_TestNS, TestFeatureOffException, false, C)",
                "#endif"
                });

            XElement existingAssembly = new XElement("assembly", new XAttribute("fullname", "testassembly"),
                    new XComment("Existing content"));
            XElement existingContent = new XElement("linker", existingAssembly);
            new XDocument(existingContent).Save("Test.ILLink.Descriptors.Combined.xml");

            var task = new CreateRuntimeRootILLinkDescriptorFile()
            {
                NamespaceFilePath = new TaskItem("namespace.h"),
                MscorlibFilePath = new TaskItem("corelib.h"),
                CortypeFilePath = new TaskItem("cortypeinfo.h"),
                RexcepFilePath = new TaskItem("rexcep.h"),
                ILLinkTrimXmlFilePath = new TaskItem("Test.ILLink.Descriptors.Combined.xml"),
                DefineConstants = new TaskItem[] {
                    new TaskItem("FOR_ILLINK"),
                    new TaskItem("_TEST"),
                    new TaskItem("FEATURE_ON"),
                    new TaskItem("FEATURE_BOTH")
                },
                RuntimeRootDescriptorFilePath = new TaskItem("Test.ILLink.Descriptors.xml")
            };

            Assert.True(task.Execute());

            XDocument output = XDocument.Load("Test.ILLink.Descriptors.xml");
            string expectedXml = new XElement("linker",
                new XElement("assembly",
                    existingAssembly.Attributes(),
                    existingAssembly.Nodes(),
                    new XElement("type", new XAttribute("fullname", "TestNS.TestClass"),
                        new XElement("method", new XAttribute("name", "TestMethod")),
                        new XElement("method", new XAttribute("name", "TestMethodIfOn")),
                        new XElement("method", new XAttribute("name", "TestMethodIfNotBoth")),
                        new XElement("method", new XAttribute("name", "TestMethodIfNotBothForILLink")),
                        new XElement("method", new XAttribute("name", "TestMethodForILLink"))),
                    new XElement("type", new XAttribute("fullname", "TestNS.TestClass/Nested"),
                        new XElement("method", new XAttribute("name", "TestMethod"))),
                    new XElement("type", new XAttribute("fullname", "TestNS.TestAlwaysException"),
                        new XElement("method", new XAttribute("name", ".ctor"))),
                    new XElement("type", new XAttribute("fullname", "TestNS.TestFeatureOnException"),
                        new XElement("method", new XAttribute("name", ".ctor")))
                    )).ToString();
            Assert.Equal(expectedXml, output.Root.ToString());
        }

        [Fact]
        public void TestCoreLibClassGenWithFeatureSwitch()
        {
            File.WriteAllLines("corelib.h", new string[] {
                "#ifndef TESTDEF",
                "#define TESTDEF",
                "#endif",
                "BEGIN_ILLINK_FEATURE_SWITCH(TestFeatureName, true, true)",
                "DEFINE_CLASS(TESTCLASS, TestNS, TestClass)",
                "DEFINE_METHOD(TESTCLASS, TESTMETHOD, TestMethod, 0)",
                "#ifdef FEATURE_ON",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODIFON, TestMethodIfOn, 1)",
                "#endif",
                "#ifdef FEATURE_OFF",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODIFOFF, TestMethodIfOff, 2)",
                "#endif",
                "#ifndef FEATURE_BOTH // Comment",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODIFBOTH, TestMethodIfBoth, 3)",
                "#if FOR_ILLINK",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODIFBOTH, TestMethodIfBothForILLink, 3)",
                "#endif",
                "#else",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODIFNOTBOTH, TestMethodIfNotBoth, 4)",
                "#if FOR_ILLINK",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODIFNOTBOTH, TestMethodIfNotBothForILLink, 4)",
                "#endif",
                "#endif // FEATURE_BOTH",
                "#if FOR_ILLINK",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODFORILLINK, TestMethodForILLink, 5)",
                "#endif",
                "END_ILLINK_FEATURE_SWITCH()",
                "BEGIN_ILLINK_FEATURE_SWITCH(TestFeature2Name, false, false)",
                "#ifdef FEATURE_OFF",
                "DEFINE_CLASS(TESTCLASS)",
                "DEFINE_METHOD(TESTCLASS, TESTMETHODFEATURE2, TestMethodFeature2, 6)",
                "#endif",
                "END_ILLINK_FEATURE_SWITCH()"
                });

            File.WriteAllText("namespace.h",
                "#define g_TestNS \"TestNS\"" + Environment.NewLine);

            File.WriteAllLines("cortypeinfo.h", Array.Empty<string>());

            File.WriteAllLines("rexcep.h", new string[] {
                "DEFINE_EXCEPTION(g_TestNS, TestAlwaysException, false, C)",
                "#ifdef FEATURE_ON",
                "DEFINE_EXCEPTION(g_TestNS, TestFeatureOnException, false, C)",
                "#endif",
                "#ifdef FEATURE_OFF",
                "DEFINE_EXCEPTION(g_TestNS, TestFeatureOffException, false, C)",
                "#endif"
                });

            XElement existingAssembly = new XElement("assembly", new XAttribute("fullname", "testassembly"),
                    new XComment("Existing content"));
            XElement existingContent = new XElement("linker", existingAssembly);
            new XDocument(existingContent).Save("Test.ILLink.Descriptors.Combined.xml");

            var task = new CreateRuntimeRootILLinkDescriptorFile()
            {
                NamespaceFilePath = new TaskItem("namespace.h"),
                MscorlibFilePath = new TaskItem("corelib.h"),
                CortypeFilePath = new TaskItem("cortypeinfo.h"),
                RexcepFilePath = new TaskItem("rexcep.h"),
                ILLinkTrimXmlFilePath = new TaskItem("Test.ILLink.Descriptors.Combined.xml"),
                DefineConstants = new TaskItem[] {
                    new TaskItem("FOR_ILLINK"),
                    new TaskItem("_TEST"),
                    new TaskItem("FEATURE_ON"),
                    new TaskItem("FEATURE_BOTH")
                },
                RuntimeRootDescriptorFilePath = new TaskItem("Test.ILLink.Descriptors.xml")
            };

            Assert.True(task.Execute());

            XDocument output = XDocument.Load("Test.ILLink.Descriptors.xml");
            string expectedXml = new XElement("linker",
                new XElement("assembly",
                    existingAssembly.Attributes(),
                    existingAssembly.Nodes(),
                    new XElement("type", new XAttribute("fullname", "TestNS.TestAlwaysException"),
                        new XElement("method", new XAttribute("name", ".ctor"))),
                    new XElement("type", new XAttribute("fullname", "TestNS.TestFeatureOnException"),
                        new XElement("method", new XAttribute("name", ".ctor")))
                    ),
                new XElement("assembly", new XAttribute("fullname", "System.Private.CoreLib"), new XAttribute("feature", "TestFeatureName"), new XAttribute("featurevalue", "true"), new XAttribute("featuredefault", "true"),
                    new XElement("type", new XAttribute("fullname", "TestNS.TestClass"),
                        new XElement("method", new XAttribute("name", "TestMethod")),
                        new XElement("method", new XAttribute("name", "TestMethodIfOn")),
                        new XElement("method", new XAttribute("name", "TestMethodIfNotBoth")),
                        new XElement("method", new XAttribute("name", "TestMethodIfNotBothForILLink")),
                        new XElement("method", new XAttribute("name", "TestMethodForILLink")))
                    )
                ).ToString();
            Assert.Equal(expectedXml, output.Root.ToString());
        }

        [Fact]
        public void TestNestedFeatureSwitchesAndStandaloneMembers()
        {
            File.WriteAllLines("corelib.h", new[] {
                "DEFINE_CLASS(TESTCLASS, TestNS, TestClass)",
                "DEFINE_METHOD(TESTCLASS, ALWAYS, Always, 0)",
                "BEGIN_ILLINK_FEATURE_SWITCH(Outer, true, true)",
                "DEFINE_METHOD(TESTCLASS, OPTIONAL, Optional, 0)",
                "DEFINE_FIELD(TESTCLASS, OPTIONAL_FIELD, OptionalField)",
                "DEFINE_STATIC_SET_PROPERTY(TESTCLASS, OPTIONAL_PROPERTY, OptionalProperty, Int)",
                "BEGIN_ILLINK_FEATURE_SWITCH(Inner, false, false)",
                "DEFINE_CLASS(NESTED, TestNS, Nested)",
                "DEFINE_METHOD(NESTED, METHOD, Method, 0)",
                "END_ILLINK_FEATURE_SWITCH()",
                "DEFINE_CLASS(RESTORED, TestNS, Restored)",
                "END_ILLINK_FEATURE_SWITCH()",
                "DEFINE_CLASS(UNCONDITIONAL, TestNS, Unconditional)"
            });
            File.WriteAllText("namespace.h", "#define g_TestNS \"TestNS\"" + Environment.NewLine);
            File.WriteAllText("cortypeinfo.h", "");
            File.WriteAllText("rexcep.h", "");
            File.WriteAllText("Test.ILLink.Descriptors.Combined.xml",
                "<linker><assembly fullname=\"System.Private.CoreLib\" /></linker>");

            var task = new CreateRuntimeRootILLinkDescriptorFile
            {
                NamespaceFilePath = new TaskItem("namespace.h"),
                MscorlibFilePath = new TaskItem("corelib.h"),
                CortypeFilePath = new TaskItem("cortypeinfo.h"),
                RexcepFilePath = new TaskItem("rexcep.h"),
                ILLinkTrimXmlFilePath = new TaskItem("Test.ILLink.Descriptors.Combined.xml"),
                DefineConstants = Array.Empty<TaskItem>(),
                RuntimeRootDescriptorFilePath = new TaskItem("Test.ILLink.Descriptors.xml")
            };

            Assert.True(task.Execute());
            XDocument output = XDocument.Load("Test.ILLink.Descriptors.xml");
            string expectedXml =
                "<linker>" +
                "<assembly fullname=\"System.Private.CoreLib\">" +
                "<type fullname=\"TestNS.TestClass\"><method name=\"Always\" /></type>" +
                "<type fullname=\"TestNS.Unconditional\" preserve=\"nothing\" />" +
                "</assembly>" +
                "<assembly fullname=\"System.Private.CoreLib\" feature=\"Outer\" featurevalue=\"true\" featuredefault=\"true\">" +
                "<type fullname=\"TestNS.TestClass\"><field name=\"OptionalField\" /><method name=\"Optional\" /><method name=\"get_OptionalProperty\" /><method name=\"set_OptionalProperty\" /></type>" +
                "<type fullname=\"TestNS.Restored\" preserve=\"nothing\" />" +
                "</assembly>" +
                "<assembly fullname=\"System.Private.CoreLib\" feature=\"Inner\" featurevalue=\"false\" featuredefault=\"false\">" +
                "<type fullname=\"TestNS.Nested\" feature=\"Outer\" featurevalue=\"true\" featuredefault=\"true\"><method name=\"Method\" /></type>" +
                "</assembly>" +
                "</linker>";
            Assert.Equal(XDocument.Parse(expectedXml).ToString(), output.ToString());
        }

        [Theory]
        [InlineData("END_ILLINK_FEATURE_SWITCH()")]
        [InlineData("BEGIN_ILLINK_FEATURE_SWITCH(Outer, true, true)")]
        [InlineData("BEGIN_ILLINK_FEATURE_SWITCH(Outer, true)")]
        [InlineData("BEGIN_ILLINK_FEATURE_SWITCH(Outer, true, true)\nBEGIN_ILLINK_FEATURE_SWITCH(Inner, true, true)\nBEGIN_ILLINK_FEATURE_SWITCH(TooDeep, true, true)")]
        public void TestInvalidFeatureSwitchGuards(string guards)
        {
            File.WriteAllText("corelib.h", guards);
            File.WriteAllText("namespace.h", "");
            File.WriteAllText("cortypeinfo.h", "");
            File.WriteAllText("rexcep.h", "");
            File.WriteAllText("Test.ILLink.Descriptors.Combined.xml",
                "<linker><assembly fullname=\"System.Private.CoreLib\" /></linker>");
            var engine = new MockBuildEngine();
            var task = new CreateRuntimeRootILLinkDescriptorFile
            {
                BuildEngine = engine,
                NamespaceFilePath = new TaskItem("namespace.h"),
                MscorlibFilePath = new TaskItem("corelib.h"),
                CortypeFilePath = new TaskItem("cortypeinfo.h"),
                RexcepFilePath = new TaskItem("rexcep.h"),
                ILLinkTrimXmlFilePath = new TaskItem("Test.ILLink.Descriptors.Combined.xml"),
                RuntimeRootDescriptorFilePath = new TaskItem("Test.ILLink.Descriptors.xml")
            };

            Assert.False(task.Execute());
            Assert.NotEmpty(engine.Errors);
        }
    }
}
