// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.VisualBasic;
using Xunit;

namespace System.CodeDom.Compiler.Tests
{
    public class VBCodeProviderOverloadTests
    {
        [Theory]
        [InlineData("Read", "Write", 0)]
        [InlineData("Read", "Read", 2)]
        [InlineData("Read", "rEAD", 2)]
        [InlineData("\u00c5ngstrom", "\u00e5ngstrom", 2)]
        [InlineData("", "", 2)]
        public void GenerateCodeFromType_MethodNames_UsesOrdinalIgnoreCase(string first, string second, int overloads)
        {
            using var provider = new VBCodeProvider();
            AssertOverloads(provider, TypeWith(Method(first), Method(second)), overloads);
        }

        [Fact]
        public void GenerateCodeFromType_NullMethodName_MatchesEmptyName()
        {
            using var provider = new VBCodeProvider();
            AssertOverloads(provider, TypeWith(Method(null), Method(string.Empty)), 2);
        }

        [Fact]
        public void GenerateCodeFromType_RepeatedMethodReference_RequiresDistinctMethod()
        {
            using var provider = new VBCodeProvider();
            CodeMemberMethod method = Method("Read");
            CodeTypeDeclaration type = TypeWith(method, method);
            AssertOverloads(provider, type, 0);

            type.Members.Add(Method("READ"));
            AssertOverloads(provider, type, 3);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void GenerateCodeFromType_ConstructorWithMatchingName_DoesNotOverloadMethod(bool isStatic)
        {
            using var provider = new VBCodeProvider();
            CodeMemberMethod constructor = isStatic ? new CodeTypeConstructor() : new CodeConstructor();
            AssertOverloads(provider, TypeWith(constructor, Method(constructor.Name)), 0);
        }

        [Fact]
        public void GenerateCodeFromType_OverloadedFlag_ObservesChanges()
        {
            using var provider = new VBCodeProvider();
            CodeMemberMethod method = Method("Read");
            CodeTypeDeclaration type = TypeWith(method);
            AssertOverloads(provider, type, 0);
            method.Attributes |= MemberAttributes.Overloaded;
            AssertOverloads(provider, type, 1);
            method.Attributes &= ~MemberAttributes.Overloaded;
            AssertOverloads(provider, type, 0);
        }

        [Fact]
        public void GenerateCodeFromType_NameAndPrivateImplementationChanges_UpdatesOverloads()
        {
            using var provider = new VBCodeProvider();
            CodeMemberMethod second = Method("Write");
            CodeTypeDeclaration type = TypeWith(Method("Read"), second);
            AssertOverloads(provider, type, 0);
            second.Name = "READ";
            AssertOverloads(provider, type, 2);
            second.PrivateImplementationType = new CodeTypeReference("IReader");
            AssertOverloads(provider, type, 0);
            second.PrivateImplementationType = null;
            AssertOverloads(provider, type, 2);
        }

        [Fact]
        public void GenerateCodeFromType_PrivateMethodRenamedThenMadePublic_UpdatesOverloads()
        {
            using var provider = new VBCodeProvider();
            CodeMemberMethod second = Method("Write");
            second.PrivateImplementationType = new CodeTypeReference("IReader");
            CodeTypeDeclaration type = TypeWith(Method("Read"), second);
            AssertOverloads(provider, type, 0);
            second.Name = "READ";
            AssertOverloads(provider, type, 0);
            second.PrivateImplementationType = null;
            AssertOverloads(provider, type, 2);
        }

        [Fact]
        public void GenerateCodeFromType_IListChanges_UpdatesOverloads()
        {
            using var provider = new VBCodeProvider();
            CodeTypeDeclaration type = TypeWith(Method("Read"), Method("Write"));
            IList members = type.Members;
            AssertOverloads(provider, type, 0);
            members.Insert(0, Method("READ"));
            AssertOverloads(provider, type, 2);
            members.RemoveAt(0);
            AssertOverloads(provider, type, 0);
            members[1] = Method("READ");
            AssertOverloads(provider, type, 2);
            members.Remove(members[1]);
            AssertOverloads(provider, type, 0);
            members.Clear();
            members.Add(Method("Read"));
            AssertOverloads(provider, type, 0);
        }

        [Theory]
        [InlineData("parameters", false)]
        [InlineData("statements", false)]
        [InlineData("implementationTypes", false)]
        [InlineData("parameters", true)]
        [InlineData("statements", true)]
        [InlineData("implementationTypes", true)]
        public void GenerateCodeFromType_PopulateCallback_ObservesLaterMethodChanges(string callback, bool makePrivate)
        {
            using var provider = new VBCodeProvider();
            CodeMemberMethod trigger = Method("Trigger");
            CodeMemberMethod changed = Method("Write");
            CodeTypeDeclaration type = TypeWith(trigger, Method("Read"), changed);
            EventHandler handler = (sender, args) =>
            {
                changed.Name = "READ";
                if (makePrivate)
                {
                    changed.PrivateImplementationType = new CodeTypeReference("IReader");
                }
            };
            switch (callback)
            {
                case "parameters": trigger.PopulateParameters += handler; break;
                case "statements": trigger.PopulateStatements += handler; break;
                case "implementationTypes": trigger.PopulateImplementationTypes += handler; break;
            }

            AssertOverloads(provider, type, makePrivate ? 0 : 2);
        }

        [Fact]
        public void GenerateCodeFromType_PopulateMembers_IncludesAddedMethods()
        {
            using var provider = new VBCodeProvider();
            var type = new CodeTypeDeclaration("Example");
            type.PopulateMembers += (sender, args) => type.Members.AddRange(TypeWith(Method("Read"), Method("READ")).Members);
            AssertOverloads(provider, type, 2);
        }

        [Fact]
        public void GenerateCodeFromType_SharedMethod_UpdatesBothTypes()
        {
            using var provider = new VBCodeProvider();
            CodeMemberMethod shared = Method("Read");
            CodeTypeDeclaration first = TypeWith(shared, Method("Write"));
            CodeTypeDeclaration second = TypeWith(shared, Method("READ"));
            AssertOverloads(provider, first, 0);
            AssertOverloads(provider, second, 2);
            shared.Name = "Write";
            AssertOverloads(provider, first, 2);
            AssertOverloads(provider, second, 0);
        }

        [Fact]
        public void GenerateCodeFromType_NestedTypes_KeepSeparateOverloadSets()
        {
            using var provider = new VBCodeProvider();
            CodeTypeDeclaration nested = TypeWith(Method("Read"), Method("READ"));
            nested.Name = "Nested";
            AssertOverloads(provider, TypeWith(Method("Read"), nested), 2);
        }

        [Fact]
        public void GenerateCodeFromType_UnchangedGraph_EmitsIdenticalText()
        {
            using var provider = new VBCodeProvider();
            CodeTypeDeclaration type = TypeWith(Method("Read"), Method("READ"), Method("Write"));
            string first = Generate(provider, type);
            Assert.Equal(first, Generate(provider, type));
        }

        [Fact]
        public void GenerateCodeFromType_MembersChangedDuringEnumeration_Throws()
        {
            using var provider = new VBCodeProvider();
            CodeMemberMethod trigger = Method("Trigger");
            CodeTypeDeclaration type = TypeWith(trigger, Method("Read"));
            trigger.PopulateParameters += (sender, args) => type.Members.Add(Method("READ"));
            Assert.Throws<InvalidOperationException>(() => Generate(provider, type));
        }

        [Fact]
        public void GenerateCodeFromType_ManyUniqueMethods_DoesNotEmitOverloads()
        {
            using var provider = new VBCodeProvider();
            var type = new CodeTypeDeclaration("Example");
            for (int index = 0; index < 2500; index++)
            {
                type.Members.Add(Method("Method" + index.ToString(CultureInfo.InvariantCulture)));
            }
            AssertOverloads(provider, type, 0);
        }

        [Theory]
        [InlineData(2)]
        [InlineData(31)]
        [InlineData(32)]
        [InlineData(33)]
        [InlineData(64)]
        public void GenerateCodeFromType_SmallAndIndexedTypes_ObserveRenames(int memberCount)
        {
            using var provider = new VBCodeProvider();
            var type = new CodeTypeDeclaration("Example");
            for (int index = 0; index < memberCount; index++)
            {
                type.Members.Add(Method("Method" + index.ToString(CultureInfo.InvariantCulture)));
            }

            AssertOverloads(provider, type, 0);
            type.Members[memberCount - 1].Name = type.Members[0].Name;
            AssertOverloads(provider, type, 2);
            type.Members.RemoveAt(memberCount - 1);
            AssertOverloads(provider, type, 0);
            type.Members.Add(Method(type.Members[0].Name));
            AssertOverloads(provider, type, 2);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void GenerateCodeFromType_ProviderAlive_DoesNotKeepPreviousTypeAlive()
        {
            using var provider = new VBCodeProvider();
            WeakReference reference = GeneratePreviousType(provider);
            Collect(reference);
            Assert.False(reference.IsAlive);
            GC.KeepAlive(provider);
        }

        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.IsPreciseGcSupported))]
        public void GenerateCodeFromType_TypeAlive_DoesNotKeepRemovedMethodAlive()
        {
            using var provider = new VBCodeProvider();
            (CodeTypeDeclaration type, WeakReference reference) = GenerateAndRemoveMethod(provider);
            Collect(reference);
            Assert.False(reference.IsAlive);
            GC.KeepAlive(type);
            GC.KeepAlive(provider);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static WeakReference GeneratePreviousType(VBCodeProvider provider)
        {
            CodeTypeDeclaration type = TypeWith(Method("Read"), Method("Write"));
            Generate(provider, type);
            var reference = new WeakReference(type);
            // Advance the generator's current-type and current-member fields as well.
            Generate(provider, TypeWith(Method("Next")));
            return reference;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static (CodeTypeDeclaration, WeakReference) GenerateAndRemoveMethod(VBCodeProvider provider)
        {
            CodeMemberMethod removed = Method("Read");
            CodeTypeDeclaration type = TypeWith(removed, Method("Write"));
            Generate(provider, type);
            type.Members.Remove(removed);
            return (type, new WeakReference(removed));
        }

        private static void Collect(WeakReference reference)
        {
            for (int attempt = 0; attempt < 3 && reference.IsAlive; attempt++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
            }
        }

        private static CodeMemberMethod Method(string name) => new CodeMemberMethod
        {
            Name = name,
            Attributes = MemberAttributes.Public | MemberAttributes.Final
        };

        private static CodeTypeDeclaration TypeWith(params CodeTypeMember[] members)
        {
            var type = new CodeTypeDeclaration("Example");
            type.Members.AddRange(members);
            // Exercise indexed generation in the mutation and lifetime tests.
            for (int index = 0; index < 64; index++)
            {
                type.Members.Add(Method("Padding" + index.ToString(CultureInfo.InvariantCulture)));
            }
            return type;
        }

        private static string Generate(VBCodeProvider provider, CodeTypeDeclaration type)
        {
            using var writer = new StringWriter(CultureInfo.InvariantCulture);
            provider.GenerateCodeFromType(type, writer, new CodeGeneratorOptions { BlankLinesBetweenMembers = false });
            return writer.ToString();
        }

        private static void AssertOverloads(VBCodeProvider provider, CodeTypeDeclaration type, int expected)
        {
            string text = Generate(provider, type);
            int actual = 0;
            for (int offset = 0; (offset = text.IndexOf("Overloads ", offset, StringComparison.Ordinal)) >= 0; offset += "Overloads ".Length)
            {
                actual++;
            }
            Assert.Equal(expected, actual);
        }
    }
}
