using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestReflectionPatternRecorder : IReflectionPatternRecorder
	{
		public struct ReflectionAccessPattern
		{
			public MethodDefinition SourceMethod;
			public Instruction ReflectionMethodCall;
			public IMetadataTokenProvider AccessedItem;
			public string Message;
		}

		public List<ReflectionAccessPattern> RecognizedPatterns = new List<ReflectionAccessPattern> ();
		public List<ReflectionAccessPattern> UnrecognizedPatterns = new List<ReflectionAccessPattern> ();

		public void RecognizedReflectionAccessPattern (MethodDefinition sourceMethod, Instruction reflectionMethodCall, IMetadataTokenProvider accessedItem)
		{
			RecognizedPatterns.Add (new ReflectionAccessPattern {
				SourceMethod = sourceMethod,
				ReflectionMethodCall = reflectionMethodCall,
				AccessedItem = accessedItem
			});
		}

		public void UnrecognizedReflectionAccessPattern (MethodDefinition sourceMethod, Instruction reflectionMethodCall, IMetadataTokenProvider accessedItem, string message)
		{
			UnrecognizedPatterns.Add (new ReflectionAccessPattern {
				SourceMethod = sourceMethod,
				ReflectionMethodCall = reflectionMethodCall,
				AccessedItem = accessedItem,
				Message = message
			});
		}
	}
}
