#if XUNIT_NULLABLE
#nullable enable
#endif

namespace Xunit.Sdk
{
	/// <summary>
	/// This is a marker interface implemented by all built-in assertion exceptions so that
	/// test failures can be marked with <see cref="F:Xunit.v3.FailureCause.Assertion"/>.
	/// </summary>
	public interface IAssertionException
	{ }
}
