// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ILLink.Shared
{
	public readonly struct DiagnosticString
	{
		readonly string _titleFormat;
		readonly string _messageFormat;

		public DiagnosticString (DiagnosticId diagnosticId)
		{
			var resourceManager = SharedStrings.ResourceManager;
			_titleFormat = resourceManager.GetString ($"{diagnosticId}Title");
			_messageFormat = resourceManager.GetString ($"{diagnosticId}Message");
		}

		public DiagnosticString (string diagnosticResourceStringName)
		{
			var resourceManager = SharedStrings.ResourceManager;
			_titleFormat = resourceManager.GetString ($"{diagnosticResourceStringName}Title");
			_messageFormat = resourceManager.GetString ($"{diagnosticResourceStringName}Message");
		}

		public string GetMessage (params string[] args) =>
			string.Format (_messageFormat, args);

		public string GetMessageFormat () => _messageFormat;

		public string GetTitle (params string[] args) =>
			string.Format (_titleFormat, args);

		public string GetTitleFormat () => _titleFormat;
	}
}
