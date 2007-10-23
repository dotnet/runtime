using System;
using System.Threading;
using System.Runtime.CompilerServices;

namespace TestLocks
{
        class MainClass
        {

                public static int Main(string[] args)
                {
                        MainClass MainClassInstance=new MainClass();
                        return MainClassInstance.LockMethod();
                }

                [MethodImpl(MethodImplOptions.Synchronized)]
                public virtual int LockMethod()
                {
			try {
                        	Monitor.PulseAll(this);
				return 0;
			} catch {
                        	Console.WriteLine("failed");
				return 1;
			}
                }
        }
}
