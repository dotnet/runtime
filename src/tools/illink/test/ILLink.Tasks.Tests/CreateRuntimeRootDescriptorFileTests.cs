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
        [Fact]
        public void ConditionalEdgesResolveForwardReferencesAndFeatureGroups()
        {
            using var test = new DescriptorTest();
            XDocument descriptor = test.Generate(new[] {
                "DEFINE_CLASS_CONDITIONAL(TARGET, TestNS, Target, METHOD, TRIGGER__ACTIVATE)",
                "DEFINE_CLASS_U_OPTIONAL(TestNS, Target, NativeTarget)",
                "DEFINE_FIELD(TARGET, LAYOUT, Layout)",
                "BEGIN_ILLINK_FEATURE_SWITCH(TestFeature, true, true)",
                "DEFINE_METHOD_CONDITIONAL(TARGET, CALLBACK, Callback, NoSig, FIELD, TRIGGER__SIGNAL)",
                "END_ILLINK_FEATURE_SWITCH()",
                "DEFINE_CLASS_OPTIONAL(TRIGGER, TestNS, Container+Trigger)",
                "DEFINE_METHOD_CONDITIONAL(TRIGGER, ACTIVATE, Activate, NoSig, CLASS, TARGET)",
                "DEFINE_FIELD_CONDITIONAL(TRIGGER, SIGNAL, Signal, CLASS, TARGET)"
            });

            XElement assembly = descriptor.Root.Elements("assembly").Single(a => a.Attribute("feature") is null);
            XElement target = assembly.Elements("type").Single(t => (string)t.Attribute("fullname") == "TestNS.Target");
            Assert.Equal("false", (string)target.Attribute("required"));
            Assert.Equal("fields", (string)target.Attribute("preserve"));
            Assert.Empty(target.Elements());
            Assert.Empty(descriptor.Descendants("conditional"));
            XDocument attributes = XDocument.Load(test.AttributesPath);
            XElement classEdge = attributes.Descendants("method").Single();
            Assert.Equal("TestNS.Container/Trigger", (string)classEdge.Parent.Attribute("fullname"));
            Assert.Equal("Activate", (string)classEdge.Attribute("name"));
            XElement attribute = Assert.Single(classEdge.Elements("attribute"));
            Assert.Equal("System.Diagnostics.CodeAnalysis.DynamicDependencyAttribute", (string)attribute.Attribute("fullname"));
            Assert.Equal(new[] { "None", "TestNS.Target", "Input" }, attribute.Elements("argument").Select(a => a.Value));
            Assert.Equal("System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes", (string)attribute.Element("argument").Attribute("type"));
            XElement feature = attributes.Root.Elements("assembly").Single(a => a.Attribute("feature") is not null);
            Assert.Equal("TestFeature", (string)feature.Attribute("feature"));
            XElement methodEdge = Assert.Single(feature.Elements("type"));
            Assert.Equal("TestNS.Container/Trigger", (string)methodEdge.Attribute("fullname"));
            Assert.Equal("Signal", (string)methodEdge.Element("field").Attribute("name"));
            Assert.Equal(new[] { "Callback", "TestNS.Target", "Input" },
                methodEdge.Element("field").Element("attribute").Elements("argument").Select(a => a.Value));
        }

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
        [InlineData(false, false, false, false, false)]
        [InlineData(true, false, false, false, false)]
        [InlineData(false, true, false, false, false)]
        [InlineData(true, true, false, false, false)]
        [InlineData(false, false, true, false, false)]
        [InlineData(false, true, true, false, false)]
        [InlineData(false, false, false, true, false)]
        [InlineData(true, false, false, true, false)]
        [InlineData(false, true, false, true, false)]
        [InlineData(true, true, false, true, false)]
        [InlineData(false, true, true, true, false)]
        [InlineData(false, false, false, false, true)]
        [InlineData(false, true, false, false, true)]
        public void LinkerDefersConditionalFieldsAndMethods(bool reachable, bool nested, bool featureEnabled, bool generic, bool requiredSignature)
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
                requiredSignature ? "DEFINE_CLASS(HELPERS, TestNS, Helpers)" : "",
                requiredSignature ? "DEFINE_METHOD(HELPERS, USE, Use, NoSig)" : "",
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
                if (requiredSignature)
                {
                    var helpers = new TypeDefinition("TestNS", "Helpers", TypeAttributes.Public, objectType);
                    module.Types.Add(helpers);
                    var use = new MethodDefinition("Use", MethodAttributes.Public | MethodAttributes.Static, voidType);
                    use.Parameters.Add(new ParameterDefinition(candidate));
                    use.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                    helpers.Methods.Add(use);
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
            if (reachable || featureEnabled || requiredSignature)
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

        [Theory]
        [InlineData("CLASS", "none", false, false)]
        [InlineData("CLASS", "target-type", false, false)]
        [InlineData("CLASS", "type", false, true)]
        [InlineData("CLASS", "nested-target", false, true)]
        [InlineData("CLASS", "generic-target", false, true)]
        [InlineData("CLASS", "signature", false, true)]
        [InlineData("CLASS", "field-type", false, true)]
        [InlineData("CLASS", "xml-type", false, true)]
        [InlineData("CLASS", "method", false, true)]
        [InlineData("CLASS", "field", false, true)]
        [InlineData("CLASS", "feature-off", false, false)]
        [InlineData("CLASS", "feature-on", false, true)]
        [InlineData("CLASS", "feature-off", true, true)]
        [InlineData("CLASS", "second", false, true)]
        [InlineData("METHOD", "none", false, false)]
        [InlineData("METHOD", "target-type", false, false)]
        [InlineData("METHOD", "type", false, false)]
        [InlineData("METHOD", "method", false, true)]
        [InlineData("METHOD", "field", false, false)]
        [InlineData("METHOD", "xml-type", false, false)]
        [InlineData("METHOD", "xml-method", false, true)]
        [InlineData("METHOD", "xml-field", false, false)]
        [InlineData("FIELD", "none", false, false)]
        [InlineData("FIELD", "target-type", false, false)]
        [InlineData("FIELD", "type", false, false)]
        [InlineData("FIELD", "method", false, false)]
        [InlineData("FIELD", "field", false, true)]
        [InlineData("FIELD", "xml-type", false, false)]
        [InlineData("FIELD", "xml-field", false, true)]
        [InlineData("FIELD", "xml-method", false, false)]
        [InlineData("CLASS", "none", true, true)]
        [InlineData("METHOD", "none", true, true)]
        [InlineData("FIELD", "none", true, true)]
        public void LinkerActivatesCrossTypeDependencies(string triggerKind, string root, bool mandatory, bool expected)
        {
            using var test = new DescriptorTest();
            string trigger = triggerKind switch {
                "CLASS" => "TRIGGER",
                "METHOD" => "TRIGGER__ACTIVATE",
                "FIELD" => "TRIGGER__SIGNAL",
                _ => throw new InvalidOperationException(triggerKind)
            };
            string targetName = root switch {
                "nested-target" => "Container+Target",
                "generic-target" => "Target`1",
                _ => "Target"
            };
            XDocument descriptor = test.Generate(new[] {
                "DEFINE_CLASS_OPTIONAL(NEVER, TestNS, Never)",
                "DEFINE_CLASS_OPTIONAL(TRIGGER, TestNS, Trigger)",
                "DEFINE_METHOD_CONDITIONAL(TRIGGER, ACTIVATE, Activate, NoSig, METHOD, TRIGGER__ACTIVATE)",
                "DEFINE_FIELD_CONDITIONAL(TRIGGER, SIGNAL, Signal, FIELD, TRIGGER__SIGNAL)",
                $"DEFINE_CLASS_OPTIONAL(TARGET, TestNS, {targetName})",
                root.StartsWith("feature-", StringComparison.Ordinal) ? "BEGIN_ILLINK_FEATURE_SWITCH(TestFeature, true, true)" : "",
                $"DEFINE_METHOD_CONDITIONAL(TARGET, CALLBACK, Callback, NoSig, {triggerKind}, {trigger})",
                $"DEFINE_FIELD_CONDITIONAL(TARGET, VALUE, Value, {triggerKind}, {trigger})",
                $"DEFINE_CLASS_CONDITIONAL(CLASS_TARGET, TestNS, ClassTarget, {triggerKind}, {trigger})",
                root.StartsWith("feature-", StringComparison.Ordinal) ? "END_ILLINK_FEATURE_SWITCH()" : "",
                "DEFINE_CLASS_U_OPTIONAL(TestNS, ClassTarget, NativeClassTarget)",
                "DEFINE_CLASS_OPTIONAL(SECOND, TestNS, Second)",
                "DEFINE_METHOD_CONDITIONAL(TARGET, SECOND_CALLBACK, Callback, NoSig, CLASS, SECOND)",
                "DEFINE_FIELD_CONDITIONAL(TARGET, SECOND_VALUE, Value, CLASS, SECOND)",
                "DEFINE_CLASS_CONDITIONAL(SECOND_CLASS_TARGET, TestNS, ClassTarget, CLASS, SECOND)",
                "DEFINE_CLASS_CONDITIONAL(CASCADE, TestNS, Cascade, METHOD, TARGET__CALLBACK)",
                "DEFINE_CLASS_CONDITIONAL(DEAD_A, TestNS, DeadA, CLASS, DEAD_B)",
                "DEFINE_CLASS_CONDITIONAL(DEAD_B, TestNS, DeadB, CLASS, DEAD_A)",
                mandatory ? "DEFINE_CLASS(TARGET_ALIAS, TestNS, Target)" : "",
                mandatory ? "DEFINE_METHOD(TARGET_ALIAS, CALLBACK, Callback, NoSig)" : "",
                mandatory ? "DEFINE_FIELD(TARGET_ALIAS, VALUE, Value)" : "",
                mandatory ? "DEFINE_CLASS(CLASS_TARGET_ALIAS, TestNS, ClassTarget)" : "",
                root == "signature" ? "DEFINE_CLASS(PROGRAM, TestNS, Program)" : "",
                root == "signature" ? "DEFINE_METHOD(PROGRAM, SIGNATURE, Signature, NoSig)" : "",
                root == "field-type" ? "DEFINE_CLASS(PROGRAM, TestNS, Program)" : "",
                root == "field-type" ? "DEFINE_FIELD(PROGRAM, SIGNATURE, SignatureField)" : "",
                root.StartsWith("xml-", StringComparison.Ordinal) ? "DEFINE_CLASS(TRIGGER_ALIAS, TestNS, Trigger)" : "",
                root == "xml-method" ? "DEFINE_METHOD(TRIGGER_ALIAS, ACTIVATE, Activate, NoSig)" : "",
                root == "xml-field" ? "DEFINE_FIELD(TRIGGER_ALIAS, SIGNAL, Signal)" : ""
            });
            foreach (XElement feature in descriptor.Root.Elements("assembly").Where(a => a.Attribute("feature") is not null))
                feature.SetAttributeValue("fullname", "Input");
            descriptor.Save(test.DescriptorPath);
            XDocument attributes = XDocument.Load(test.AttributesPath);
            foreach (XElement feature in attributes.Root.Elements("assembly"))
                feature.SetAttributeValue("fullname", "Input");
            attributes.Save(test.AttributesPath);

            string input = Path.Combine(test.DirectoryPath, "Input.dll");
            using (AssemblyDefinition assembly = AssemblyDefinition.CreateAssembly(
                new AssemblyNameDefinition("Input", new Version(1, 0)), "Input", ModuleKind.Console))
            {
                ModuleDefinition module = assembly.MainModule;
                TypeReference objectType = module.ImportReference(typeof(object));
                AddType(module, "Never");
                TypeDefinition triggerType = AddType(module, "Trigger");
                MethodDefinition activate = AddMethod(triggerType, "Activate");
                var signal = new FieldDefinition("Signal", FieldAttributes.Public | FieldAttributes.Static, objectType);
                triggerType.Fields.Add(signal);
                TypeDefinition target;
                if (root == "nested-target")
                {
                    target = new TypeDefinition("", "Target", TypeAttributes.NestedPublic, objectType);
                    AddType(module, "Container").NestedTypes.Add(target);
                }
                else
                {
                    target = AddType(module, targetName);
                    if (root == "generic-target")
                        target.GenericParameters.Add(new GenericParameter("T", target));
                }
                AddMethod(target, "Callback");
                AddMethod(target, "Callback").Parameters.Add(new ParameterDefinition(triggerType));
                AddMethod(target, "Unused");
                target.Fields.Add(new FieldDefinition("Value", FieldAttributes.Public | FieldAttributes.Static, objectType));
                target.Fields.Add(new FieldDefinition("Unused", FieldAttributes.Public | FieldAttributes.Static, objectType));
                TypeDefinition classTarget = AddType(module, "ClassTarget");
                classTarget.Fields.Add(new FieldDefinition("Layout", FieldAttributes.Private, objectType));
                TypeDefinition second = AddType(module, "Second");
                AddType(module, "Cascade");
                AddType(module, "DeadA");
                AddType(module, "DeadB");
                TypeDefinition program = AddType(module, "Program");
                AddMethod(program, "Signature").Parameters.Add(new ParameterDefinition(triggerType));
                program.Fields.Add(new FieldDefinition("SignatureField", FieldAttributes.Public | FieldAttributes.Static, triggerType));
                MethodDefinition main = AddMethod(program, "Main");
                main.Body.Instructions.Clear();
                if (root == "target-type")
                {
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, target));
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
                }
                else if (root is "type" or "feature-off" or "feature-on" or "second" or "nested-target" or "generic-target")
                {
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldtoken, root == "second" ? second : triggerType));
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
                }
                else if (root == "method")
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Call, activate));
                else if (root == "field")
                {
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Ldsfld, signal));
                    main.Body.Instructions.Add(Instruction.Create(OpCodes.Pop));
                }
                main.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                module.EntryPoint = main;
                assembly.Write(input);
            }

            string output = Path.Combine(test.DirectoryPath, "linked");
            using var driver = new Driver(new Queue<string>(new[] {
                "-a", "Input", "entrypoint", "-reference", input, "-x", test.DescriptorPath,
                "--link-attributes", test.AttributesPath, "-out", output,
                "--feature", "TestFeature", root == "feature-off" ? "false" : "true",
                "-reference", typeof(object).Assembly.Location, "--action", "skip", "System.Private.CoreLib"
            }));
            var logger = new MockDriver.CustomLogger();
            Assert.True(driver.Run(logger) == 0, string.Join(Environment.NewLine, logger.Messages));
            using AssemblyDefinition linked = AssemblyDefinition.ReadAssembly(Path.Combine(output, "Input.dll"));
            foreach (TypeDefinition type in linked.MainModule.GetTypes())
            {
                Assert.DoesNotContain(type.CustomAttributes, IsDynamicDependency);
                foreach (MethodDefinition method in type.Methods)
                    Assert.DoesNotContain(method.CustomAttributes, IsDynamicDependency);
                foreach (FieldDefinition field in type.Fields)
                    Assert.DoesNotContain(field.CustomAttributes, IsDynamicDependency);
            }
            TypeDefinition retained = linked.MainModule.GetType("TestNS." + targetName.Replace('+', '/'));
            Assert.Equal(expected || root == "target-type", retained is not null);
            if (expected)
            {
                Assert.Equal(2, retained.Methods.Count(m => m.Name == "Callback"));
                Assert.Equal(2, retained.Methods.Count);
                Assert.Equal("Value", Assert.Single(retained.Fields).Name);
            }
            else if (root == "target-type")
            {
                Assert.Empty(retained.Methods);
                Assert.Empty(retained.Fields);
                Assert.Null(linked.MainModule.GetType("TestNS.Trigger"));
            }
            Assert.Equal(expected, linked.MainModule.GetType("TestNS.ClassTarget") is not null);
            if (expected)
                Assert.Equal("Layout", Assert.Single(linked.MainModule.GetType("TestNS.ClassTarget").Fields).Name);
            Assert.Equal(expected, linked.MainModule.GetType("TestNS.Cascade") is not null);
            Assert.Null(linked.MainModule.GetType("TestNS.DeadA"));
            Assert.Null(linked.MainModule.GetType("TestNS.DeadB"));
            Assert.Null(linked.MainModule.GetType("TestNS.Never"));

            static bool IsDynamicDependency(CustomAttribute attribute) =>
                attribute.AttributeType.FullName == "System.Diagnostics.CodeAnalysis.DynamicDependencyAttribute";

            static TypeDefinition AddType(ModuleDefinition module, string name)
            {
                var type = new TypeDefinition("TestNS", name, TypeAttributes.Public, module.ImportReference(typeof(object)));
                module.Types.Add(type);
                return type;
            }

            static MethodDefinition AddMethod(TypeDefinition type, string name)
            {
                var method = new MethodDefinition(name, MethodAttributes.Public | MethodAttributes.Static, type.Module.ImportReference(typeof(void)));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                type.Methods.Add(method);
                return method;
            }
        }

        [Theory]
        [InlineData("DEFINE_CLASS_CONDITIONAL(TARGET, TestNS, Target, UNKNOWN, TRIGGER)")]
        [InlineData("DEFINE_CLASS_CONDITIONAL(TARGET, TestNS, Target, CLASS, UNKNOWN)")]
        [InlineData("DEFINE_METHOD_CONDITIONAL(TARGET, CALLBACK, Callback, NoSig, METHOD, TRIGGER__ABSENT)")]
        [InlineData("DEFINE_FIELD_CONDITIONAL(TARGET, VALUE, Value, FIELD, TRIGGER__ABSENT)")]
        [InlineData("DEFINE_METHOD_CONDITIONAL(UNKNOWN, CALLBACK, Callback, NoSig, CLASS, TRIGGER)")]
        [InlineData("DEFINE_METHOD_CONDITIONAL(TARGET, CALLBACK)")]
        public void InvalidConditionalDeclarationsDoNotPublishDescriptors(string declaration)
        {
            using var test = new DescriptorTest();
            Assert.Null(test.Generate(new[] {
                "DEFINE_CLASS_OPTIONAL(TRIGGER, TestNS, Trigger)",
                "DEFINE_CLASS_OPTIONAL(TARGET, TestNS, Target)",
                declaration
            }, expectSuccess: false));
            Assert.Single(test.BuildEngine.Errors);
            Assert.False(File.Exists(test.DescriptorPath));
            Assert.False(File.Exists(test.AttributesPath));
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void RuntimeHeaderEmitsConditionalNativeDependencies(bool varargs)
        {
            using var test = new DescriptorTest();
            string inputs = Path.Combine(AppContext.BaseDirectory, "RuntimeRoots");
            XDocument descriptor = test.Generate(
                File.ReadAllLines(Path.Combine(inputs, "corelib.h")),
                namespaces: File.ReadAllText(Path.Combine(inputs, "namespace.h")),
                defines: varargs ? new[] { "FOR_ILLINK", "FEATURE_VARARGS" } : new[] { "FOR_ILLINK" });
            XElement assembly = descriptor.Root.Element("assembly");
            XDocument attributes = XDocument.Load(test.AttributesPath);
            AssertDependency("System.RuntimeMethodHandle", "GetMethodBody", "None", "System.Reflection.RuntimeExceptionHandlingClause");
            AssertDependency("System.RuntimeMethodHandle", "GetMethodBody", "None", "System.Reflection.RuntimeLocalVariableInfo");
            AssertDependency("System.RuntimeMethodHandle", "GetMethodBody", "GetMethodBody", "System.RuntimeMethodHandle");
            AssertDependency("System.IO.Stream", "HasOverriddenSlow", "HasOverriddenSlow", "System.IO.Stream");
            foreach (string method in new[] { "BeginRead", "EndRead", "BeginWrite", "EndWrite" })
                AssertDependency("System.IO.Stream", "HasOverriddenSlow", method, "System.IO.Stream");
            foreach (string name in new[] { "GenericComparer`1", "ObjectComparer`1", "EnumComparer`1", "NullableComparer`1" })
                AssertDependency("System.Collections.Generic.Comparer`1", null, "None", "System.Collections.Generic." + name);
            foreach (string name in new[] { "StringEqualityComparer", "EnumEqualityComparer`1", "NullableEqualityComparer`1", "GenericEqualityComparer`1", "ObjectEqualityComparer`1" })
                AssertDependency("System.Collections.Generic.EqualityComparer`1", null, "None", "System.Collections.Generic." + name);
            AssertDependency("System.Text.StringBuilder", null, "None", "System.StubHelpers.UTF8BufferMarshaler");
            foreach (string name in new[] { "System.Reflection.RuntimeExceptionHandlingClause", "System.Reflection.RuntimeLocalVariableInfo" })
            {
                XElement type = GetType(name);
                Assert.Equal("false", (string)type.Attribute("required"));
                Assert.Equal("fields", (string)type.Attribute("preserve"));
                Assert.Contains(type.Elements("method"), m => (string)m.Attribute("name") == ".ctor");
            }
            Assert.Null(GetType("System.Reflection.RuntimeMethodBody").Attribute("required"));
            foreach (string name in new[] { "System.Text.StringBuilder", "System.StubHelpers.UTF8BufferMarshaler", "System.Collections.Generic.Comparer`1", "System.Collections.Generic.EqualityComparer`1" })
                Assert.Equal("false", (string)GetType(name).Attribute("required"));
            XElement stream = GetType("System.IO.Stream");
            Assert.DoesNotContain(stream.Elements("method"), m => (string)m.Attribute("name") is "HasOverriddenSlow" or "BeginRead" or "EndRead" or "BeginWrite" or "EndWrite");
            Assert.DoesNotContain(GetType("System.RuntimeMethodHandle").Elements("method"), m => (string)m.Attribute("name") == "GetMethodBody");
            if (varargs)
            {
                Assert.Equal("false", (string)GetType("System.ArgIterator").Attribute("required"));
                Assert.Equal("fields", (string)GetType("System.ArgIterator").Attribute("preserve"));
                foreach (string method in new[] { "MarshalToManagedVaList", "MarshalToUnmanagedVaList", "CalcVaListSize" })
                {
                    AssertDependency("System.ArgIterator", null, method, "System.StubHelpers.StubHelpers");
                    Assert.DoesNotContain(GetType("System.StubHelpers.StubHelpers").Elements("method"), m => (string)m.Attribute("name") == method);
                }
            }
            else
            {
                Assert.DoesNotContain(assembly.Elements("type"), t => (string)t.Attribute("fullname") == "System.ArgIterator");
                Assert.DoesNotContain(attributes.Descendants("type"), t => (string)t.Attribute("fullname") == "System.ArgIterator");
            }

            XElement GetType(string name) => assembly.Elements("type").Single(t => (string)t.Attribute("fullname") == name);

            void AssertDependency(string triggerType, string triggerMethod, string selector, string target)
            {
                IEnumerable<XElement> providers = attributes.Descendants("type").Where(t => (string)t.Attribute("fullname") == triggerType);
                if (triggerMethod is not null)
                    providers = providers.Elements("method").Where(m => (string)m.Attribute("name") == triggerMethod);
                Assert.Contains(providers.Elements("attribute"), a =>
                    (string)a.Attribute("fullname") == "System.Diagnostics.CodeAnalysis.DynamicDependencyAttribute" &&
                    a.Elements("argument").Select(argument => argument.Value).SequenceEqual(new[] { selector, target, "Input" }));
            }
        }

        sealed class DescriptorTest : IDisposable
        {
            public string DirectoryPath { get; } = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            public string DescriptorPath => Path.Combine(DirectoryPath, "descriptor.xml");
            public string AttributesPath => Path.Combine(DirectoryPath, "attributes.xml");
            public MockBuildEngine BuildEngine { get; } = new MockBuildEngine();

            public DescriptorTest() => Directory.CreateDirectory(DirectoryPath);

            public XDocument Generate(string[] declarations, string coreTypes = "", string exceptions = "", bool expectSuccess = true,
                string namespaces = null, string[] defines = null)
            {
                File.WriteAllLines(Path.Combine(DirectoryPath, "corelib.h"), declarations);
                File.WriteAllText(Path.Combine(DirectoryPath, "namespace.h"), namespaces ?? "#define g_TestNS \"TestNS\"" + Environment.NewLine);
                File.WriteAllText(Path.Combine(DirectoryPath, "cortypeinfo.h"), coreTypes);
                File.WriteAllText(Path.Combine(DirectoryPath, "rexcep.h"), exceptions);
                string template = Path.Combine(DirectoryPath, "template.xml");
                File.WriteAllText(template, "<linker><assembly fullname=\"Input\" /></linker>");
                var task = new CreateRuntimeRootILLinkDescriptorFile {
                    BuildEngine = BuildEngine,
                    NamespaceFilePath = new TaskItem(Path.Combine(DirectoryPath, "namespace.h")),
                    MscorlibFilePath = new TaskItem(Path.Combine(DirectoryPath, "corelib.h")),
                    CortypeFilePath = new TaskItem(Path.Combine(DirectoryPath, "cortypeinfo.h")),
                    RexcepFilePath = new TaskItem(Path.Combine(DirectoryPath, "rexcep.h")),
                    ILLinkTrimXmlFilePath = new TaskItem(template),
                    RuntimeRootDescriptorFilePath = new TaskItem(DescriptorPath),
                    RuntimeRootLinkAttributesFilePath = new TaskItem(AttributesPath),
                    DefineConstants = defines?.Select(define => new TaskItem(define)).ToArray()
                };
                Assert.Equal(expectSuccess, task.Execute());
                return expectSuccess ? XDocument.Load(DescriptorPath) : null;
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
