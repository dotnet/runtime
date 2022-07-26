// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace HelloWorld
{
    public class C 
    {
        static void Foo1(MyIterator i) => i.Dispose();
        static void Foo2(MyIterator i) => ((IDisposable)i).Dispose();
    }

    public struct MyIterator: IDisposable
    {
        public void Dispose()
        {
            // in 99% cases iterators have empty Dispose()
        }
    }


    internal class Program
    {
        private static void Main(string[] args)
        {
          
        }
    }
}
