// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using Mono.Cecil;
using Mono.Cecil.Cil;

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

		public IMemberDefinition Source { get; private set; }
		public IMetadataTokenProvider MemberWithRequirements { get; private set; }
		public Instruction Instruction { get; private set; }
		public bool ReportingEnabled { get; private set; }

		public ReflectionPatternContext (
			LinkContext context,
			bool reportingEnabled,
			IMemberDefinition source,
			IMetadataTokenProvider memberWithRequirements,
			Instruction instruction = null)
		{
			_context = context;
			ReportingEnabled = reportingEnabled;
			Source = source;
			MemberWithRequirements = memberWithRequirements;
			Instruction = instruction;

#if DEBUG
			_patternAnalysisAttempted = false;
			_patternReported = false;
#endif
		}

#pragma warning disable CA1822
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
#pragma warning restore CA1822

		public void RecordRecognizedPattern (IMetadataTokenProvider accessedItem, Action mark)
		{
#if DEBUG
			if (!_patternAnalysisAttempted)
				throw new InvalidOperationException ($"Internal error: To correctly report all patterns, when starting to analyze a pattern the AnalyzingPattern must be called first. {Source} -> {MemberWithRequirements}");

			_patternReported = true;
#endif

			mark ();

			if (ReportingEnabled)
				_context.ReflectionPatternRecorder.RecognizedReflectionAccessPattern (Source, Instruction, accessedItem);
		}

		public void RecordUnrecognizedPattern (int messageCode, string message)
		{
#if DEBUG
			if (!_patternAnalysisAttempted)
				throw new InvalidOperationException ($"Internal error: To correctly report all patterns, when starting to analyze a pattern the AnalyzingPattern must be called first. {Source} -> {MemberWithRequirements}");

			_patternReported = true;
#endif

			if (ReportingEnabled)
				_context.ReflectionPatternRecorder.UnrecognizedReflectionAccessPattern (Source, Instruction, MemberWithRequirements, message, messageCode);
		}

		public void Dispose ()
		{
#if DEBUG
			if (_patternAnalysisAttempted && !_patternReported)
				throw new InvalidOperationException ($"Internal error: A reflection pattern was analyzed, but no result was reported. {Source} -> {MemberWithRequirements}");
#endif
		}
	}
}
