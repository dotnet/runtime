// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Linq;

namespace Microsoft.Interop
{
    internal sealed record GeneratedStubCodeContext(
        ManagedTypeInfo OriginalDefiningType,
        ContainingSyntaxContext ContainingSyntaxContext,
        GeneratedComMember Stub,
        SequenceEqualImmutableArray<DiagnosticInfo> Diagnostics) : GeneratedMethodContextBase(OriginalDefiningType, Diagnostics);

    internal sealed record GeneratedComMember(
        StubMemberKind Kind,
        string Identifier,
        GeneratedMethodSignature Signature,
        SequenceEqualImmutableArray<string> Attributes,
        string Body,
        string Modifiers = "",
        bool IsExpressionBody = false)
    {
        public bool HasSameAccessorTarget(GeneratedComMember other)
        {
            if (Kind.IsIndexerAccessor() != other.Kind.IsIndexerAccessor())
            {
                return false;
            }

            if (!Kind.IsIndexerAccessor())
            {
                return Identifier == other.Identifier;
            }

            return Signature.Parameters.Select(static parameter => (parameter.Type, parameter.Modifiers))
                .SequenceEqual(other.Signature.Parameters.Select(static parameter => (parameter.Type, parameter.Modifiers)));
        }

        public void WriteTo(IndentedTextWriter writer, string? explicitInterface = null, GeneratedComMember? setter = null)
        {
            string qualifier = explicitInterface is null ? "" : explicitInterface + ".";
            if (Kind is StubMemberKind.Method)
            {
                WriteAttributes(writer);
                writer.Write($"{(Modifiers.Length == 0 ? "" : Modifiers + " ")}{Signature.ReturnType} {qualifier}{Identifier}{Signature.ParameterList}");
                if (IsExpressionBody)
                {
                    writer.WriteLine($" => {Body};");
                }
                else
                {
                    writer.WriteLine();
                    writer.Write(Body);
                }
                return;
            }

            if (Kind.IsIndexerAccessor())
            {
                writer.WriteLine($"{Signature.ReturnType} {qualifier}this[{string.Join(", ", Signature.Parameters)}]");
            }
            else
            {
                writer.WriteLine($"{Signature.ReturnType} {qualifier}{Identifier}");
            }

            using (writer.WriteBlock())
            {
                WriteAccessor(writer);
                setter?.WriteAccessor(writer);
            }
        }

        private void WriteAccessor(IndentedTextWriter writer)
        {
            WriteAttributes(writer);
            string keyword = Kind.IsAccessorSetter() ? "set" : "get";
            if (IsExpressionBody)
            {
                writer.WriteLine($"{keyword} => {Body};");
            }
            else
            {
                writer.WriteLine(keyword);
                writer.Write(Body);
            }
        }

        private void WriteAttributes(IndentedTextWriter writer)
        {
            foreach (string attribute in Attributes)
            {
                writer.WriteLine($"[{attribute}]");
            }
        }
    }
}
