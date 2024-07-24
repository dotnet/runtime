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
		public void TestCoreLibClassGen ()
		{
			File.WriteAllLines ("corelib.h", new string[] {
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

			File.WriteAllText ("namespace.h",
				"#define g_TestNS \"TestNS\"" + Environment.NewLine);

			File.WriteAllLines ("cortypeinfo.h", Array.Empty<string> ());

			File.WriteAllLines ("rexcep.h", new string[] {
				"DEFINE_EXCEPTION(g_TestNS, TestAlwaysException, false, C)",
				"#ifdef FEATURE_ON",
				"DEFINE_EXCEPTION(g_TestNS, TestFeatureOnException, false, C)",
				"#endif",
				"#ifdef FEATURE_OFF",
				"DEFINE_EXCEPTION(g_TestNS, TestFeatureOffException, false, C)",
				"#endif"
				});

			XElement existingAssembly = new XElement ("assembly", new XAttribute ("fullname", "testassembly"),
					new XComment ("Existing content"));
			XElement existingContent = new XElement ("linker", existingAssembly);
			new XDocument (existingContent).Save ("Test.ILLink.Descriptors.Combined.xml");

			var task = new CreateRuntimeRootILLinkDescriptorFile () {
				NamespaceFilePath = new TaskItem ("namespace.h"),
				MscorlibFilePath = new TaskItem ("corelib.h"),
				CortypeFilePath = new TaskItem ("cortypeinfo.h"),
				RexcepFilePath = new TaskItem ("rexcep.h"),
				ILLinkTrimXmlFilePath = new TaskItem ("Test.ILLink.Descriptors.Combined.xml"),
				DefineConstants = new TaskItem[] {
					new TaskItem("FOR_ILLINK"),
					new TaskItem("_TEST"),
					new TaskItem("FEATURE_ON"),
					new TaskItem("FEATURE_BOTH")
				},
				RuntimeRootDescriptorFilePath = new TaskItem ("Test.ILLink.Descriptors.xml")
			};

			Assert.True (task.Execute ());

			XDocument output = XDocument.Load ("Test.ILLink.Descriptors.xml");
			string expectedXml = new XElement ("linker",
				new XElement ("assembly",
					existingAssembly.Attributes (),
					existingAssembly.Nodes (),
					new XElement ("type", new XAttribute ("fullname", "TestNS.TestClass"),
						new XElement ("method", new XAttribute ("name", "TestMethod")),
						new XElement ("method", new XAttribute ("name", "TestMethodIfOn")),
						new XElement ("method", new XAttribute ("name", "TestMethodIfNotBoth")),
						new XElement ("method", new XAttribute ("name", "TestMethodIfNotBothForILLink")),
						new XElement ("method", new XAttribute ("name", "TestMethodForILLink"))),
					new XElement ("type", new XAttribute ("fullname", "TestNS.TestAlwaysException"),
						new XElement ("method", new XAttribute ("name", ".ctor"))),
					new XElement ("type", new XAttribute ("fullname", "TestNS.TestFeatureOnException"),
						new XElement ("method", new XAttribute ("name", ".ctor"))),
					new XElement ("type", new XAttribute("fullname", "TestNS.TestClass/Nested"),
						new XElement ("method", new XAttribute("name", "TestMethod")))
					)).ToString ();
			Assert.Equal (expectedXml, output.Root.ToString ());
		}

		[Fact]
		public void TestCoreLibClassGenWithFeatureSwitch ()
		{
			File.WriteAllLines ("corelib.h", new string[] {
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

			File.WriteAllText ("namespace.h",
				"#define g_TestNS \"TestNS\"" + Environment.NewLine);

			File.WriteAllLines ("cortypeinfo.h", Array.Empty<string> ());

			File.WriteAllLines ("rexcep.h", new string[] {
				"DEFINE_EXCEPTION(g_TestNS, TestAlwaysException, false, C)",
				"#ifdef FEATURE_ON",
				"DEFINE_EXCEPTION(g_TestNS, TestFeatureOnException, false, C)",
				"#endif",
				"#ifdef FEATURE_OFF",
				"DEFINE_EXCEPTION(g_TestNS, TestFeatureOffException, false, C)",
				"#endif"
				});

			XElement existingAssembly = new XElement ("assembly", new XAttribute ("fullname", "testassembly"),
					new XComment ("Existing content"));
			XElement existingContent = new XElement ("linker", existingAssembly);
			new XDocument (existingContent).Save ("Test.ILLink.Descriptors.Combined.xml");

			var task = new CreateRuntimeRootILLinkDescriptorFile () {
				NamespaceFilePath = new TaskItem ("namespace.h"),
				MscorlibFilePath = new TaskItem ("corelib.h"),
				CortypeFilePath = new TaskItem ("cortypeinfo.h"),
				RexcepFilePath = new TaskItem ("rexcep.h"),
				ILLinkTrimXmlFilePath = new TaskItem ("Test.ILLink.Descriptors.Combined.xml"),
				DefineConstants = new TaskItem[] {
					new TaskItem("FOR_ILLINK"),
					new TaskItem("_TEST"),
					new TaskItem("FEATURE_ON"),
					new TaskItem("FEATURE_BOTH")
				},
				RuntimeRootDescriptorFilePath = new TaskItem ("Test.ILLink.Descriptors.xml")
			};

			Assert.True (task.Execute ());

			XDocument output = XDocument.Load ("Test.ILLink.Descriptors.xml");
			string expectedXml = new XElement ("linker",
				new XElement ("assembly",
					existingAssembly.Attributes (),
					existingAssembly.Nodes (),
					new XElement ("type", new XAttribute ("fullname", "TestNS.TestAlwaysException"),
						new XElement ("method", new XAttribute ("name", ".ctor"))),
					new XElement ("type", new XAttribute ("fullname", "TestNS.TestFeatureOnException"),
						new XElement ("method", new XAttribute ("name", ".ctor")))
					),
				new XElement ("assembly", new XAttribute ("fullname", "System.Private.CoreLib"), new XAttribute ("feature", "TestFeatureName"), new XAttribute ("featurevalue", "true"), new XAttribute ("featuredefault", "true"),
					new XElement ("type", new XAttribute ("fullname", "TestNS.TestClass"),
						new XElement ("method", new XAttribute ("name", "TestMethod")),
						new XElement ("method", new XAttribute ("name", "TestMethodIfOn")),
						new XElement ("method", new XAttribute ("name", "TestMethodIfNotBoth")),
						new XElement ("method", new XAttribute ("name", "TestMethodIfNotBothForILLink")),
						new XElement ("method", new XAttribute ("name", "TestMethodForILLink")))
					)
				).ToString ();
			Assert.Equal (expectedXml, output.Root.ToString ());
		}

	}
}
