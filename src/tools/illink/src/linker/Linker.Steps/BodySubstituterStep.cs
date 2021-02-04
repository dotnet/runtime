using System.Xml.XPath;

namespace Mono.Linker.Steps
{
	public class BodySubstituterStep : ProcessLinkerXmlStepBase
	{
		public BodySubstituterStep (XPathDocument document, string xmlDocumentLocation)
			: base (document, xmlDocumentLocation)
		{
		}

		protected override void Process ()
		{
			new BodySubstitutionParser (Context, _document, _xmlDocumentLocation).Parse (Context.Annotations.MemberActions.PrimarySubstitutionInfo);
		}
	}
}
