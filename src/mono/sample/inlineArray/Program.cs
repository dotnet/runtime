using System;
using System.Runtime.CompilerServices;

namespace InlineArrayTest
{
    [InlineArray(42)]
	public struct Test {
		public char singleChar;
	};

	[InlineArray(45)] 
	struct MyArray<T> 
	{ 
		public T _element0; 
		// public Span<T> SliceExample() 
		// { 
		// 	return MemoryMarshal.CreateSpan(ref _element0, 42); 
		// } 
	} 

	public class Program
	{
		public static void Main (string[] args)
		{
			Test abc;
			abc.singleChar = 'a';
			Console.WriteLine(abc.singleChar);
			MyArray<int> array;
			array._element0 = 5;
		}
	}
}
