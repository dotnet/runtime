using System;

public class LockTest 
{
	public class Test 
	{
		public int val {
			get {
				return(v);
			}
			set {
				v=value;
			}
		}
		
		int v;
	}
	
	public static void Main() {
		Test a=new Test();
		
		lock(a) {
			a.val=2;
		}
		Console.WriteLine("a is " + a.val);
	}
}
