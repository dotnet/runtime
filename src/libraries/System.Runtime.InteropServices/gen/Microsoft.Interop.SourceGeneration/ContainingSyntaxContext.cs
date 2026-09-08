// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Linq;

namespace Microsoft.Interop
{
    public enum ContainingDeclarationKind
    {
        Class,
        Struct,
        Interface,
        Record,
        RecordStruct,
        Method
    }

    public readonly struct ContainingSyntax : IEquatable<ContainingSyntax>
    {
        public ContainingSyntax(ImmutableArray<string> modifiers, ContainingDeclarationKind typeKind, string identifier, string? typeParameters = null)
        {
            Modifiers = modifiers;
            TypeKind = typeKind;
            Identifier = identifier;
            TypeParameters = typeParameters;
        }

        public ImmutableArray<string> Modifiers { get; init; }
        public string Identifier { get; init; }
        public ContainingDeclarationKind TypeKind { get; init; }
        public string? TypeParameters { get; init; }

        public string DeclarationKeyword => TypeKind switch
        {
            ContainingDeclarationKind.Class => "class",
            ContainingDeclarationKind.Struct => "struct",
            ContainingDeclarationKind.Interface => "interface",
            ContainingDeclarationKind.Record => "record",
            ContainingDeclarationKind.RecordStruct => "record struct",
            _ => throw new UnreachableException(),
        };

        public override bool Equals(object? obj) => obj is ContainingSyntax other && Equals(other);

        public bool Equals(ContainingSyntax other)
        {
            return Modifiers.SequenceEqual(other.Modifiers)
                && TypeKind == other.TypeKind
                && Identifier == other.Identifier
                && TypeParameters == other.TypeParameters;
        }

        public override int GetHashCode()
        {
            int hash = HashCode.Combine(TypeKind, Identifier);
            hash = HashCode.Combine(hash, TypeParameters);
            foreach (string modifier in Modifiers)
            {
                hash = HashCode.Combine(hash, modifier);
            }
            return hash;
        }
    }

    public sealed record ContainingSyntaxContext(ImmutableArray<ContainingSyntax> ContainingSyntax, string? ContainingNamespace)
    {
        public ContainingSyntaxContext AddContainingSyntax(ContainingSyntax nestedType)
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
            foreach (ContainingSyntax containingSyntax in ContainingSyntax)
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

        public string WrapMembersInContainingSyntaxWithUnsafeModifier(params string[] members)
        {
            var writer = new IndentedTextWriter();
            WriteToWithUnsafeModifier(writer, members, static (writer, members) =>
            {
                foreach (string member in members)
                {
                    WriteMember(writer, member);
                }
            });
            return writer.ToString();
        }

        public void WriteToWithUnsafeModifier<TState>(IndentedTextWriter writer, TState writeMembersState, Action<IndentedTextWriter, TState> writeMembers)
        {
            WriteTo(writer, writeMembersState, writeMembers, addUnsafe: true);
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
                ContainingSyntax syntax = ContainingSyntax[i];
                ImmutableArray<string> modifiers = addUnsafe ? CodeWriterHelpers.AddModifier(syntax.Modifiers, "unsafe") : syntax.Modifiers;
                if (!modifiers.IsEmpty)
                {
                    writer.Write(string.Join(" ", modifiers));
                    writer.Write(' ');
                }
                writer.Write($"{syntax.DeclarationKeyword} {syntax.Identifier}");
                if (syntax.TypeParameters is not null)
                {
                    writer.WriteVerbatim(syntax.TypeParameters);
                }
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
