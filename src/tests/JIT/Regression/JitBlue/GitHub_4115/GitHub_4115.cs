// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// This test could fail in GCStress=0xC case, if the object movement happens at the right place and time.
// If the GC guarantees object movement at every collection, the case will fail in that GC stress mode always.
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Issue_4115
{
    public class MyClass
    {
        private GCHandle _pinHandle;
        private IntPtr _dataArrayPtr;
        public ArraySegment<byte> Data;
        public MyClass()
        {
        }

        public object Obj { get; private set; }
        public byte[] Array => Data.Array;
        public int Start { get; set; }
        public int End { get; set; }
        public MyClass Next { get; set; }
    }

    public struct MyIterator
    {
        private static readonly int _vectorSpan = 100;

        private MyClass _class;

        private int _index;

        public bool IsDefault
        {
            get
            {
                return this._class == null;
            }
        }

        public bool IsEnd
        {
            get
            {
                if (this._class == null) {
                    return true;
                }
                if (this._index < this._class.End) {
                    return false;
                }
                for (MyClass next = this._class.Next; next != null; next = next.Next) {
                    if (next.Start < next.End) {
                        return false;
                    }
                }
                return true;
            }
        }

        public MyClass Class
        {
            get
            {
                return this._class;
            }
        }

        public int Index
        {
            get
            {
                return this._index;
            }
        }

        public MyIterator(MyClass clazz)
        {
            this._class = clazz;
            MyClass expr_0E = this._class;
            this._index = ((expr_0E != null) ? expr_0E.Start : 0);
        }

        public MyIterator(MyClass clazz, int index)
        {
            this._class = clazz;
            this._index = index;
        }
    }

    public class MainClass
    {
        private readonly object _returnLock = new object(); 
 
        private MyClass _head; 
        private MyClass _tail; 
        private MyIterator _lastStart;

        public MyIterator TestMethod()
        {
            lock (_returnLock) 
            {
                if (_tail == null) 
                { 
                    return default(MyIterator); 
                }

                // In this assignment there could be a GC hole:
                // gcrReg -[rbx]
                // byrReg +[rbx]
                // add    rbx, 32
                // mov    r15, rbx <--- r15 is not tracked to have a byref pointer.
                // If a GC happens between this instruction and the next one, we have an invalid pointer.
                // gcrReg -[rdi]
                // byrReg +[rdi]
                // mov    rdi, r15
                // New gcrReg live regs = 00000000 { }

                _lastStart = new MyIterator(_tail, _tail.End);
                return _lastStart; 
            } 
        }

        [Fact]
        public static int TestEntryPoint()
        {
            MainClass mainClass = new MainClass();
            mainClass._head = mainClass._tail = new MyClass();
            mainClass._head.End = 4;
            if (mainClass.TestMethod().Class.End == 4)
            {
                Console.WriteLine("Passed");
                return 100;
            }
            else
            {
                Console.WriteLine("Failed");
                return 1;
            }
        }
    }
}
