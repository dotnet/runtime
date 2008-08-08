using System;

public class Program {
		
	internal class Button  : ContextBoundObject
	{
		public int Counter (int? x)
		{
			if (x == null)
				return 0;
			return x.Value + 1;
		}
		
		public static Button TheButton = new Button ();
	}

	public static int Main ()
	{
		// Test remoting and nullables
		if (Button.TheButton.Counter (1) != 2)
			return 1;

		int?[] x = new int?[] { null };
		return x.GetValue (0) == null ? 0 : 2;
	}
}