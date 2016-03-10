// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;

namespace CrossThds
{
        using System.Reflection;
	using System.Threading;

	class Node
	{
		int [] mem;
		public Node Next;
		public Node Last;
		public Node()
		{
			mem= new int[256]; //1K
			mem[0] = 0;
			mem[255] = 256;
			Next = null;
			Last = null;
		}
	}

	class RefCrossThds
	{
		static Object [] ObjAry = new Object[2];

		public RefCrossThds()
		{
// console synchronization Console.SetOut(TextWriter.Synchronized(Console.Out));
		}

        private static void ThreadAbort(Thread thread)
        {
            MethodInfo abort = null;
            foreach(MethodInfo m in thread.GetType().GetMethods(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (m.Name.Equals("AbortInternal") && m.GetParameters().Length == 0) abort = m;
            }
            if (abort == null) {
                throw new Exception("Failed to get Thread.Abort method");
            }
            abort.Invoke(thread, new object[0]);
         }

		public static int Main(String [] str)
		{
			Console.Out.WriteLine( "RefCrossThds");
			Console.Out.WriteLine( "Should exit with a 100");
			// console synchronization Console.SetOut(TextWriter.Synchronized(Console.Out));
			Thread [] thd = new Thread[2];
			RefCrossThds mainobj = new RefCrossThds();

			for(int i=0; i< 2; i++)
			{
				ObjAry[i] = new Node();
			}

			for( int i=0; i<2; i++ )
			{
				thd[i] = new Thread( new ThreadStart( mainobj.RunThread ) );
				thd[i].Start();
			}
			Thread.Sleep(5000);
			for(int i=0; i< 2; i++)
			{
				ThreadAbort(thd[i]);
//				thd[i].Join();
			}
			Console.Out.WriteLine( "Test Passed");
			return 100;
		}

		void RunThread()
		{
			Random Ran;
			Ran = new Random();
			while(true)
			{
				int iRand = Ran.Next(0, 512);
				if( iRand%2 == 0 )
				    lock (ObjAry[0]) {DoDoubLink(ObjAry[0], iRand/2);}
				else
					lock (ObjAry[1]) {DoSingLink(ObjAry[1], iRand/2+1);}
			}
		}

	//**create or delete a node from double link list.							**/
	//**If the passin index is smaller than the length of the double link list, **/
	//**delete the object at the index, otherwise add a new object to the list.	**/
		static
		void DoDoubLink(Object head, int index)
		{
			int depth = 0;

			Node Current = (Node)head;
			bool bAdd;
			while( true )
			{
				if( Current.Next == null)
				{
					bAdd = true;
					break;
				}
				else
				{
					if( index == depth )
					{
						bAdd = false;
						break;
					}
					depth++;
				}
				Current = Current.Next;
			}

			if( bAdd )
			{
				Current.Next = new Node();
				Current.Next.Last = Current;
			}
			else
			{
				Current.Last = Current.Next;
				Current.Next.Last = Current.Last;
			}
		}

	//**create or delete a node from single link list.							**/
	//**If the passin index is smaller than the length of the single link list, **/
	//**delete the object at the index, otherwise add a new object to the list.	**/
		static
		void DoSingLink(Object head, int index)
		{
			int depth = 0;
			Node Current = (Node)head;
			bool bAdd;
			while( true )
			{
				if( Current.Next == null)
				{
					bAdd = true;
					break;
				}
				else
				{
					if( index == depth )
					{
						bAdd = false;
						break;
					}
					depth++;
				}
				Current = Current.Next;
			}

			if( bAdd )
			{
				Current.Next = new Node();
			}
			else
			{
				Current.Last = Current.Next;
			}
		}
	}

}//end of namespace