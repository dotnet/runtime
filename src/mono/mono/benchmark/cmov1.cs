using System;
class T {
	static void Main () {
		int y = 0;
		int z = 1;
		for (int i = 0; i < 500000000; i ++) {
			if (y == 0)
				z = i;
			
			if (y == 4)
				y = i;
			
			if (z == 0)
				y = 1;
			
			if (y == 1)
				z = i;
			
			if (y == 1)
				y = i;
			
			if (z == 2)
				y = z;
			else
				y = i;
		}
	}
	
}