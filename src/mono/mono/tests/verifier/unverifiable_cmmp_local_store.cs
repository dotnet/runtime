using System;

class Program
{
   static void Test<T> (T[]t)
   {
       Foo (b: ref t [0], a: t[0]);
   }

   static void Foo<T> (T a, ref T b)
   {
   }

   public static int Main ()
   {
       Test<int>(new [] { 3 });
       return 0;
   }
}