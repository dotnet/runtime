using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestReflectionPatternRecorder : IReflectionPatternRecorder
	{
		public IReflectionPatternRecorder PreviousRecorder = null;

		public struct ReflectionAccessPattern
		{
			public IMemberDefinition Source;
			public Instruction SourceInstruction;
			public IMetadataTokenProvider AccessedItem;
			public string Message;
		}

		public List<ReflectionAccessPattern> RecognizedPatterns = new List<ReflectionAccessPattern> ();
		public List<ReflectionAccessPattern> UnrecognizedPatterns = new List<ReflectionAccessPattern> ();

		public void RecognizedReflectionAccessPattern (IMemberDefinition source, Instruction sourceInstruction, IMetadataTokenProvider accessedItem)
		{
			PreviousRecorder?.RecognizedReflectionAccessPattern (source, sourceInstruction, accessedItem);
			RecognizedPatterns.Add (new ReflectionAccessPattern {
				Source = source,
				SourceInstruction = sourceInstruction,
				AccessedItem = accessedItem
			});
		}

		public void UnrecognizedReflectionAccessPattern (IMemberDefinition source, Instruction sourceInstruction, IMetadataTokenProvider accessedItem, string message)
		{
			PreviousRecorder?.UnrecognizedReflectionAccessPattern (source, sourceInstruction, accessedItem, message);
			UnrecognizedPatterns.Add (new ReflectionAccessPattern {
				Source = source,
				SourceInstruction = sourceInstruction,
				AccessedItem = accessedItem,
				Message = message
			});
		}
	}
}
