
#nullable enable

namespace ILLink.Shared
{
	internal static class MessageFormat
	{
		public static string FormatRequiresAttributeMessageArg (string? message)
		{
			string arg1 = "";
			if (!string.IsNullOrEmpty (message))
				arg1 = $" {message}{(message!.TrimEnd ().EndsWith (".") ? "" : ".")}";
			return arg1;
		}

		public static string FormatRequiresAttributeUrlArg (string? url)
		{
			string arg2 = "";
			if (!string.IsNullOrEmpty (url))
				arg2 = " " + url;
			return arg2;
		}

		public static string FormatRequiresAttributeMismatch (bool memberHasAttribute, bool isInterface, string var0, string var1, string var2)
		{
			string format = (memberHasAttribute, isInterface) switch {
				(false, true) => SharedStrings.InterfaceRequiresMismatchMessage,
				(true, true) => SharedStrings.ImplementationRequiresMismatchMessage,
				(false, false) => SharedStrings.BaseRequiresMismatchMessage,
				(true, false) => SharedStrings.DerivedRequiresMismatchMessage
			};
			return string.Format (format, var0, var1, var2);
		}
	}
}