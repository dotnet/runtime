using System;

public class Driver {
	static float D = 3;

	public static int StoreStaticField (float y) {
		D = y;
		return 0;
	}

	public static float ReadSingle () {
		Object o = "";
		o.ToString ();
		return 64f;
	}

	public static int TestStoreArray() {
		float[] arr = new float[10];
		 arr[0] = ReadSingle();
		return 0;
	}


	public static int Main () {
		int res = 0;
		res = StoreStaticField (128f);
		res |= TestStoreArray();

		return res;
	}
}
