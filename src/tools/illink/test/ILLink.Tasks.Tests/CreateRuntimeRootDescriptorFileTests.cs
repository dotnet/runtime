// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Utilities;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Linker;
using Xunit;

namespace ILLink.Tasks.Tests
{
    public class CreateRuntimeRootDescriptorFileTests
    {
        [Theory]
        [InlineData(false, false, false)]
        [InlineData(false, false, true)]
        [InlineData(false, true, false)]
        [InlineData(false, true, true)]
        [InlineData(true, false, false)]
        [InlineData(true, false, true)]
        [InlineData(true, true, false)]
        [InlineData(true, true, true)]
        public void ConditionalAliasesMergeByManagedIdentity(bool mandatoryAlias, bool mandatoryLayout, bool layoutFirst)
        {
            using var test = new DescriptorTest();
            string declaration = mandatoryAlias ? "DEFINE_CLASS" : "DEFINE_CLASS_OPTIONAL";
            string layoutDeclaration = mandatoryLayout ? "DEFINE_CLASS_U" : "DEFINE_CLASS_U_OPTIONAL";
            string layout = $"{layoutDeclaration}(TestNS, Candidate+Nested, NativeCandidate)";
            string type = $"{declaration}(CANDIDATE, TestNS, Candidate+Nested)";
            XDocument output = test.Generate(new[] {
                layoutFirst ? layout : type,
                layoutFirst ? type : layout,
                "DEFINE_CLASS_OPTIONAL(ALIAS, TestNS, Candidate+Nested)",
                "DEFINE_FIELD(ALIAS, VALUE, _value)",
                "DEFINE_METHOD(CANDIDATE, CALLBACK, Callback, NoSig)",
                "DEFINE_METHOD(ALIAS, CALLBACK_OVERLOAD, Callback, OtherSig)",
                "DEFINE_CLASS(MANDATORY, TestNS, Mandatory)"
            });

            XElement candidate = output.Root.Element("assembly").Elements("type").Single(t => (string)t.Attribute("fullname") == "TestNS.Candidate/Nested");
            Assert.Equal(mandatoryAlias || mandatoryLayout ? null : "false", (string)candidate.Attribute("required"));
            Assert.Equal("fields", (string)candidate.Attribute("preserve"));
            Assert.Empty(candidate.Elements("field"));
            Assert.Equal("Callback", (string)Assert.Single(candidate.Elements("method")).Attribute("name"));
            XElement mandatory = output.Root.Element("assembly").Elements("type").Single(t => (string)t.Attribute("fullname") == "TestNS.Mandatory");
            Assert.Null(mandatory.Attribute("required"));
            Assert.Equal("nothing", (string)mandatory.Attribute("preserve"));
        }

        [Fact]
        public void ConditionalDeclarationsPreserveFeatureSwitchBoundaries()
        {
            using var test = new DescriptorTest();
            XDocument output = test.Generate(new[] {
                "DEFINE_CLASS_OPTIONAL(CANDIDATE, TestNS, Candidate)",
                "DEFINE_FIELD(CANDIDATE, VALUE, _value)",
                "BEGIN_ILLINK_FEATURE_SWITCH(TestFeature, true, true)",
                "DEFINE_CLASS(ALIAS, TestNS, Candidate)",
                "DEFINE_METHOD(ALIAS, CALLBACK, Callback, NoSig)",
                "END_ILLINK_FEATURE_SWITCH()"
            });

            XElement unconditional = output.Root.Elements("assembly").Single(a => a.Attribute("feature") is null).Element("type");
            Assert.Equal("false", (string)unconditional.Attribute("required"));
            Assert.Equal("fields", (string)unconditional.Attribute("preserve"));
            Assert.Empty(unconditional.Elements());
            XElement feature = output.Root.Elements("assembly").Single(a => a.Attribute("feature") is not null);
            Assert.Equal("TestFeature", (string)feature.Attribute("feature"));
            Assert.Equal("true", (string)feature.Attribute("featuredefault"));
            Assert.Null(feature.Element("type").Attribute("required"));
            Assert.Equal("Callback", (string)feature.Element("type").Element("method").Attribute("name"));
        }

        [Fact]
        public void CoreTypesAndExceptionsDominateConditionalAliases()
        {
            using var test = new DescriptorTest();
            XDocument output = test.Generate(new[] {
                "DEFINE_CLASS_OPTIONAL(CANDIDATE, TestNS, Candidate)",
                "DEFINE_CLASS_OPTIONAL(EXCEPTION, TestNS, TestException)"
            }, "TYPEINFO(ELEMENT_TYPE_OBJECT, \"TestNS\", \"Candidate\", 0)",
                "DEFINE_EXCEPTION(g_TestNS, TestException, false, C)");

            Assert.All(output.Root.Element("assembly").Elements("type"), t => Assert.Null(t.Attribute("required")));
            Assert.Equal(".ctor", (string)output.Root.Element("assembly").Elements("type").Last().Element("method").Attribute("name"));
        }

        [Theory]
        [InlineData(false, false)]
        [InlineData(true, false)]
        [InlineData(false, true)]
        [InlineData(true, true)]
        public void ConditionalTypeAndMemberPolicy(bool optional, bool hasMembers)
        {
            using var test = new DescriptorTest();
            string declaration = optional ? "DEFINE_CLASS_OPTIONAL" : "DEFINE_CLASS";
            XDocument output = test.Generate(new[] {
                $"{declaration}(CANDIDATE, TestNS, Candidate)",
                "#ifdef INACTIVE",
                "DEFINE_CLASS(INACTIVE_ALIAS, TestNS, Candidate)",
                "#endif",
                hasMembers ? "DEFINE_FIELD(CANDIDATE, VALUE, _value)" : "",
                hasMembers ? "DEFINE_METHOD(CANDIDATE, CALLBACK, Callback, NoSig)" : ""
            });

            XElement candidate = Assert.Single(output.Root.Element("assembly").Elements("type"));
            Assert.Equal(optional ? "false" : null, (string)candidate.Attribute("required"));
            Assert.Equal(!hasMembers ? "nothing" : optional ? "fields" : null, (string)candidate.Attribute("preserve"));
            Assert.Equal(hasMembers && !optional ? 1 : 0, candidate.Elements("field").Count());
            Assert.Equal(hasMembers ? 1 : 0, candidate.Elements("method").Count());
        }

        [Theory]
        [InlineData(false, false, false, false)]
        [InlineData(true, false, false, false)]
        [InlineData(false, true, false, false)]
        [InlineData(true, true, false, false)]
        [InlineData(false, false, true, false)]
        [InlineData(false, true, true, false)]
        [InlineData(false, false, false, true)]
        [InlineData(true, false, false, true)]
        [InlineData(false, true, false, true)]
        [InlineData(true, true, false, true)]
        [InlineData(false, true, true, true)]
        public void LinkerDefersConditionalFieldsAndMethods(bool reachable, bool nested, bool featureEnabled, bool generic)
        {
            using var test = new DescriptorTest();
            string containerName = generic ? "Container`1" : "Container";
            string candidateName = generic && !nested ? "Candidate`1" : "Candidate";
            string name = nested ? $"{containerName}+{candidateName}" : candidateName;
            XDocument descriptor = test.Generate(new[] {
                $"DEFINE_CLASS_OPTIONAL(CANDIDATE, TestNS, {name})",
                $"DEFINE_CLASS_U_OPTIONAL(TestNS, {name}, NativeCandidate)",
                "DEFINE_FIELD(CANDIDATE, VALUE, _value)",
                "DEFINE_METHOD(CANDIDATE, CALLBACK, Callback, NoSig)",
                "BEGIN_ILLINK_FEATURE_SWITCH(TestFeature, true, true)",
                $"DEFINE_CLASS(ALIAS, TestNS, {name})",
                "END_ILLINK_FEATURE_SWITCH()"
            });
            // Feature groups target CoreLib in production; this fixture lives in Input.
            descriptor.Root.Elements("assembly").Single(a => a.Attribute("feature") is not null).SetAttributeValue("fullname", "Input");
            descriptor.Save(test.DescriptorPath);

            string input = Path.Combine(test.DirectoryPath, "Input.dll");
            using (AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition("Input", new Version(1, 0)), "Input", ModuleKind.Console))
            {
                ModuleDefinition module = assembly.MainModule;
                TypeReference objectType = module.ImportReference(typeof(object));
                TypeReference voidType = module.ImportReference(typeof(void));
                var candidate = new TypeDefinition("TestNS", candidateName, TypeAttributes.Public, objectType);
                if (generic)
                    candidate.GenericParameters.Add(new GenericParameter("T", candidate));
                if (nested)
                {
                    var container = new TypeDefinition("TestNS", containerName, TypeAttributes.Public, objectType);
                    if (generic)
                        container.GenericParameters.Add(new GenericParameter("T", container));
                    module.Types.Add(container);
                    candidate.Namespace = "";
                    candidate.Attributes = TypeAttributes.NestedPublic;
                    container.NestedTypes.Add(candidate);
                }
                else
                {
                    module.Types.Add(candidate);
                }
                candidate.Fields.Add(new FieldDefinition("_value", FieldAttributes.Private, objectType));
                candidate.Fields.Add(new FieldDefinition("_layoutOnly", FieldAttributes.Private, module.ImportReference(typeof(IntPtr))));
                foreach (bool overload in new[] { false, true })
                {
                    var callback = new MethodDefinition("Callback", MethodAttributes.Public | MethodAttributes.Static, voidType);
                    if (overload)
                        callback.Parameters.Add(new ParameterDefinition(objectType));
                    callback.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    candidate.Methods.Add(callback);
                }
                var program = new TypeDefinition("TestNS", "Program", TypeAttributes.Public, objectType);
                module.Types.Add(program);
                var main = new MethodDefinition("Main", MethodAttributes.Public | MethodAttributes.Static, voidType);
                if (reachable)
                {
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, candidate));
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
                }
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                program.Methods.Add(main);
                module.EntryPoint = main;
                assembly.Write(input);
            }

            string output = Path.Combine(test.DirectoryPath, "linked");
            using var driver = new Driver(new Queue<string>(new[] {
                "-a", "Input", "entrypoint", "-reference", input, "-x", test.DescriptorPath, "-out", output,
                "-reference", typeof(object).Assembly.Location,
                "--feature", "TestFeature", featureEnabled ? "true" : "false",
                "--action", "skip", "System.Private.CoreLib"
            }));
            var logger = new MockDriver.CustomLogger();
            Assert.True(driver.Run(logger) == 0, string.Join(Environment.NewLine, logger.Messages));
            using AssemblyDefinition linked = AssemblyDefinition.ReadAssembly(Path.Combine(output, "Input.dll"));
            TypeDefinition retained = nested
                ? linked.MainModule.GetType($"TestNS.{containerName}")?.NestedTypes.SingleOrDefault(t => t.Name == candidateName)
                : linked.MainModule.GetType($"TestNS.{candidateName}");
            if (reachable || featureEnabled)
            {
                Assert.NotNull(retained);
                Assert.Equal(new[] { "_value", "_layoutOnly" }, retained.Fields.Select(f => f.Name));
                Assert.Equal(2, retained.Methods.Count(m => m.Name == "Callback"));
            }
            else
            {
                Assert.Null(retained);
            }
        }

        sealed class DescriptorTest : IDisposable
        {
            public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            public string DescriptorPath => Path.Combine(DirectoryPath, "descriptor.xml");

            public DescriptorTest() => Directory.CreateDirectory(DirectoryPath);

            public XDocument Generate(string[] declarations, string coreTypes = "", string exceptions = "")
            {
                File.WriteAllLines(Path.Combine(DirectoryPath, "corelib.h"), declarations);
                File.WriteAllText(Path.Combine(DirectoryPath, "namespace.h"), "#define g_TestNS \"TestNS\"" + Environment.NewLine);
                File.WriteAllText(Path.Combine(DirectoryPath, "cortypeinfo.h"), coreTypes);
                File.WriteAllText(Path.Combine(DirectoryPath, "rexcep.h"), exceptions);
                string template = Path.Combine(DirectoryPath, "template.xml");
                File.WriteAllText(template, "<linker><assembly fullname=\"Input\" /></linker>");
                var task = new CreateRuntimeRootILLinkDescriptorFile {
                    NamespaceFilePath = new TaskItem(Path.Combine(DirectoryPath, "namespace.h")),
                    MscorlibFilePath = new TaskItem(Path.Combine(DirectoryPath, "corelib.h")),
                    CortypeFilePath = new TaskItem(Path.Combine(DirectoryPath, "cortypeinfo.h")),
                    RexcepFilePath = new TaskItem(Path.Combine(DirectoryPath, "rexcep.h")),
                    ILLinkTrimXmlFilePath = new TaskItem(template),
                    RuntimeRootDescriptorFilePath = new TaskItem(DescriptorPath)
                };
                Assert.True(task.Execute());
                return XDocument.Load(DescriptorPath);
            }

            public void Dispose() => Directory.Delete(DirectoryPath, recursive: true);
        }

        [Fact]
        public void ReadsNestedNamespaceDefinitions()
        {
            Dictionary<string, string> namespaces = CreateRuntimeRootILLinkDescriptorFile.ReadNamespaces(new[] {
                "#define g_SystemNS \"System\"",
                "#define g_RuntimeNS g_SystemNS \".Runtime\"",
                "#define g_LoaderNS g_RuntimeNS \".Loader\" // comment"
            });
            Assert.Equal("System.Runtime.Loader", namespaces["Loader"]);
        }

        [Theory]
        [InlineData("#if defined(FEATURE_ON) || defined(FEATURE_OTHER)")]
        [InlineData("#if defined()")]
        [InlineData("#if FEATURE_ON || FEATURE_OTHER")]
        [InlineData("#elif FEATURE_OTHER")]
        [InlineData("#else")]
        [InlineData("#endif")]
        [InlineData("#ifdef FEATURE_ON\n#else\n#else\n#endif")]
        [InlineData("#ifdef FEATURE_ON")]
        public void InvalidHeaderConditionsAreDiagnosed(string source)
        {
            var errors = new List<string>();
            var tracker = new CreateRuntimeRootILLinkDescriptorFile.DefineTracker(new HashSet<string>(), errors.Add, "corelib.h");
            foreach (string line in source.Split('\n'))
                tracker.ProcessLine(line);
            tracker.Complete();
            Assert.NotEmpty(errors);
            Assert.All(errors, error => Assert.Contains("corelib.h", error));
        }

        [Theory]
        [InlineData("#if defined(DEBUGGING_SUPPORTED)", true)]
        [InlineData("#if defined(DEBUGGING_SUPPORTED)", false)]
        [InlineData("#if defined( DEBUGGING_SUPPORTED ) // Comment", true)]
        [InlineData("#if defined( DEBUGGING_SUPPORTED ) // Comment", false)]
        public void DefinedHeaderConditionsSelectTheActiveBranch(string condition, bool isDefined)
        {
            var errors = new List<string>();
            var defines = new HashSet<string>();
            if (isDefined)
                defines.Add("DEBUGGING_SUPPORTED");
            var tracker = new CreateRuntimeRootILLinkDescriptorFile.DefineTracker(defines, errors.Add, "corelib.h");

            Assert.True(tracker.ProcessLine(condition));
            Assert.Equal(isDefined, tracker.IsActiveSection);
            Assert.True(tracker.ProcessLine("#else"));
            Assert.Equal(!isDefined, tracker.IsActiveSection);
            Assert.True(tracker.ProcessLine("#endif"));
            Assert.True(tracker.IsActiveSection);
            tracker.Complete();
            Assert.Empty(errors);
        }

        [Theory]
        [InlineData(null, null)]
        [InlineData("corelib.h", "#if FEATURE_ON || FEATURE_OTHER")]
        [InlineData("cortypeinfo.h", "#if FEATURE_ON || FEATURE_OTHER")]
        [InlineData("rexcep.h", "#if FEATURE_ON || FEATURE_OTHER")]
        [InlineData("corelib.h", "#ifdef FEATURE_ON")]
        [InlineData("cortypeinfo.h", "#ifdef FEATURE_ON")]
        [InlineData("rexcep.h", "#ifdef FEATURE_ON")]
        [InlineData("corelib.h", "#ifdef FEATURE_OFF")]
        [InlineData("cortypeinfo.h", "#ifdef FEATURE_OFF")]
        [InlineData("rexcep.h", "#ifdef FEATURE_OFF")]
        public void TestCoreLibClassGen(string invalidHeader, string condition)
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

            if (invalidHeader is not null)
                File.AppendAllText(invalidHeader, $"\n{condition}\n");

            File.Delete("Test.ILLink.Descriptors.xml");
            var buildEngine = new MockBuildEngine();
            var task = new CreateRuntimeRootILLinkDescriptorFile()
            {
                BuildEngine = buildEngine,
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

            bool success = task.Execute();
            if (invalidHeader is not null)
            {
                Assert.False(success);
                Assert.Single(buildEngine.Errors);
                Assert.Contains(invalidHeader, buildEngine.Errors[0].Message);
                Assert.False(File.Exists("Test.ILLink.Descriptors.xml"));
                return;
            }

            Assert.True(success);
            Assert.Empty(buildEngine.Errors);

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

    }
}
