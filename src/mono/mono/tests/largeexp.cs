using System;

class Test {

	private static int[] crc_lookup=new int[256];

	public byte[] header_base = new byte [10];
	public int header = 0;

	internal int checksum() {
		uint crc_reg=0;
		int i = 5;
		
		crc_reg = (crc_reg<<8)^(uint)(crc_lookup[((crc_reg >> 24)&0xff)^(header_base[header+i]&0xff)]);

		return 0;

	}
	
	
	public static int Main () {
		Test t1 = new Test ();
		
		return t1.checksum();
	}
}
