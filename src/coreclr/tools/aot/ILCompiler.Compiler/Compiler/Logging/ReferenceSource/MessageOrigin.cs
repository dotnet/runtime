// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker
{
	public readonly struct MessageOrigin : IComparable<MessageOrigin>, IEquatable<MessageOrigin>
	{
		public string? FileName { get; }
		public ICustomAttributeProvider? Provider { get; }
		readonly ICustomAttributeProvider? _suppressionContextMember;
		public ICustomAttributeProvider? SuppressionContextMember {
			get {
				Debug.Assert (_suppressionContextMember == null || _suppressionContextMember is IMemberDefinition || _suppressionContextMember is AssemblyDefinition);
				return _suppressionContextMember ?? Provider;
			}
		}

		public int SourceLine { get; }
		public int SourceColumn { get; }
		public int? ILOffset { get; }

		const int HiddenLineNumber = 0xfeefee;

		public MessageOrigin (IMemberDefinition? memberDefinition, int? ilOffset = null)
			: this (memberDefinition as ICustomAttributeProvider, ilOffset)
		{
		}

		public MessageOrigin (ICustomAttributeProvider? provider)
			: this (provider, null)
		{
		}

		public MessageOrigin (string fileName, int sourceLine = 0, int sourceColumn = 0)
			: this (fileName, sourceLine, sourceColumn, null)
		{
		}

		// The assembly attribute should be specified if available as it allows assigning the diagnostic
		// to a an assembly (we group based on assembly).
		public MessageOrigin (string fileName, int sourceLine, int sourceColumn, AssemblyDefinition? assembly)
		{
			FileName = fileName;
			SourceLine = sourceLine;
			SourceColumn = sourceColumn;
			Provider = assembly;
			_suppressionContextMember = null;
			ILOffset = null;
		}

		public MessageOrigin (ICustomAttributeProvider? provider, int? ilOffset)
			: this (provider, ilOffset, null)
		{
		}

		public MessageOrigin (ICustomAttributeProvider? provider, int? ilOffset, ICustomAttributeProvider? suppressionContextMember)
		{
			Debug.Assert (provider == null || provider is IMemberDefinition || provider is AssemblyDefinition);
			Debug.Assert (suppressionContextMember == null || suppressionContextMember is IMemberDefinition || provider is AssemblyDefinition);
			FileName = null;
			Provider = provider;
			_suppressionContextMember = suppressionContextMember;
			SourceLine = 0;
			SourceColumn = 0;
			ILOffset = ilOffset;
		}

		public MessageOrigin (MessageOrigin other, IMemberDefinition? suppressionContextMember)
		{
			FileName = other.FileName;
			Provider = other.Provider;
			_suppressionContextMember = suppressionContextMember;
			SourceLine = other.SourceLine;
			SourceColumn = other.SourceColumn;
			ILOffset = other.ILOffset;
		}

		public override string? ToString ()
		{
			int sourceLine = SourceLine, sourceColumn = SourceColumn;
			string? fileName = FileName;
			if (Provider is MethodDefinition method &&
				method.DebugInformation.HasSequencePoints) {
				var offset = ILOffset ?? 0;
				SequencePoint? correspondingSequencePoint = method.DebugInformation.SequencePoints
					.Where (s => s.Offset <= offset)?.Last ();

				// If the warning comes from hidden line (compiler generated code typically)
				// search for any sequence point with non-hidden line number and report that as a best effort.
				if (correspondingSequencePoint?.StartLine == HiddenLineNumber) {
					correspondingSequencePoint = method.DebugInformation.SequencePoints
						.Where (s => s.StartLine != HiddenLineNumber).FirstOrDefault ();
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
			(FileName, Provider, SourceLine, SourceColumn, ILOffset) == (other.FileName, other.Provider, other.SourceLine, other.SourceColumn, other.ILOffset);

		public override bool Equals (object? obj) => obj is MessageOrigin messageOrigin && Equals (messageOrigin);
		public override int GetHashCode () => (FileName, Provider, SourceLine, SourceColumn).GetHashCode ();
		public static bool operator == (MessageOrigin lhs, MessageOrigin rhs) => lhs.Equals (rhs);
		public static bool operator != (MessageOrigin lhs, MessageOrigin rhs) => !lhs.Equals (rhs);

		public int CompareTo (MessageOrigin other)
		{
			if (Provider != null && other.Provider != null) {
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
					return ILOffset.Value.CompareTo (other.ILOffset);

				return ILOffset == null ? (other.ILOffset == null ? 0 : 1) : -1;
			} else if (Provider == null && other.Provider == null) {
				if (FileName != null && other.FileName != null) {
					return string.Compare (FileName, other.FileName);
				} else if (FileName == null && other.FileName == null) {
					return (SourceLine, SourceColumn).CompareTo ((other.SourceLine, other.SourceColumn));
				}

				return (FileName == null) ? 1 : -1;
			}

			return (Provider == null) ? 1 : -1;
		}
	}
}
