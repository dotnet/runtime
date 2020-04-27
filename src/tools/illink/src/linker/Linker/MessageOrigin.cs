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
		public string FileName { get; }
		public int SourceLine { get; }
		public int SourceColumn { get; }

		public MessageOrigin (string fileName, int sourceLine = 0, int sourceColumn = 0)
		{
			FileName = fileName;
			SourceLine = sourceLine;
			SourceColumn = sourceColumn;
		}

		public static MessageOrigin? TryGetOrigin (IMemberDefinition sourceMethod, int ilOffset)
		{
			if (sourceMethod is MethodDefinition methodDef) {
				if (!methodDef.DebugInformation.HasSequencePoints)
					return null;

				SequencePoint correspondingSequencePoint = methodDef.DebugInformation.SequencePoints
					.Where (s => s.Offset <= ilOffset)?.First ();
				if (correspondingSequencePoint == null)
					return null;

				return new MessageOrigin (correspondingSequencePoint.Document.Url, correspondingSequencePoint.StartLine, correspondingSequencePoint.StartColumn);
			}

			return null;
		}

		public override string ToString ()
		{
			StringBuilder sb = new StringBuilder (FileName);

			if (SourceLine != 0) {
				sb.Append ("(").Append (SourceLine);
				if (SourceColumn != 0)
					sb.Append (",").Append (SourceColumn);

				sb.Append (")");
			}

			return sb.ToString ();
		}
	}
}
