// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

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
