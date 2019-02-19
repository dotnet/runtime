using System;
namespace Mono.Linker
{
	public enum MessageImportance
	{
		High,
		Low,
		Normal,
	}

	public interface ILogger
	{
		void LogMessage (MessageImportance importance, string message, params object[] values);
	}
}
