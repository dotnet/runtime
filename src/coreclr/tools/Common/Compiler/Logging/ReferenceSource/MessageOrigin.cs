// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker
{
	public readonly struct MessageOrigin : IComparable<MessageOrigin>, IEquatable<MessageOrigin>
	{
#nullable enable
		public string? FileName { get; }
		public IMemberDefinition? MemberDefinition { get; }
#nullable disable
		public int SourceLine { get; }
		public int SourceColumn { get; }
		public int? ILOffset { get; }

		public MessageOrigin (string fileName, int sourceLine = 0, int sourceColumn = 0)
		{
			FileName = fileName;
			SourceLine = sourceLine;
			SourceColumn = sourceColumn;
			MemberDefinition = null;
			ILOffset = null;
		}

		public MessageOrigin (IMemberDefinition memberDefinition, int? ilOffset = null)
		{
			FileName = null;
			MemberDefinition = memberDefinition;
			SourceLine = 0;
			SourceColumn = 0;
			ILOffset = ilOffset;
		}

		public override string ToString ()
		{
			int sourceLine = SourceLine, sourceColumn = SourceColumn;
			string fileName = FileName;
			if (MemberDefinition is MethodDefinition method &&
				method.DebugInformation.HasSequencePoints) {
				var offset = ILOffset ?? 0;
				SequencePoint correspondingSequencePoint = method.DebugInformation.SequencePoints
					.Where (s => s.Offset <= offset)?.Last ();
				if (correspondingSequencePoint != null) {
					fileName = correspondingSequencePoint.Document.Url;
					sourceLine = correspondingSequencePoint.StartLine;
					sourceColumn = correspondingSequencePoint.StartColumn;
				}
			}

			if (fileName == null)
				return null;

			StringBuilder sb = new StringBuilder (fileName);
			if (sourceLine != 0) {
				sb.Append ("(").Append (sourceLine);
				if (sourceColumn != 0)
					sb.Append (",").Append (sourceColumn);

				sb.Append (")");
			}

			return sb.ToString ();
		}

		public bool Equals (MessageOrigin other) =>
			(FileName, MemberDefinition, SourceLine, SourceColumn) == (other.FileName, other.MemberDefinition, other.SourceLine, other.SourceColumn);

		public override bool Equals (object obj) => obj is MessageOrigin messageOrigin && Equals (messageOrigin);
		public override int GetHashCode () => (FileName, MemberDefinition, SourceLine, SourceColumn).GetHashCode ();
		public static bool operator == (MessageOrigin lhs, MessageOrigin rhs) => lhs.Equals (rhs);
		public static bool operator != (MessageOrigin lhs, MessageOrigin rhs) => !lhs.Equals (rhs);

		public int CompareTo (MessageOrigin other)
		{
			if (MemberDefinition != null && other.MemberDefinition != null) {
				return (MemberDefinition.DeclaringType?.Module?.Assembly?.Name?.Name, MemberDefinition.DeclaringType?.Name, MemberDefinition?.Name).CompareTo
					((other.MemberDefinition.DeclaringType?.Module?.Assembly?.Name?.Name, other.MemberDefinition.DeclaringType?.Name, other.MemberDefinition?.Name));
			} else if (MemberDefinition == null && other.MemberDefinition == null) {
				if (FileName != null && other.FileName != null) {
					return string.Compare (FileName, other.FileName);
				} else if (FileName == null && other.FileName == null) {
					return (SourceLine, SourceColumn).CompareTo ((other.SourceLine, other.SourceColumn));
				}

				return (FileName == null) ? 1 : -1;
			}

			return (MemberDefinition == null) ? 1 : -1;
		}
	}
}
