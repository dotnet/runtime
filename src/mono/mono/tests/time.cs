using System;

class time_test {
	
	static int Main ()
	{
		DateTime uepoch = new DateTime (1970, 1, 1);

		if (uepoch.Ticks != 621355968000000000)
			return 1;
				
		return 0;
	}
}
