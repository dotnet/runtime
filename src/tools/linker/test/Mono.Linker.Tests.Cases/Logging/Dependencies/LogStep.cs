using System;
using Mono.Linker;
using Mono.Linker.Steps;

namespace Log
{
	public class LogStep : IStep
	{
		public void Process (LinkContext context)
		{
			var msgError = MessageContainer.CreateErrorMessage ("Error", 1004);
			var msgWarning = MessageContainer.CreateWarningMessage (context, "Warning", 2001, origin: new MessageOrigin ("logtest", 1, 1), version: WarnVersion.Latest);
			var msgInfo = MessageContainer.CreateInfoMessage ("Info");
			var msgDiagnostics = MessageContainer.CreateDiagnosticMessage ("Diagnostics");
			context.LogMessage (msgError);
			context.LogMessage (msgWarning);
			context.LogMessage (msgInfo);
			context.LogMessage (msgDiagnostics);
		}
	}
}
