// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// Test that we can implement generic interfaces from mscorlib with recursion.
// such as MyType : IComparable<MyType>

/*
generic interfaces:
- ICollection
- IComparer
- IDictionary
- IEnumerable
- IEnumerator
- IEqualityComparer
- IList
*/


using System;
using System.Collections.Generic;
using Xunit;
 using System.Collections;

public struct MyClassICollection : ICollection<MyClassICollection> 
{
        public int Count { get {return 1;} }

        public bool IsReadOnly { get {return true;} }

        public void Add(MyClassICollection item){}

        public void Clear(){}

        public bool Contains(MyClassICollection item) { return true;}
		
        public void CopyTo(MyClassICollection[] array, int arrayIndex){}
		
        public bool Remove(MyClassICollection item){ return true;}

	 System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
	{
		return default(System.Collections.IEnumerator);	
	}

	 IEnumerator<MyClassICollection> System.Collections.Generic.IEnumerable<MyClassICollection>.GetEnumerator()
	{
		return default(IEnumerator<MyClassICollection>);
	}
	
}

public struct MyClassIComparer : IComparer<MyClassIComparer> 
{
	public int Compare(MyClassIComparer x, MyClassIComparer y){return 1;}
}

public struct MyClassIDictionary: IDictionary<MyClassIDictionary,MyClassIDictionary> 
{


	bool System.Collections.Generic.IDictionary<MyClassIDictionary, MyClassIDictionary>.ContainsKey(MyClassIDictionary key)
	{
		return false;
	}


	void System.Collections.Generic.IDictionary<MyClassIDictionary, MyClassIDictionary>.Add(MyClassIDictionary key, MyClassIDictionary value)
	{}


	bool System.Collections.Generic.IDictionary<MyClassIDictionary, MyClassIDictionary>.Remove(MyClassIDictionary key)
	{
		return false;
	}

	bool System.Collections.Generic.IDictionary<MyClassIDictionary,MyClassIDictionary>.TryGetValue(MyClassIDictionary key, out MyClassIDictionary value)
	{
		value = new MyClassIDictionary();
		return false;
	}



	ICollection<MyClassIDictionary> System.Collections.Generic.IDictionary<MyClassIDictionary, MyClassIDictionary>.Keys
	{
		get
		{
			return default(ICollection<MyClassIDictionary>);
		}
	}


	ICollection<MyClassIDictionary> System.Collections.Generic.IDictionary<MyClassIDictionary, MyClassIDictionary>.Values
	{
		get
		{
			return default(ICollection<MyClassIDictionary>);
		}
	}

	MyClassIDictionary System.Collections.Generic.IDictionary<MyClassIDictionary, MyClassIDictionary>.this[MyClassIDictionary key]
	{
		get
		{
			return new MyClassIDictionary();
		}
		set{}
	}


	#region ICollection<KeyValuePair<MyDictionary,Int32>> Members

	void System.Collections.Generic.ICollection<KeyValuePair<MyClassIDictionary, MyClassIDictionary>>.Add(KeyValuePair<MyClassIDictionary, MyClassIDictionary> item)
	{
	}

	void System.Collections.Generic.ICollection<KeyValuePair<MyClassIDictionary, MyClassIDictionary>>.Clear()
	{}

	
	int System.Collections.Generic.ICollection<KeyValuePair<MyClassIDictionary, MyClassIDictionary>>.Count
	{
		get
		{
			return 1;
		}
	}

	bool System.Collections.Generic.ICollection<KeyValuePair<MyClassIDictionary, MyClassIDictionary>>.IsReadOnly
	{
		get
		{
			return false;
		}
	}

	void System.Collections.Generic.ICollection<KeyValuePair<MyClassIDictionary, MyClassIDictionary>>.CopyTo(KeyValuePair<MyClassIDictionary,MyClassIDictionary>[] array, int arrayIndex)
	{}


	bool System.Collections.Generic.ICollection<KeyValuePair<MyClassIDictionary, MyClassIDictionary>>.Contains(KeyValuePair<MyClassIDictionary, MyClassIDictionary> item)
	{
		return false;
	}

	bool System.Collections.Generic.ICollection<KeyValuePair<MyClassIDictionary, MyClassIDictionary>>.Remove(KeyValuePair<MyClassIDictionary, MyClassIDictionary> item)
	{
		return false;
	}

	IEnumerator<KeyValuePair<MyClassIDictionary, MyClassIDictionary>> System.Collections.Generic.IEnumerable<KeyValuePair<MyClassIDictionary, MyClassIDictionary>>.GetEnumerator()
	{
		return default(IEnumerator<KeyValuePair<MyClassIDictionary, MyClassIDictionary>>);
	}

	#endregion

	 System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
	{
		return default(System.Collections.IEnumerator);	
		
	}

}


public struct MyClassIEnumerable : IEnumerable<MyClassIEnumerable>
{

	 System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
	{
		return default(System.Collections.IEnumerator);	
		
	}

	 IEnumerator<MyClassIEnumerable> System.Collections.Generic.IEnumerable<MyClassIEnumerable>.GetEnumerator()
	{
		return default(IEnumerator<MyClassIEnumerable>);
	}

}

public struct MyClassIEnumerator : IEnumerator <MyClassIEnumerator>
{

	public MyClassIEnumerator Current 
	{
            get {return new MyClassIEnumerator();} 
	}

	public bool MoveNext() { return true;}

	 public void Reset(){}

	 Object System.Collections.IEnumerator.Current 
	 {
            get {return new Object();}
        }

	 public void Dispose(){}
			

}


public struct MyClassIEqualityComparer: IEqualityComparer<MyClassIEqualityComparer>
{

	
	public bool Equals(MyClassIEqualityComparer x, MyClassIEqualityComparer y) { return true;}
	
       public int GetHashCode(MyClassIEqualityComparer obj) {return 1;}  
}



public struct MyClassIList:  IList<MyClassIList>
{

	 public MyClassIList this[int index] 
	 {
            get{return new MyClassIList();}
            set{}
        }
    
        public int IndexOf(MyClassIList item) {return 1;}
    

        public void Insert(int index, MyClassIList item) {}
        
        
        public void RemoveAt(int index) {}


	public int Count { get {return 1;} }

        public bool IsReadOnly { get {return true;} }

        public void Add(MyClassIList item){}

        public void Clear(){}

        public bool Contains(MyClassIList item) { return true;}
		
        public void CopyTo(MyClassIList[] array, int arrayIndex){}
		
        public bool Remove(MyClassIList item){ return true;}


	 System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
	{
		return default(System.Collections.IEnumerator);	
	}

	 IEnumerator<MyClassIList> System.Collections.Generic.IEnumerable<MyClassIList>.GetEnumerator()
	{
		return default(IEnumerator<MyClassIList>);
	}

}



public struct MyClassIList2:  IList<MyClassIList>,  IList<int>
{

	 MyClassIList System.Collections.Generic.IList<MyClassIList>.this[int index] 
	 {
            get{return new MyClassIList();}
            set{}
        }

	  int System.Collections.Generic.IList<int>.this[int index] 
	 {
            get{return 1;}
            set{}
        }
    
        public int IndexOf(MyClassIList item) {return 1;}
    

        public void Insert(int index, MyClassIList item) {}
        
        
        public void RemoveAt(int index) {}


	public int Count { get {return 1;} }

        public bool IsReadOnly { get {return true;} }

        public void Add(MyClassIList item){}

        public void Clear(){}

        public bool Contains(MyClassIList item) { return true;}
		
        public void CopyTo(MyClassIList[] array, int arrayIndex){}
		
        public bool Remove(MyClassIList item){ return true;}


	 System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
	{
		return default(System.Collections.IEnumerator);	
	}

	 IEnumerator<MyClassIList> System.Collections.Generic.IEnumerable<MyClassIList>.GetEnumerator()
	{
		return default(IEnumerator<MyClassIList>);
	}


	// int
	  public int IndexOf(int item) {return 1;}
    

        public void Insert(int index, int item) {}
        

        public void Add(int item){}


        public bool Contains(int item) { return true;}
		
        public void CopyTo(int[] array, int arrayIndex){}
		
        public bool Remove(int item){ return true;}

	 IEnumerator<int> System.Collections.Generic.IEnumerable<int>.GetEnumerator()
	{
		return default(IEnumerator<int>);
	}


}


public class Test_Struct_ImplementMscorlibGenInterface
{
	[Fact]
	public static int TestEntryPoint()
	{
		#pragma warning disable 219
		
		try
		{
			Console.WriteLine("Test 1: Instantiate Struct : ICollection<Struct>");
			MyClassICollection obj1 = new MyClassICollection();
			
			Console.WriteLine("Test 2: Instantiate Struct : IComparer<Struct>");
			MyClassIComparer obj2 = new MyClassIComparer();

			Console.WriteLine("Test 3: Instantiate Struct : IDictionary<Struct>");
			MyClassIDictionary obj3 = new MyClassIDictionary();

			Console.WriteLine("Test 4: Instantiate Struct : IEnumerable<Struct>");
			MyClassIEnumerable obj4 = new MyClassIEnumerable();

			Console.WriteLine("Test 5: Instantiate Struct : IEnumerator<Struct>");
			MyClassIEnumerator obj5 = new MyClassIEnumerator();

			Console.WriteLine("Test 6: Instantiate Struct : IEqualityComparer<Struct>");
			MyClassIEqualityComparer obj6 = new MyClassIEqualityComparer();

			Console.WriteLine("Test 7: Instantiate Struct : IList<Struct>");
			MyClassIList obj7 = new MyClassIList();

			Console.WriteLine("Test 8: Instantiate Struct : IList<Struct>, IList<int>");
			MyClassIList2 obj8 = new MyClassIList2();

			Console.WriteLine("PASS");
			return 100;
		}
		catch (Exception e)
		{
			Console.WriteLine("FAIL: Caught unexpected exception: " + e);
			return 101;
		}

		#pragma warning restore 219

	}
}

