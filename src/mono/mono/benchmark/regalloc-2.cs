class T {
	static void Main ()
	{
		int j = 0, k = 0, l = 0;
		for (int i = 0; i < 50000000; i ++) {
			int a = i ^ 1;
			int b = a ^ 2;
			int c = b ^ 3;
			int d = c ^ 4;
			int e = d ^ 5;
			int f = e ^ 6;
			int g = f ^ 7;
			int h = g ^ 8;
			
			j ^= h;
			k ^= h + 1;
			l ^= h & 5;
			
			j ^= l;
			k ^= k + 1;
			l ^= j & 5;
			
			j ^= l;
			k ^= k + 1;
			l ^= j & 5;
		}
	}
}