using System.Collections.Generic;

namespace Mono.Linker.Tests.TestCasesRunner
{
	public class LinkerTestLogger : ILogger
	{
		public struct MessageRecord
		{
			public string Message;
			public MessageCategory Category;
			public MessageOrigin? Origin;
			public int? Code;
			public string Text;
			public string OriginMemberDefinitionFullName;
		}

		public List<MessageRecord> Messages { get; private set; } = new List<MessageRecord> ();

		public void LogMessage (MessageContainer msBuildMessage)
		{
			Messages.Add (new MessageRecord () {
				Message = msBuildMessage.ToString (),
				Category = msBuildMessage.Category,
				Origin = msBuildMessage.Origin,
				Code = msBuildMessage.Code,
				Text = msBuildMessage.Text,
				OriginMemberDefinitionFullName = msBuildMessage.Origin?.MemberDefinition?.FullName
			});
		}
	}
}