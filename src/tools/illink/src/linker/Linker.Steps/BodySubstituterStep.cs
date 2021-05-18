using System.IO;

namespace Mono.Linker.Steps
{
	public class BodySubstituterStep : ProcessLinkerXmlStepBase
	{
		public BodySubstituterStep (Stream documentStream, string xmlDocumentLocation)
			: base (documentStream, xmlDocumentLocation)
		{
		}

		protected override void Process ()
		{
			new BodySubstitutionParser (Context, _documentStream, _xmlDocumentLocation).Parse (Context.Annotations.MemberActions.PrimarySubstitutionInfo);
		}
	}
}
