// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

// This is needed due to NativeAOT which doesn't enable nullable globally yet
#nullable enable

namespace ILLink.Shared
{
	public readonly struct DiagnosticString
	{
		readonly string _titleFormat;
		readonly string _messageFormat;

		public DiagnosticString (DiagnosticId diagnosticId)
		{
			var resourceManager = SharedStrings.ResourceManager;
			_titleFormat = resourceManager.GetString ($"{diagnosticId}Title") ?? throw new InvalidOperationException ($"{diagnosticId} does not have a matching resource called {diagnosticId}Title");
			_messageFormat = resourceManager.GetString ($"{diagnosticId}Message") ?? throw new InvalidOperationException ($"{diagnosticId} does not have a matching resource called {diagnosticId}Message");
		}

		public DiagnosticString (string diagnosticResourceStringName)
		{
			var resourceManager = SharedStrings.ResourceManager;
			_titleFormat = resourceManager.GetString ($"{diagnosticResourceStringName}Title") ?? throw new InvalidOperationException ($"{diagnosticResourceStringName} does not have a matching resource called {diagnosticResourceStringName}Title");
			_messageFormat = resourceManager.GetString ($"{diagnosticResourceStringName}Message") ?? throw new InvalidOperationException ($"{diagnosticResourceStringName} does not have a matching resource called {diagnosticResourceStringName}Message");
		}

		public string GetMessage (params string[] args) =>
			string.Format (_messageFormat, args);

		public string GetMessageFormat () => _messageFormat;

		public string GetTitle (params string[] args) =>
			string.Format (_titleFormat, args);

		public string GetTitleFormat () => _titleFormat;
	}
}
