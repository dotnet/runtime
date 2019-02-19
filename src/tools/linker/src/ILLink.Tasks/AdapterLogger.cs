using System;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace ILLink.Tasks
{
	class AdapterLogger : Mono.Linker.ILogger
	{
		private TaskLoggingHelper log;

		public AdapterLogger (TaskLoggingHelper log)
		{
			this.log = log;
		}

		public void LogMessage (Mono.Linker.MessageImportance importance, string message, params object[] values)
		{
			Microsoft.Build.Framework.MessageImportance msBuildImportance;
			switch (importance)
			{
				case Mono.Linker.MessageImportance.High:
					msBuildImportance = MessageImportance.High;
					break;
				case Mono.Linker.MessageImportance.Normal:
					msBuildImportance = MessageImportance.Normal;
					break;
				case Mono.Linker.MessageImportance.Low:
					msBuildImportance = MessageImportance.Low;
					break;
				default:
					throw new ArgumentException ($"Unrecognized importance level {importance}", nameof(importance));
			}

			log.LogMessageFromText (String.Format (message, values), msBuildImportance);
		}
	}
}
