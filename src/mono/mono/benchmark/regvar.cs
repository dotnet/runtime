//
// We get stuff like:
//  48:	8b c3                	mov    eax,ebx
//  4a:	8b cf                	mov    ecx,edi
//  4c:	0b c1                	or     eax,ecx
//  4e:	8b d8                	mov    ebx,eax
//

using System;
class T {
	static void Main () {
		int i = Environment.TickCount;
		new T ().X ();
		Console.WriteLine (Environment.TickCount - i);
	}
	
	void X () {
		int a = 0, b = 0, c = 0, d = 0;
		for (int i = 0; i < 50000000; i ++) {
			
			
			a |= b;
			b |= c;
			c |= d;
			b |= d;
			
			a ^= b;
			b ^= c;
			c ^= d;
			b ^= d;
			
			a &= b;
			b &= c;
			c &= d;
			b &= d;
			
			a += b;
			b += c;
			c += d;
			b += d;
		}
	}
}