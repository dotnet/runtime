using System;
using Mono.Linker;
using Mono.Linker.Steps;

namespace Log
{
	public class LogStep : IStep
	{
		public void Process (LinkContext context)
		{
			var msgError = MessageContainer.CreateCustomErrorMessage ("Error", 6001);
			var msgWarning = MessageContainer.CreateCustomWarningMessage (context, "Warning", 6002, origin: new MessageOrigin ("logtest", 1, 1), version: WarnVersion.Latest);
			var msgInfo = MessageContainer.CreateInfoMessage ("Info");
			var msgDiagnostics = MessageContainer.CreateDiagnosticMessage ("Diagnostics");
			context.LogMessage (msgError);
			context.LogMessage (msgWarning);
			context.LogMessage (msgInfo);
			context.LogMessage (msgDiagnostics);
		}
	}
}
