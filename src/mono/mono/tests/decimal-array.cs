using System;

class Test
{
	public static int Main()
	{
		decimal[,] tab = new decimal[2,2] {{3,4},{5,6}};
		bool b1 = false;
		decimal d;

		try {
			d = tab[1,2];
		} catch (Exception e) {
			b1 = true;
		}

		if (!b1)
			return 1;
		
		d = tab[1,1];
		if (d != 6)
			return 1;
		
		return 0;
	}
}


