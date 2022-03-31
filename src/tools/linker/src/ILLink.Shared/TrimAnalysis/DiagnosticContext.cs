// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace ILLink.Shared.TrimAnalysis
{
	readonly partial struct DiagnosticContext
	{
		public partial void AddDiagnostic (DiagnosticId id, params string[] args);
	}
}
