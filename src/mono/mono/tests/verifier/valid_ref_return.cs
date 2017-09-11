using System;

class Foo {
	static int X = 10;
	static int[] Arr = new int[1];
	int y;

	static void Main () {
	}

	static ref int ReturnStatic () {
		return ref X;
	}

	ref int ReturnField () {
		return ref this.y;
	}

	ref int ReturnArrayElement () {
		return ref Arr [0];
	}

	ref int ReturnArg (ref int arg) {
		return ref arg;
	}

	ref int TwoReturns (bool b) {
		if (b) 
			return ref X;
		else
			return ref Arr [0];
	}

	ref int LocalVarRet (bool b) {
		ref int x = ref X;
		ReturnArg (ref x);
		return ref x;
	}

	ref int ReturnRet (ref int arg) {
		return ref ReturnArg (ref arg);
	}

	ref int ReturnFromCatch () {
		try {
			return ref X;
		} catch (Exception) {
			return ref X;
		} 
	}

	ref int ReturnFunc () {
		return ref ReturnStatic ();
	}
}
