// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Linq;
using System.Text;

namespace Mono.Linker
{
	public readonly struct MessageOrigin
	{
#nullable enable
		public string? FileName { get; }
		public IMemberDefinition? MemberDefinition { get; }
#nullable disable
		public int SourceLine { get; }
		public int SourceColumn { get; }

		public MessageOrigin (string fileName, int sourceLine = 0, int sourceColumn = 0)
		{
			FileName = fileName;
			SourceLine = sourceLine;
			SourceColumn = sourceColumn;
			MemberDefinition = null;
		}

		public MessageOrigin (IMemberDefinition memberDefinition, int sourceLine = 0, int sourceColumn = 0)
		{
			FileName = null;
			MemberDefinition = memberDefinition;
			SourceLine = sourceLine;
			SourceColumn = sourceColumn;
		}

		private MessageOrigin (string fileName, IMemberDefinition memberDefinition, int sourceLine = 0, int sourceColumn = 0)
		{
			FileName = fileName;
			MemberDefinition = memberDefinition;
			SourceLine = sourceLine;
			SourceColumn = sourceColumn;
		}

		public static MessageOrigin TryGetOrigin (MethodDefinition sourceMethod, int ilOffset)
		{
			if (sourceMethod.DebugInformation.HasSequencePoints) {
				SequencePoint correspondingSequencePoint = sourceMethod.DebugInformation.SequencePoints
					.Where (s => s.Offset <= ilOffset)?.Last ();
				if (correspondingSequencePoint == null)
					return new MessageOrigin (correspondingSequencePoint.Document.Url, sourceMethod);

				return new MessageOrigin (correspondingSequencePoint.Document.Url, sourceMethod, correspondingSequencePoint.StartLine, correspondingSequencePoint.StartColumn);
			}

			return new MessageOrigin (sourceMethod);
		}

		public override string ToString ()
		{
			if (FileName == null)
				return null;

			StringBuilder sb = new StringBuilder (FileName);
			if (SourceLine != 0) {
				sb.Append ("(").Append (SourceLine);
				if (SourceColumn != 0)
					sb.Append (",").Append (SourceColumn);

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
	}
}
