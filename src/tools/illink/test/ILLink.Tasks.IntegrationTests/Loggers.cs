using Xunit.Abstractions;
using Xunit.Sdk;

namespace ILLink.Tests
{
	public interface ILogger
	{
		void LogMessage (string message);
	}

	public class TestLogger : ILogger
	{
		private ITestOutputHelper output;
		public TestLogger (ITestOutputHelper output)
		{
			this.output = output;
		}
		public void LogMessage (string message)
		{
			output.WriteLine (message);
		}
	}

	public class FixtureLogger : ILogger
	{
		private IMessageSink messageSink;
		public FixtureLogger (IMessageSink messageSink)
		{
			this.messageSink = messageSink;
		}
		public void LogMessage (string message)
		{
			messageSink.OnMessage (new DiagnosticMessage (message));
		}
	}
}