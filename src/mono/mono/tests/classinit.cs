using System;

class Foo {

        static public int i = 0;
}

class Bar {

        static public int j;

        static Bar () {
                j = Foo.i;
        }
}

class Bug {

        static public int Main () {
                Foo.i = 5;
		if (Bar.j != 5)
			return 1;

		return 0;
        }
}
