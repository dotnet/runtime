using System;

public class Test {

	public static IntPtr to_intptr (int value)
	{
		return new IntPtr (value);
	}
	
        unsafe public static int Main () {
		int num = 0;

		num++;
		IntPtr a = to_intptr (1);
		if ((int)a != 1)
			return num;
		
		num++;
		if (sizeof (void*) != 4)
			return num;

		num++;
		if (sizeof (byte*) != 4)
			return num;

		num++;
		if (sizeof (int*) != 4)
			return num;

                return 0;
        }
}

