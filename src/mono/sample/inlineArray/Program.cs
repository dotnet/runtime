using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace InlineArrayTest
{
	[InlineArray(42)]
	struct MyArray<T> 
	{ 
		public T _element0;
		public Span<T> SliceExample()
		{
			return MemoryMarshal.CreateSpan(ref _element0, 42);
		}
	} 

	public class Program
	{
		public static void Main (string[] args)
		{
			MyArray<int> array;
			array._element0 = 5;
			Span<int> arraySpan = array.SliceExample();
			for (int i = 0; i < arraySpan.Length; i++) {
				Console.WriteLine(arraySpan[i]);
			}
		}
	}
}
