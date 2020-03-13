using MonoTests.Helpers;

public class Program {
	public static void Main (string[] args)
	{
		OOMHelpers.RunTest ("sgen-new-threads-dont-join-stw-2");
	}
}
