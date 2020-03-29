namespace Mono.Linker
{
	public enum AssemblyAction
	{
		Skip,
		Copy,
		CopyUsed,
		Link,
		Delete,
		Save,
		AddBypassNGen,
		AddBypassNGenUsed
	}
}
