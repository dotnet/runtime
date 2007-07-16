using System;

public class Library {

	public Library ()
	{
	}

	public int Shebang ()
	{
		return Bang ();
	}

	protected virtual int Bang ()
	{
		return 1;
	}
}


public class PowerFulLibrary : Library {

	protected override int Bang ()
	{
		return 0;
	}
}

[NotLinked, AttributeUsage (AttributeTargets.All)]
public class NotLinkedAttribute : Attribute {
}
