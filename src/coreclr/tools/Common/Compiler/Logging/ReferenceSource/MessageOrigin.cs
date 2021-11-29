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

		readonly IMemberDefinition _suppressionContextMember;
		public IMemberDefinition? SuppressionContextMember { get => _suppressionContextMember ?? MemberDefinition; }
#nullable disable
		public int SourceLine { get; }
		public int SourceColumn { get; }
		public int? ILOffset { get; }

		const int HiddenLineNumber = 0xfeefee;

		public MessageOrigin (IMemberDefinition memberDefinition)
			: this (memberDefinition, null)
		{
		}

		public MessageOrigin (string fileName, int sourceLine = 0, int sourceColumn = 0)
		{
			FileName = fileName;
			SourceLine = sourceLine;
			SourceColumn = sourceColumn;
			MemberDefinition = null;
			_suppressionContextMember = null;
			ILOffset = null;
		}

		public MessageOrigin (IMemberDefinition memberDefinition, int? ilOffset)
			: this (memberDefinition, ilOffset, null)
		{
		}

		public MessageOrigin (IMemberDefinition memberDefinition, int? ilOffset, IMemberDefinition suppressionContextMember)
		{
			FileName = null;
			MemberDefinition = memberDefinition;
			_suppressionContextMember = suppressionContextMember;
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

				// If the warning comes from hidden line (compiler generated code typically)
				// search for any sequence point with non-hidden line number and report that as a best effort.
				if (correspondingSequencePoint.StartLine == HiddenLineNumber) {
					correspondingSequencePoint = method.DebugInformation.SequencePoints
						.Where (s => s.StartLine != HiddenLineNumber)?.FirstOrDefault ();
				}

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
			(FileName, MemberDefinition, SourceLine, SourceColumn, ILOffset) == (other.FileName, other.MemberDefinition, other.SourceLine, other.SourceColumn, other.ILOffset);

		public override bool Equals (object obj) => obj is MessageOrigin messageOrigin && Equals (messageOrigin);
		public override int GetHashCode () => (FileName, MemberDefinition, SourceLine, SourceColumn).GetHashCode ();
		public static bool operator == (MessageOrigin lhs, MessageOrigin rhs) => lhs.Equals (rhs);
		public static bool operator != (MessageOrigin lhs, MessageOrigin rhs) => !lhs.Equals (rhs);

		public int CompareTo (MessageOrigin other)
		{
			if (MemberDefinition != null && other.MemberDefinition != null) {
				TypeDefinition thisTypeDef = (MemberDefinition as TypeDefinition) ?? MemberDefinition.DeclaringType;
				TypeDefinition otherTypeDef = (other.MemberDefinition as TypeDefinition) ?? other.MemberDefinition.DeclaringType;
				int result = (thisTypeDef?.Module?.Assembly?.Name?.Name, thisTypeDef?.Name, MemberDefinition?.Name).CompareTo
					((otherTypeDef?.Module?.Assembly?.Name?.Name, otherTypeDef?.Name, other.MemberDefinition?.Name));
				if (result != 0)
					return result;

				if (ILOffset != null && other.ILOffset != null)
					return ILOffset.Value.CompareTo (other.ILOffset);

				return ILOffset == null ? (other.ILOffset == null ? 0 : 1) : -1;
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
