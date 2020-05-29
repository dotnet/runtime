using Mono.Cecil;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class TestReflectionPatternRecorder : IReflectionPatternRecorder
	{
		public LinkContext Context { private get; set; }

		public Action<MessageContainer> LogMessage {
			get {
				return Context.LogMessage;
			}
		}

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
			LogMessage (MessageContainer.CreateWarningMessage (Context, message, 2006,
				reflectionMethodCall != null ? MessageOrigin.TryGetOrigin (sourceMethod, reflectionMethodCall.Offset) : new MessageOrigin (sourceMethod),
				"Unrecognized reflection pattern"));
			UnrecognizedPatterns.Add (new ReflectionAccessPattern {
				SourceMethod = sourceMethod,
				ReflectionMethodCall = reflectionMethodCall,
				AccessedItem = accessedItem,
				Message = message
			});
		}
	}
}
