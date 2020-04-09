using Mono.Cecil;
using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestReflectionPatternRecorder : IReflectionPatternRecorder
	{
		public struct ReflectionAccessPattern
		{
			public MethodDefinition SourceMethod;
			public MethodDefinition ReflectionMethod;
			public IMetadataTokenProvider AccessedItem;
			public string Message;
		}

		public List<ReflectionAccessPattern> RecognizedPatterns = new List<ReflectionAccessPattern> ();
		public List<ReflectionAccessPattern> UnrecognizedPatterns = new List<ReflectionAccessPattern> ();

		public void RecognizedReflectionAccessPattern (MethodDefinition sourceMethod, MethodDefinition reflectionMethod, IMetadataTokenProvider accessedItem)
		{
			RecognizedPatterns.Add (new ReflectionAccessPattern {
				SourceMethod = sourceMethod,
				ReflectionMethod = reflectionMethod,
				AccessedItem = accessedItem
			});
		}

		public void UnrecognizedReflectionAccessPattern (MethodDefinition sourceMethod, MethodDefinition reflectionMethod, string message)
		{
			UnrecognizedPatterns.Add (new ReflectionAccessPattern {
				SourceMethod = sourceMethod,
				ReflectionMethod = reflectionMethod,
				Message = message
			});
		}
	}
}
