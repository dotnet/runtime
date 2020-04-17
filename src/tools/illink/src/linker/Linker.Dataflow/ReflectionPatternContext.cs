// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Mono.Cecil;
using System;
using System.Diagnostics;

namespace Mono.Linker.Dataflow
{
	/// <summary>
	/// Helper struct to pass around context information about reflection pattern
	/// as a single parameter (and have a way to extend this in the future if we need to easily).
	/// Also implements a simple validation mechanism to check that the code does report patter recognition
	/// results for all methods it works on.
	/// The promise of the pattern recorder is that for a given reflection method, it will either not talk
	/// about it ever, or it will always report recognized/unrecognized.
	/// </summary>
	struct ReflectionPatternContext : IDisposable
	{
		readonly LinkContext _context;
#if DEBUG
		bool _patternAnalysisAttempted;
		bool _patternReported;
#endif

		public MethodDefinition MethodCalling { get; private set; }
		public MethodDefinition MethodCalled { get; private set; }
		public int InstructionIndex { get; private set; }

		public ReflectionPatternContext (LinkContext context, MethodDefinition methodCalling, MethodDefinition methodCalled, int instructionIndex)
		{
			_context = context;
			MethodCalling = methodCalling;
			MethodCalled = methodCalled;
			InstructionIndex = instructionIndex;

#if DEBUG
			_patternAnalysisAttempted = false;
			_patternReported = false;
#endif
		}

		[Conditional ("DEBUG")]
		public void AnalyzingPattern ()
		{
#if DEBUG
			_patternAnalysisAttempted = true;
#endif
		}

		[Conditional ("DEBUG")]
		public void RecordHandledPattern ()
		{
#if DEBUG
			_patternReported = true;
#endif
		}

		public void RecordRecognizedPattern<T> (T accessedItem, Action mark)
			where T : IMemberDefinition
		{
#if DEBUG
			if (!_patternAnalysisAttempted)
				throw new InvalidOperationException ($"Internal error: To correctly report all patterns, when starting to analyze a pattern the AnalyzingPattern must be called first. {MethodCalling} -> {MethodCalled}");

			_patternReported = true;
#endif

			mark ();
			_context.ReflectionPatternRecorder.RecognizedReflectionAccessPattern (MethodCalling, MethodCalled, accessedItem);
		}

		public void RecordUnrecognizedPattern (string message)
		{
#if DEBUG
			if (!_patternAnalysisAttempted)
				throw new InvalidOperationException ($"Internal error: To correctly report all patterns, when starting to analyze a pattern the AnalyzingPattern must be called first. {MethodCalling} -> {MethodCalled}");

			_patternReported = true;
#endif

			_context.ReflectionPatternRecorder.UnrecognizedReflectionAccessPattern (MethodCalling, MethodCalled, message);
		}

		public void Dispose ()
		{
#if DEBUG
			if (_patternAnalysisAttempted && !_patternReported)
				throw new InvalidOperationException ($"Internal error: A reflection pattern was analyzed, but no result was reported. {MethodCalling} -> {MethodCalled}");
#endif
		}
	}
}
