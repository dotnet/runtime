using NUnit.Framework;

namespace Mono.Linker.Tests
{
	[TestFixture]
	public class MessageContainerTests
	{
		[Test]
		public void MSBuildFormat ()
		{
			var msg = MessageContainer.CreateErrorMessage ("text", 1000);
			Assert.AreEqual ("illinker: error IL1000: text", msg.ToMSBuildString ());

			msg = MessageContainer.CreateWarningMessage ("message", 2001, origin: new MessageOrigin("logtest", 1, 1));
			Assert.AreEqual ("logtest(1,1): warning IL2001: message", msg.ToMSBuildString ());

			msg = MessageContainer.CreateInfoMessage ("log test");
			Assert.AreEqual ("illinker: log test", msg.ToMSBuildString ());
		}
	}
}
