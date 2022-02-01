#if NETSTANDARD
// Allow use of init setters on downlevel frameworks.
namespace System.Runtime.CompilerServices
{
	public sealed class IsExternalInit
	{
	}
}
#endif