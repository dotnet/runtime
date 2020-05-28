class C
{
  public static int Foo<T> ()
  {
    return 1;
  }
  
  public static int Foo<T1, T2> ()
  {
    return 0;
  }
  
  public static int Main ()
  {
    return Foo<int, int> ();
  }
}

