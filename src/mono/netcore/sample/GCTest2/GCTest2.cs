using System;
using System.Threading;

namespace GCTest {
	class ListNode
	{
		public int i;
		public ListNode Next;

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
				cur.Next = new ListNode(i);
				cur = cur.Next;
			}

			return head;
		}

		public static (ListNode,ListNode) MakeListWithEnd(int len)
		{
			ListNode head = new ListNode(0);
			ListNode cur = head;

			for (int i = 1; i < len; i++)
			{
				cur.Next = new ListNode(i);
				cur = cur.Next;
			}

			return (head, cur);
		}


		public static void Main(string[] args)
		{
			int short_lived_length = 100;
			int long_lived_segment_length = 100;
			int iterations = 10000;

			ListNode short_lived_a;
			ListNode short_lived_b;
			ListNode short_lived_c;
			var (long_lived, long_lived_end) = MakeListWithEnd(long_lived_segment_length);


			
			for (int i = 0; i < iterations; i++)
			{
				short_lived_a = MakeList(short_lived_length);
				short_lived_b = MakeList(short_lived_length);
				short_lived_c = MakeList(short_lived_length);

				if(i % 1000 == 0)
				{
					var (new_segment_head, new_segment_end) = MakeListWithEnd(long_lived_segment_length);
					long_lived_end.Next = new_segment_head;
					long_lived_end = new_segment_end;
				}


			}

			GC.Collect();
		}


	}
}
