using System;

struct Point {
    public int x;
    public int y;
}

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

    Point mp;

    ref int Pick (bool b, ref Point p) {
        if (b)
            return ref p.x;
        else
            return ref p.y;
    }

    void F (bool b) {
        Point lp = new Point {x = 3, y = 3};
        ref int z = ref Pick (b, ref lp);
        z = 4;
        ref int z2 = ref Pick (b, ref mp);
        z2 = 5;
    }
}
