// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Text;

using Internal.TypeSystem;

namespace ILCompiler.Logging
{
    public struct MessageOrigin
#if false
        : IComparable<MessageOrigin>, IEquatable<MessageOrigin>
#endif
    {
        public string FileName { get; }
        public TypeSystemEntity MemberDefinition { get; }

        public int? SourceLine { get; }
        public int? SourceColumn { get; }

        public MessageOrigin(string fileName, int? sourceLine = null, int? sourceColumn = null)
        {
            FileName = fileName;
            SourceLine = sourceLine;
            SourceColumn = sourceColumn;
            MemberDefinition = null;
        }

        public MessageOrigin(TypeSystemEntity memberDefinition, string fileName = null, int? sourceLine = 0, int? sourceColumn = 0)
        {
            FileName = fileName;
            MemberDefinition = memberDefinition;
            SourceLine = sourceLine;
            SourceColumn = sourceColumn;
        }

        public override string ToString()
        {
            if (FileName == null)
                return null;

            StringBuilder sb = new StringBuilder(FileName);
            if (SourceLine.HasValue)
            {
                sb.Append("(").Append(SourceLine);
                if (SourceColumn.HasValue)
                    sb.Append(",").Append(SourceColumn);

                sb.Append(")");
            }

            return sb.ToString();
        }

#if false
        public bool Equals(MessageOrigin other) =>
            (FileName, MemberDefinition, SourceLine, SourceColumn, ILOffset) == (other.FileName, other.MemberDefinition, other.SourceLine, other.SourceColumn, other.ILOffset);

        public override bool Equals(object obj) => obj is MessageOrigin messageOrigin && Equals(messageOrigin);
        public override int GetHashCode() => (FileName, MemberDefinition, SourceLine, SourceColumn).GetHashCode();
        public static bool operator ==(MessageOrigin lhs, MessageOrigin rhs) => lhs.Equals(rhs);
        public static bool operator !=(MessageOrigin lhs, MessageOrigin rhs) => !lhs.Equals(rhs);

        public int CompareTo(MessageOrigin other)
        {
            if (MemberDefinition != null && other.MemberDefinition != null)
            {
                TypeDefinition thisTypeDef = (MemberDefinition as TypeDefinition) ?? MemberDefinition.DeclaringType;
				TypeDefinition otherTypeDef = (other.MemberDefinition as TypeDefinition) ?? other.MemberDefinition.DeclaringType;
				int result = (thisTypeDef?.Module?.Assembly?.Name?.Name, thisTypeDef?.Name, MemberDefinition?.Name).CompareTo
					((otherTypeDef?.Module?.Assembly?.Name?.Name, otherTypeDef?.Name, other.MemberDefinition?.Name));
				if (result != 0)
					return result;
				if (ILOffset != null && other.ILOffset != null)
					return ILOffset.Value.CompareTo (other.ILOffset);
				return ILOffset == null ? (other.ILOffset == null ? 0 : 1) : -1;
            }
            else if (MemberDefinition == null && other.MemberDefinition == null)
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

            return (MemberDefinition == null) ? 1 : -1;
        }
#endif
    }
}
