// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;
using SourceGenerators;

namespace Microsoft.Interop
{
    public sealed record ContainingSyntaxContext(ImmutableArray<DeclarationHeader> ContainingSyntax, string? ContainingNamespace)
    {
        public ContainingSyntaxContext AddContainingSyntax(DeclarationHeader nestedType)
        {
            return this with { ContainingSyntax = ContainingSyntax.Insert(0, nestedType) };
        }

        public bool Equals(ContainingSyntaxContext? other)
        {
            return other is not null
                && ContainingSyntax.SequenceEqual(other.ContainingSyntax)
                && ContainingNamespace == other.ContainingNamespace;
        }

        public override int GetHashCode()
        {
            int hash = ContainingNamespace?.GetHashCode() ?? 0;
            foreach (DeclarationHeader containingSyntax in ContainingSyntax)
            {
                hash = HashCode.Combine(hash, containingSyntax.GetHashCode());
            }
            return hash;
        }

        /// <summary>Wraps a member in its containing types and namespace.</summary>
        public string WrapMemberInContainingSyntax(string member)
        {
            var writer = new IndentedTextWriter();
            WriteTo(writer, member, static (writer, member) => WriteMember(writer, member), addUnsafe: false);
            return writer.ToString();
        }

        /// <summary>Wraps members, adding unsafe to containing types only under the legacy memory safety rules.</summary>
        public string WrapMembersInContainingSyntaxWithUnsafeModifier(bool useUpdatedMemorySafetyRules, params string[] members)
        {
            var writer = new IndentedTextWriter();
            WriteToWithUnsafeModifier(useUpdatedMemorySafetyRules, writer, members, static (writer, members) =>
            {
                foreach (string member in members)
                {
                    WriteMember(writer, member);
                }
            });
            return writer.ToString();
        }

        /// <summary>Writes containing declarations with their original modifiers.</summary>
        public void WriteTo<TState>(IndentedTextWriter writer, TState writeMembersState, Action<IndentedTextWriter, TState> writeMembers)
        {
            WriteTo(writer, writeMembersState, writeMembers, addUnsafe: false);
        }

        /// <summary>Writes containing declarations, adding unsafe only under the legacy memory safety rules.</summary>
        public void WriteToWithUnsafeModifier<TState>(bool useUpdatedMemorySafetyRules, IndentedTextWriter writer, TState writeMembersState, Action<IndentedTextWriter, TState> writeMembers)
        {
            WriteTo(writer, writeMembersState, writeMembers, addUnsafe: !useUpdatedMemorySafetyRules);
        }

        private void WriteTo<TState>(IndentedTextWriter writer, TState state, Action<IndentedTextWriter, TState> writeMembers, bool addUnsafe)
        {
            if (ContainingNamespace is not null)
            {
                writer.WriteLine($"namespace {ContainingNamespace}");
                writer.WriteLine('{');
                writer.Indent++;
            }

            // Containing types are stored innermost-first.
            for (int i = ContainingSyntax.Length - 1; i >= 0; i--)
            {
                DeclarationHeader syntax = ContainingSyntax[i];
                ImmutableArray<string> modifiers = addUnsafe ? CodeWriterHelpers.AddModifier(syntax.Modifiers, "unsafe") : syntax.Modifiers;
                if (!modifiers.IsEmpty)
                {
                    writer.Write(string.Join(" ", modifiers));
                    writer.Write(' ');
                }
                writer.Write($"{syntax.Keyword} ");
                writer.Write(syntax.Name);
                writer.WriteLine();
                writer.WriteLine('{');
                writer.Indent++;
            }

            writeMembers(writer, state);

            for (int i = 0; i < ContainingSyntax.Length; i++)
            {
                writer.Indent--;
                writer.WriteLine('}');
            }
            if (ContainingNamespace is not null)
            {
                writer.Indent--;
                writer.WriteLine('}');
            }
        }

        private static void WriteMember(IndentedTextWriter writer, string member)
        {
            writer.Write(member);
            if (member.Length != 0 && member[member.Length - 1] is not ('\r' or '\n'))
            {
                writer.WriteLine();
            }
        }
    }
}
