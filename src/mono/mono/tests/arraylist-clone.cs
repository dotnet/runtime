

using System.IO;
using System;
using System.Collections;

namespace T {
	public class T {
		string name="unset";

		T(string n) {
			name=n;
		}

		public static int Main () {
			ArrayList tlist=new ArrayList(), newlist;
			T[] tarray = new T [2];
			T t1=new T("t1");
			T t2=new T("t2");
			tlist.Add(t1);
			tlist.Add(t2);

			newlist=(ArrayList)tlist.Clone();
			newlist.CopyTo (tarray);

			if (tarray [0].name != "t1")
				return 1;
			if (tarray [1].name != "t2")
				return 2;
			
			return 0;
		}
	}
}
