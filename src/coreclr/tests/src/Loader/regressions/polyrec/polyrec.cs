// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

// Test thread safety of type and method instantiation
// Usage: polyrec <nthreads> <ninsts> 
// where nthreads is the number of threads to create
// and niters it the number of type/method instantiations to create each thread
using System;
using System.Threading;

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

  public static int Main(String[] args)
  {
    if (args.Length < 2)
    {
      Console.WriteLine("Usage: polyrec <nthreads> <ninsts>");
      return 99;
    }

    nthreads = Int32.Parse(args[0]);
    ninsts = Int32.Parse(args[1]);

    for (int i = 0; i < nthreads; i++)
    {	
      Thread t = new Thread(i % 2 == 0 ? new ThreadStart(Start) : new ThreadStart(Start2));
      t.Name = "Thread " + i;	
      t.Start();
    }
      Console.WriteLine("Main thread exited");
    return 100;
  }
}