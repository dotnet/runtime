public interface IHelloWorldWriter
{
	int WriteIt();
}

public class RealWriter : IHelloWorldWriter
{
	public int WriteIt()
	{
		return 33;
	}
}

public class ProjectName {
	static int Main()
	{
       		IHelloWorldWriter writer = new RealWriter();
		if (writer.WriteIt() != 33)
			return 1;
		return 0;
    	}
}

