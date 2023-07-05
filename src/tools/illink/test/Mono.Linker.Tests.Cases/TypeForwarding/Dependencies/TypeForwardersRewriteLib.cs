namespace Mono.Linker.Tests.Cases.TypeForwarding
{
	public class C
	{

	}

	public class G<T>
	{
		public class N
		{
		}
	}

	public struct S
	{

	}

	public interface I
	{
		public void Test (C c);
	}
}