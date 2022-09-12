// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Internal.IL;
using Internal.TypeSystem;

#nullable enable

namespace ILCompiler.Logging
{
    public struct MessageOrigin :
#if false
        IComparable<MessageOrigin>,
#endif
        IEquatable<MessageOrigin>
    {
        public string? FileName { get; }
        public TypeSystemEntity? MemberDefinition { get; }

        public int? SourceLine { get; }
        public int? SourceColumn { get; }
        public int? ILOffset { get; }

        public MessageOrigin(string fileName, int? sourceLine = null, int? sourceColumn = null)
        {
            FileName = fileName;
            SourceLine = sourceLine;
            SourceColumn = sourceColumn;
            MemberDefinition = null;
            ILOffset = null;
        }

        public MessageOrigin(TypeSystemEntity memberDefinition, string? fileName = null, int? sourceLine = 0, int? sourceColumn = 0)
        {
            FileName = fileName;
            MemberDefinition = memberDefinition;
            SourceLine = sourceLine;
            SourceColumn = sourceColumn;
            ILOffset = null;
        }

        public MessageOrigin(MethodIL methodBody, int ilOffset)
        {
            string? document = null;
            int? lineNumber = null;

            IEnumerable<ILSequencePoint>? sequencePoints = methodBody.GetDebugInfo()?.GetSequencePoints();
            if (sequencePoints != null)
            {
                foreach (var sequencePoint in sequencePoints)
                {
                    if (sequencePoint.Offset <= ilOffset)
                    {
                        document = sequencePoint.Document;
                        lineNumber = sequencePoint.LineNumber;
                    }
                }
            }
            FileName = document;
            MemberDefinition = methodBody.OwningMethod;
            SourceLine = lineNumber;
            SourceColumn = null;
            ILOffset = ilOffset;
        }

        public MessageOrigin WithInstructionOffset(MethodIL methodBody, int ilOffset)
        {
            Debug.Assert(methodBody.OwningMethod == MemberDefinition);
            return new MessageOrigin(methodBody, ilOffset);
        }

        public override string? ToString()
        {
            if (FileName == null)
                return null;

            StringBuilder sb = new StringBuilder(FileName);
            if (SourceLine.HasValue)
            {
                sb.Append('(').Append(SourceLine);
                if (SourceColumn.HasValue)
                    sb.Append(',').Append(SourceColumn);

                sb.Append(')');
            }

            return sb.ToString();
        }

        public bool Equals(MessageOrigin other) =>
            (FileName, MemberDefinition, SourceLine, SourceColumn, ILOffset) == (other.FileName, other.MemberDefinition, other.SourceLine, other.SourceColumn, other.ILOffset);

        public override bool Equals(object? obj) => obj is MessageOrigin messageOrigin && Equals(messageOrigin);
        public override int GetHashCode() => (FileName, MemberDefinition, SourceLine, SourceColumn, ILOffset).GetHashCode();
        public static bool operator ==(MessageOrigin lhs, MessageOrigin rhs) => lhs.Equals(rhs);
        public static bool operator !=(MessageOrigin lhs, MessageOrigin rhs) => !lhs.Equals(rhs);

#if false
        public int CompareTo(MessageOrigin other)
        {
            if (MemberDefinition != null && other.MemberDefinition != null)
            {
                var thisMember = Provider as IMemberDefinition;
                var otherMember = other.Provider as IMemberDefinition;
                TypeDefinition? thisTypeDef = (Provider as TypeDefinition) ?? (Provider as IMemberDefinition)?.DeclaringType;
                TypeDefinition? otherTypeDef = (other.Provider as TypeDefinition) ?? (other.Provider as IMemberDefinition)?.DeclaringType;
                var thisAssembly = thisTypeDef?.Module.Assembly ?? Provider as AssemblyDefinition;
                var otherAssembly = otherTypeDef?.Module.Assembly ?? other.Provider as AssemblyDefinition;
                int result = (thisAssembly?.Name.Name, thisTypeDef?.Name, thisMember?.Name).CompareTo
                    ((otherAssembly?.Name.Name, otherTypeDef?.Name, otherMember?.Name));
                if (result != 0)
                    return result;

                if (ILOffset != null && other.ILOffset != null)
                    return ILOffset.Value.CompareTo(other.ILOffset);

                return ILOffset == null ? (other.ILOffset == null ? 0 : 1) : -1;
            }
            else if (Provider == null && other.Provider == null)
            {
                if (FileName != null && other.FileName != null)
                {
                    return string.Compare(FileName, other.FileName);
                }
                else if (FileName == null && other.FileName == null)
                {
                    return (SourceLine, SourceColumn).CompareTo((other.SourceLine, other.SourceColumn));
                }

                return (FileName == null) ? 1 : -1;
            }

            return (Provider == null) ? 1 : -1;
        }
#endif
    }
}
