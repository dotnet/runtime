using System;
using System.Threading;

namespace GCTest {
	class ListNode
	{
		public int i;
		public ListNode next;

		public ListNode(int i)
		{
			this.i = i;
		}
	
		public static ListNode MakeList(int len)
		{
			ListNode head = new ListNode(0);
			ListNode cur = head;

			for (int i = 1; i < len; i++)
			{
				cur.next = new ListNode(i);
				cur = cur.next;
			}

			return head;
		}


		public static void Main(string[] args)
		{
			int list_length = 10000000;
			ListNode a;
			ListNode b;
			ListNode c;
			ListNode d;
			

			a = MakeList(list_length);
			b = MakeList(list_length);
			c = MakeList(list_length);
			d = MakeList(list_length);

			GC.Collect();
		}


	}
}
