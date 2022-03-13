// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

// Test thread safety of type and method instantiation
// Usage: polyrec <nthreads> <ninsts> 
// where nthreads is the number of threads to create
// and niters it the number of type/method instantiations to create each thread
using System;
using System.Threading;
using Xunit;

// Spice things up a bit with some mutual recursion between instantiations
class C<T>
{
}

class D<T> : C< E<T> >
{
}

class E<T> : C< D<T> >
{
}

public class P 
{ 
  public static int nthreads;
  public static int ninsts;
  public static object x;

  // By the magic of polymorphic recursion we get n instantiations of D
  // and n instantiations of genmeth
  public static void genmeth<S>(int n)
  {
    if (n==0) return;
    else
    {
      x = new D<S>();
      genmeth< D<S> >(n-1);
    }
  }

  // By the magic of polymorphic recursion we get n instantiations of D
  // and n instantiations of genmeth
  public static void genmeth2<S>(int n)
  {
    if (n==0) return;
    else
    {
      x = new E<S>();
      genmeth2< E<S> >(n-1);
    }
  }

  public static void Start()
  {
    genmeth<string>(ninsts);
      Console.WriteLine("Aux thread exited");
  }

  public static void Start2()
  {
    genmeth2<string>(ninsts);
  }

  public static void Test(int threads, int insts)
  {
    nthreads = threads;
    ninsts = insts;

    for (int i = 0; i < nthreads; i++)
    {
      Thread t = new Thread(i % 2 == 0 ? new ThreadStart(Start) : new ThreadStart(Start2));
      t.Name = "Thread " + i;	
      t.Start();
    }

    Console.WriteLine("Main thread exited");
  }

  [Fact]
  public static void Test_4_50()
  {
    Test(4, 50);
  }
}
