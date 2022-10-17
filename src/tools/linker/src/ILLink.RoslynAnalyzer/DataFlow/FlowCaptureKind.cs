// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable disable

namespace ILLink.RoslynAnalyzer.DataFlow
{
	// Copied from https://github.com/dotnet/roslyn/blob/c8ebc8682889b395fcb84c85bf4ff54577377d26/src/Workspaces/SharedUtilitiesAndExtensions/Compiler/Core/FlowAnalysis/FlowCaptureKind.cs
	/// <summary>
	/// Indicates the kind of flow capture in an <see cref="IFlowCaptureOperation"/>.
	/// </summary>
	public enum FlowCaptureKind
	{
		/// <summary>
		/// Indicates an R-Value flow capture, i.e. capture of a symbol's value.
		/// </summary>
		RValueCapture,

		/// <summary>
		/// Indicates an L-Value flow capture, i.e. captures of a symbol's location/address.
		/// </summary>
		LValueCapture,

		/// <summary>
		/// Indicates both an R-Value and an L-Value flow capture, i.e. captures of a symbol's value and location/address.
		/// These are generated for left of a compound assignment operation, such that there is conditional code on the right side of the compound assignment.
		/// </summary>
		LValueAndRValueCapture
	}
}
