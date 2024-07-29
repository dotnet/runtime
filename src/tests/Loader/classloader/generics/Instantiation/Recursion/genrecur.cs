// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

#pragma warning disable 0414

// Some generally-useful stuff
public class List<T> { }

public class Utils {
  public static int failures = 0;
  public static void Check(Type expected, Type actual) {
    if (expected != actual) {
      failures++;
      Console.WriteLine("Expected " + expected + " but got " + actual);
    }
  }
}

// Recursion through an instantiated superclass for a non-generic class
class Test1 {
  class C<T>
  {
    public virtual void m(Type expected)
    {
      Utils.Check(expected, typeof(T));
    }
  }

  class D : C<D>
  {
  }

  public static void Test()
  {
    D d = new D();
    C<D> cd = new C<D>();
    cd.m(typeof(D));
    d.m(typeof(D));
  }
}

// Recursion through an instantiated interface for a non-generic class
class Test2
{
  interface I<T>
  {
    void m(T x);
  }

  class D : I<D>
  {
    public void m(D x)
    {
      object obj = this;
      if (!(obj is I<D>)) {
	Utils.failures++;
	Console.WriteLine("Test2.Test failed");
      }
    }
  }

  public static void Test()
  {
    D d = new D();
    I<D> id = d;
    id.m(d);
  }
}
    
// Mutual recursion through an instantiated superclass for a non-generic class
class Test3
{
  class C<T>
  {
    public void mymeth(Type expected)
    {
      Utils.Check(expected, typeof(T));
    }
  }

  class D : C<E>
  {
  }

  class E : C<D>
  {
  }

  public static void Test()
  {
    D d = new D();
    E e = new E();
    C<D> cd = new C<D>();
    C<E> ce = new C<E>();
    d.mymeth(typeof(E));
    e.mymeth(typeof(D));
    cd.mymeth(typeof(D));
    ce.mymeth(typeof(E));
  }
}
    
// Recursion through an instantiated superclass for a generic class
class Test4
{
  class C<T>
  {
    public virtual void m(Type expected)
    {
      Utils.Check(expected, typeof(T));
    }
  }

  class D<T> : C <D<T> >
  {
  }

  public static void Test()
  {
    D<int> di = new D<int>();
    D<string> ds = new D<string>();
    D<object> d = new D<object>();
    di.m(typeof(D<int>));
    ds.m(typeof(D<string>));
    d.m(typeof(D<object>));
  }
}
   
// Mutual recursion through an instantiated superclass for a generic class
class Test5
{
  class C<T>
  {
    public virtual void m(Type expected)
    {
      Utils.Check(expected, typeof(T));
    }
  }

  class D<T> : C< E<T> >
  {
  }

  class E<T> : C< D<T> >
  {
  }

  public static void Test()
  {
    D<int> di = new D<int>();
    D<string> ds = new D<string>();
    D<object> d = new D<object>();
    E<int> ei = new E<int>();
    E<string> es = new E<string>();
    E<object> e = new E<object>();
    di.m(typeof(E<int>));
    ds.m(typeof(E<string>));
    d.m(typeof(E<object>));
    ei.m(typeof(D<int>));
    es.m(typeof(D<string>));
    e.m(typeof(D<object>));
  }
}

// Recursion through an instantiated interface for a generic class
class Test6
{
  interface I<T>
  {
    void m(T x);
  }

  class D<T> : I< D<T> >
  {
    public void m(D<T> x)
    {
      object obj = this;
      if (!(obj is I< D<T> >)) {
	Utils.failures++;
        Console.WriteLine("Test6 failed");
      }
    }
  }

  public static void Test()
  {
    D<int> di = new D<int>();
    D<string> ds = new D<string>();
    D<object> d = new D<object>();
    I< D<int> > idi = di;
    I< D<string> > ids = ds;
    I< D<object> > id = d;
    idi.m(di);
    ids.m(ds);
    id.m(d);
  }
}

// Use of instantiated generic structs inside other structures
// In particular, recursive reference to defining class in a struct field
class Test7
{
  struct Pair<A,B>
  {
    public A fst;
    public B snd;
    public Pair(A a, B b) { fst = a; snd = b; }
  }
  
  struct Triple<A,B,C>
  {
    public A fst;
    public Pair<B,C> snd;
    public Triple(A a, B b, C c) { fst = a; snd = new Pair<B,C>(b,c); }
  }

  class P
  {
    public Triple<int,P,long> fld;
    public P(int x, P y, long z) { fld = new Triple<int,P,long>(x, y, z); }
  }

  public static void Test()
  {
    P p = new P(5, null, 12345678987654321);
    p.fld.snd.fst = p;    
    if (p.fld.snd.snd != 12345678987654321) {
      Console.WriteLine("Test7 failed");
      Utils.failures++;
    }
  }
}    
     
// Polymorphic recursion through classes and fields
class Test8
{
  class Recursive2<A,B>
  {
    public Recursive2<A,B> f1;
    public Recursive2(Recursive2<A,B> a1) { f1 = a1; }
  }
  
  class Mixed2<A,B>
  {
    public Mixed2<A,B> f1;
    public Mixed2<int,int> f2;
    public Mixed2<B,A> f3;
    public Mixed2(Mixed2<A,B> a1, Mixed2<int,int> a2, Mixed2<B,A> a3) { f1 = a1; f2 = a2; f3=a3; }
  }
  
  class NonRecursive2<A,B>
  {
    public NonRecursive2<int,int> f2;
    public NonRecursive2(NonRecursive2<int,int> a2) { f2 = a2; }
  }
  
  class Expansive2<A,B>
  {
    public Expansive2<Expansive2<A,B>, B > f1;
    public Expansive2(Expansive2<Expansive2<A,B>, B> a1) { f1 = a1; }
  }
  
  public static void Test()
  {
    object x1 = new Recursive2<int,int>(null);
    object x2 = new Mixed2<int,int>(null, null, null);
    object x3 = new NonRecursive2<int,int>(null);
    object x4 = new Expansive2<int,int>(null);

    object y1 = new Recursive2<string,string>(null);
    object y2 = new Mixed2<string,string>(null, null, null);
    object y3 = new NonRecursive2<string,string>(null);
    object y4 = new Expansive2<string,string>(null);
  }
}    
     
// Mutual polymorphic recursion through classes, structs and fields, where structs have two fields
class Test9
{
	#pragma warning disable 649
	#pragma warning disable 0414
  class RecursiveClass2<A>
  {
    public RecursiveStruct2<A> f2; //TODO: Assign to default value after compile supports T.default
    public RecursiveClass2() { }
  }
  
  struct RecursiveStruct2<A>
  {
    public RecursiveClass2<A> f1; //TODO: Assign to default value after compile supports T.default
    public RecursiveClass2<A> f2; //TODO: Assign to default value after compile supports T.default
    public RecursiveStruct2(int x) { f1 = null; f2 = null; }
  }
  
  class NonRecursiveClass2<A>
  {
    public NonRecursiveStruct2<int> f2;
    public NonRecursiveClass2() {  }
  }
  
  struct NonRecursiveStruct2<A>
  {
    public NonRecursiveClass2<int> f1;
    public NonRecursiveClass2<int> f2;
    public NonRecursiveStruct2(int x) { f1 = null; f2 = null;  }
  }
  
  class ExpansiveClass2<A>
  {
    public ExpansiveStruct2<ExpansiveClass2<A> > f2; //TODO: Assign to default value after compile supports T.default
    public ExpansiveClass2() {  }
  }
  
  struct ExpansiveStruct2<A>
  {
    public ExpansiveClass2<ExpansiveStruct2<A> > f1; //TODO: Assign to default value after compile supports T.default
    public ExpansiveClass2<ExpansiveStruct2<A> > f2; //TODO: Assign to default value after compile supports T.default
    public ExpansiveStruct2(int x) { f1 = null; f2 = null;  }
  }
  	#pragma warning restore 0414
	#pragma warning restore 649
  
  public static void Test()
  {
    object x1 = new RecursiveClass2<int>();
    object x2 = new RecursiveStruct2<int>(3);
    object x3 = new NonRecursiveClass2<int>();
    object x4 = new NonRecursiveStruct2<int>(3);
    object x5 = new ExpansiveClass2<int>();
    object x6 = new ExpansiveStruct2<int>(3);

    object y1 = new RecursiveClass2<string>();
    object y2 = new RecursiveStruct2<string>(3);
    object y3 = new NonRecursiveClass2<string>();
    object y4 = new NonRecursiveStruct2<string>(3);
    object y5 = new ExpansiveClass2<string>();
    object y6 = new ExpansiveStruct2<string>(3);

  }
}    
     
// Mutual polymorphic recursion through classes, structs and fields, where structs have one field (these are optimized)
class Test10
{
	#pragma warning disable 649
	#pragma warning disable 0414
  class RecursiveClass1<A>
  {
    public RecursiveStruct1<A> f1;
    public RecursiveClass1() { }
  }
  
  struct RecursiveStruct1<A>
  {
    public RecursiveClass1<A> f1;
    public RecursiveStruct1(int x) { f1 = null; }
  }
  
  class NonRecursiveClass1<A>
  {
    public NonRecursiveStruct1<int> f1;
    public NonRecursiveClass1() {  }
  }
  
  struct NonRecursiveStruct1<A>
  {
    public NonRecursiveClass1<int> f1;
    public NonRecursiveStruct1(int x) { f1 = null;  }
  }
  
  class ExpansiveClass1<A>
  {
    public ExpansiveStruct1<ExpansiveClass1<A> > f1;
    public ExpansiveClass1() {  }
  }
  
  struct ExpansiveStruct1<A>
  {
    public ExpansiveClass1<ExpansiveStruct1<A> > f1;
    public ExpansiveStruct1(int x) { f1 = null;  }
  }
  	#pragma warning restore 649
	#pragma warning restore 0414
	
  public static void Test()
  {
    object x1 = new RecursiveClass1<int>();
    object x2 = new RecursiveStruct1<int>(3);
    object x3 = new NonRecursiveClass1<int>();
    object x4 = new NonRecursiveStruct1<int>(3);
    object x5 = new ExpansiveClass1<int>();
    object x6 = new ExpansiveStruct1<int>(3);

    object y1 = new RecursiveClass1<string>();
    object y2 = new RecursiveStruct1<string>(3);
    object y3 = new NonRecursiveClass1<string>();
    object y4 = new NonRecursiveStruct1<string>(3);
    object y5 = new ExpansiveClass1<string>();
    object y6 = new ExpansiveStruct1<string>(3);

  }
}    


// Recursion through base class instantiation at derived class
class Test11 {
  class G<T>
  {
    public virtual void m(Type expected)
    {
      Utils.Check(expected, typeof(T));
    }
  }

  class C : G<D>{}
  
  class D : C {}

  public static void Test()
  {
    D d = new D();
    C c = new C();
    G<D> gd = new G<D>();
    gd.m(typeof(D));
    d.m(typeof(D));
  }
}


// Recursion through base class instantiation at derived class (more complex)
class Test12 {
  class G<T>
  {
    public virtual void m(Type expected)
    {
      Utils.Check(expected, typeof(T));
    }
  }

  class C : G<D>{}
  
  class D : C {}

  class E<T> : D{}

  public static void Test()
  {
    E<G<D>> egd = new E<G<D>>();
    D d = new D();
    C c = new C();
    G<D> gd = new G<D>();
    gd.m(typeof(D));
    d.m(typeof(D));
    egd.m(typeof(D));
  }
}
	 

// Recursion through base class instantiation at derived class (more complex)
class Test13 {
  class G<T>
  {
    public virtual void m(Type expected)
    {
      Utils.Check(expected, typeof(T));
    }
  }

  class C : G<D<E>>{}
  
  class D<T> : C {}

  class E : D<E>{}

  public static void Test()
  {
    E e = new E();
    D<E> d = new D<E>();
    C c = new C();
    G<D<E>> gd = new G<D<E>>();
    gd.m(typeof(D<E>));
    d.m(typeof(D<E>));
    e.m(typeof(D<E>));
  }
}


// Mutual recursion for classes through interface instantiation
class Test14
{
  interface I<T>
  {
    void m(T x);
  }

  class C : I<D>
  {
    public void m(D x)
    {
      object obj = this;
      if (!(obj is I< D >)) {
	Utils.failures++;
        Console.WriteLine("Test14 failed");
      	}
    }
  }

  class D : I<C>
  {
    public void m(C x)
    {
      object obj = this;
      if (!(obj is I< C >)) {
	Utils.failures++;
        Console.WriteLine("Test14 failed");
      	}
    }
  }


  public static void Test()
  {
    D d = new D();
    C c = new C();
	
    I< C > id = d;
    I< D > ic = c;
	
    id.m(c);
    ic.m(d);
  }
}
  

// Mutual recursion through interface instantiation for generic classes
class Test15
{
  interface I<T>
  {
    void m(T x);
  }

  class C<T> : I<D<T>>
  {
    public void m(D<T> x)
    {
      object obj = this;
      if (!(obj is I< D<T> >)) {
	Utils.failures++;
        Console.WriteLine("Test15 failed");
      	}
    }
  }

  class D<T> : I<C<T>>
  {
    public void m(C<T> x)
    {
      object obj = this;
      if (!(obj is I< C<T> >)) {
	Utils.failures++;
        Console.WriteLine("Test15 failed");
      	}
    }
  }

  public static void Test()
  {
    D<int> di = new D<int>();
    D<string> ds = new D<string>();
    D<object> d = new D<object>();
		
    C<int> ci = new C<int>();
    C<string> cs = new C<string>();
    C<object> c = new C<object>();


    I< D<int> > idi = ci;
    I< D<string> > ids = cs;
    I< D<object> > id = c;
	
    idi.m(di);
    ids.m(ds);
    id.m(d);

    I< C<int> > ici = di;
    I< C<string> > ics = ds;
    I< C<object> > ic = d;
	
    ici.m(ci);
    ics.m(cs);
    ic.m(c);


  }
}
  


// Interface instantiation is a derived class
class Test16
{
  interface I<T>
  {
    void m(T x);
  }

  class C : I<D>
  {
    public void m(D x)
    {
      object obj = this;
      if (!(obj is I< D >)) {
	Utils.failures++;
        Console.WriteLine("Test16 failed");
      	}
    }
  }

  class D : C
  {
  }

  public static void Test()
  {
   	D d = new D();
	C c = new C();

	I<D> id = c;
	id.m(d);
  }
}


 // Interface instantiation is a derived generic class
class Test17
{
  interface I<T>
  {
    void m(T x);
  }

  class C<T> : I<D<T>>
  {
    public void m(D<T> x)
    {
      object obj = this;
      if (!(obj is I< D<T> >)) {
	Utils.failures++;
        Console.WriteLine("Test17 failed");
      	}
    }
  }

  class D<T> : C<T>
  {
  }

  public static void Test()
  {
   	D<int> di = new D<int>();
 	D<string> ds = new D<string>();
 	D<object> d = new D<object>();

	D<C<int>> ddi = new D<C<int>>();
	
	C<int> ci = new C<int>();
 	C<string> cs = new C<string>();
 	C<object> co = new C<object>();

	C<C<int>> cci = new C<C<int>>();
	
	I<D<int>> idi = ci;
	I<D<string>> ids = cs;
	I<D<object>> ido = co;

	I<D<C<int>>> idci = cci;

	idi.m(di);
	ids.m(ds);
	ido.m(d);
	
	idci.m(ddi);
	
  }
}


// Direct recursion through interface instantiation, for struct
class Test18
{
  interface I<T>
  {
    void m(T x);
  }

  struct S : I<S>
  {
    public void m(S x)
    {
      object obj = this;
      if (!(obj is I< S >)) {
	Utils.failures++;
        Console.WriteLine("Test18 failed");
      	}
    }
  }

 struct S2<T> : I<S2<T>>
  {
    public void m(S2<T> x)
    {
      object obj = this;
      if (!(obj is I< S2<T> >)) {
	Utils.failures++;
        Console.WriteLine("Test18 failed");
      	}
    }
  }

  public static void Test()
  {
    S s = new S();
    I<S> iofs = s;
    iofs.m(s);

    S2<int> si = new S2<int>();
    I<S2<int>> isi = si;
    isi.m(si);
  }
}

// Recursion through instance field types of classes
class Test19 {
	#pragma warning disable 649
  class C<T>
  {
  	public int i;
	public D<T> d;
  }

  class D<T>
  {
  	public double d;
	public C<T> c;
  }
	#pragma warning restore 649
  public static void Test()
  {
  	C<int> c = new C<int>();
	D<string> d = new D<string>();
	
  }
}


// Recursion through instance field types of structs and classes
class Test20 {
	#pragma warning disable 649
  class C<T>
  {
  	public int i;
	public S<T> s;
  }

  struct S<T>
  {
  	public double d;
	public C<T> c;
  }
	#pragma warning restore 649
	
  public static void Test()
  {
  	C<double> c = new C<double>();

	#pragma warning disable 219
	S<object> s = new S<object>();
	#pragma warning restore 219
	
  }
}


// Recursion through instance field types of structs and classes
// and static struct/class field which is also recursive
class Test21 {
	#pragma warning disable 649
	#pragma warning disable 0414
  class C<T>
  {
  	public int i;
	public S<T> s;
	public static C<T> c = new C<T>();
	public static S<T> st = new S<T>();
  }

  struct S<T>
  {
  	public double d;
	public C<T> c;
	public static S<T> s = new S<T>();
	public static C<T> ct = new C<T>();
  }
	#pragma warning restore 649
	#pragma warning restore 0414
  public static void Test()
  {
  	C<int> c = new C<int>();

	#pragma warning disable 219
	S<int> s = new S<int>();
	#pragma warning restore 219
	
  }
}

// Mutual recursion for structs through interface instantiation
class Test22
{
  interface I<T>
  {
    void m(T x);
  }

  struct S1 : I<S2>
  {
    public void m(S2 x)
    {
      object obj = this;
      if (!(obj is I< S2 >)) {
	Utils.failures++;
        Console.WriteLine("Test22 failed");
      	}
    }
  }

  struct S2 : I<S1>
  {
    public void m(S1 x)
    {
      object obj = this;
      if (!(obj is I< S1 >)) {
	Utils.failures++;
        Console.WriteLine("Test22 failed");
      	}
    }
  }


  public static void Test()
  {
    S2 s2 = new S2();
    S1 s1 = new S1();
	
    I< S1 > is2 = s2;
    I< S2 > is1 = s1;
	
    is2.m(s1);
    is1.m(s2);
  }
}
  

// Mutual recursion through interface instantiation for generic structs
class Test23
{
  interface I<T>
  {
    void m(T x);
  }

  struct S1<T> : I<S2<T>>
  {
    public void m(S2<T> x)
    {
      object obj = this;
      if (!(obj is I< S2<T> >)) {
	Utils.failures++;
        Console.WriteLine("Test25 failed");
      	}
    }
  }

  struct S2<T> : I<S1<T>>
  {
    public void m(S1<T> x)
    {
      object obj = this;
      if (!(obj is I< S1<T> >)) {
	Utils.failures++;
        Console.WriteLine("Test25 failed");
      	}
    }
  }

  public static void Test()
  {
    S2<int> di = new S2<int>();
    S2<string> ds = new S2<string>();
    S2<object> d = new S2<object>();
		
    S1<int> s1i = new S1<int>();
    S1<string> s1s = new S1<string>();
    S1<object> s1 = new S1<object>();


    I< S2<int> > is2i = s1i;
    I< S2<string> > is2s = s1s;
    I< S2<object> > is2 = s1;
	
    is2i.m(di);
    is2s.m(ds);
    is2.m(d);

    I< S1<int> > is1i = di;
    I< S1<string> > is1s = ds;
    I< S1<object> > is1 = d;
	
    is1i.m(s1i);
    is1s.m(s1s);
    is1.m(s1);


  }
}
  

public class M {
  [Fact]
  public static int TestEntryPoint() {
    Test1.Test();
    if (Utils.failures == 0) Console.WriteLine("Test1 OK");
    Test2.Test();
    if (Utils.failures == 0) Console.WriteLine("Test2 OK");
    Test3.Test();
    if (Utils.failures == 0) Console.WriteLine("Test3 OK");
    Test4.Test();
    if (Utils.failures == 0) Console.WriteLine("Test4 OK");
    Test5.Test();
    if (Utils.failures == 0) Console.WriteLine("Test5 OK");
    Test6.Test();
    if (Utils.failures == 0) Console.WriteLine("Test6 OK");
    Test7.Test();
    if (Utils.failures == 0) Console.WriteLine("Test7 OK");
    Test8.Test();
    if (Utils.failures == 0) Console.WriteLine("Test8 OK");
    Test9.Test();
    if (Utils.failures == 0) Console.WriteLine("Test9 OK");
    Test10.Test();
    if (Utils.failures == 0) Console.WriteLine("Test10 OK");
    Test11.Test();
    if (Utils.failures == 0) Console.WriteLine("Test11 OK");
   Test12.Test();
    if (Utils.failures == 0) Console.WriteLine("Test12 OK");
   Test13.Test();
    if (Utils.failures == 0) Console.WriteLine("Test13 OK");
   Test14.Test();
    if (Utils.failures == 0) Console.WriteLine("Test14 OK");
   Test15.Test();
    if (Utils.failures == 0) Console.WriteLine("Test15 OK");
   Test16.Test();
    if (Utils.failures == 0) Console.WriteLine("Test16 OK");
   Test17.Test();
    if (Utils.failures == 0) Console.WriteLine("Test17 OK");
   Test18.Test();
    if (Utils.failures == 0) Console.WriteLine("Test18 OK");
   Test19.Test();
    if (Utils.failures == 0) Console.WriteLine("Test19 OK");
   Test20.Test();
     if (Utils.failures == 0) Console.WriteLine("Test20 OK");
   Test21.Test();
    if (Utils.failures == 0) Console.WriteLine("Test21 OK");
   Test22.Test();
     if (Utils.failures == 0) Console.WriteLine("Test22 OK");
   Test23.Test();
    if (Utils.failures == 0) Console.WriteLine("Test23 OK");
    if (Utils.failures > 0) return 101;
    else 
    {
        Console.WriteLine("PASS");
		return 100;
	}	
  }
}
