using NUnit.Framework;

namespace Mono.Linker.Tests
{
	[TestFixture]
	public class MessageContainerTests
	{
		[Test]
		public void MSBuildFormat ()
		{
			LinkContext context = new LinkContext (new Pipeline ());

			var msg = MessageContainer.CreateErrorMessage ("text", 1000);
			Assert.AreEqual ("ILLink: error IL1000: text", msg.ToMSBuildString ());

			msg = MessageContainer.CreateWarningMessage (context, "message", 2001, new MessageOrigin ("logtest", 1, 1));
			Assert.AreEqual ("logtest(1,1): warning IL2001: message", msg.ToMSBuildString ());

			msg = MessageContainer.CreateInfoMessage ("log test");
			Assert.AreEqual ("ILLink: log test", msg.ToMSBuildString ());
		}
	}
}
