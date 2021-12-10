using System.Collections.Generic;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestReflectionPatternRecorder : IReflectionPatternRecorder
	{
		public IReflectionPatternRecorder PreviousRecorder = null;

		public struct ReflectionAccessPattern
		{
			public ICustomAttributeProvider Source;
			public Instruction SourceInstruction;
			public IMetadataTokenProvider AccessedItem;
			public string Message;
			public int MessageCode;
		}

		public List<ReflectionAccessPattern> RecognizedPatterns = new List<ReflectionAccessPattern> ();
		public List<ReflectionAccessPattern> UnrecognizedPatterns = new List<ReflectionAccessPattern> ();

		public void RecognizedReflectionAccessPattern (ICustomAttributeProvider source, Instruction sourceInstruction, IMetadataTokenProvider accessedItem)
		{
			PreviousRecorder?.RecognizedReflectionAccessPattern (source, sourceInstruction, accessedItem);
			RecognizedPatterns.Add (new ReflectionAccessPattern {
				Source = source,
				SourceInstruction = sourceInstruction,
				AccessedItem = accessedItem
			});
		}

		public void UnrecognizedReflectionAccessPattern (in MessageOrigin origin, ICustomAttributeProvider source, Instruction sourceInstruction, IMetadataTokenProvider accessedItem, string message, int messageCode)
		{
			PreviousRecorder?.UnrecognizedReflectionAccessPattern (origin, source, sourceInstruction, accessedItem, message, messageCode);
			UnrecognizedPatterns.Add (new ReflectionAccessPattern {
				Source = source,
				SourceInstruction = sourceInstruction,
				AccessedItem = accessedItem,
				Message = message,
				MessageCode = messageCode
			});
		}
	}
}
