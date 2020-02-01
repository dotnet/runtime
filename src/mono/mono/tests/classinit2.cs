class A {   
  static A() { System.Console.WriteLine("A"); M.a_run = true;}
  public static void f() {}
}
class B {
  static B() { System.Console.WriteLine("B"); A.f(); M.b_run = true;}
  public static void f() {}
}
class M {
  public static bool b_run = false;
  public static bool a_run = false;
  public static int Main() { 
	B.f(); 
	if (!a_run)
		return 1;
	if (!b_run)
		return 2;
	return 0;
  }
}
