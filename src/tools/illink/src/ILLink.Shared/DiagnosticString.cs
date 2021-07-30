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

		public string GetMessage (params string[] args) =>
			string.Format (_messageFormat, args);

		public string GetMessageFormat () => _messageFormat;

		public string GetTitle (params string[] args) =>
			string.Format (_titleFormat, args);

		public string GetTitleFormat () => _titleFormat;
	}
}
