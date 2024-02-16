// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;

public struct ValX0 {}
public struct ValY0 {}
public struct ValX1<T> {}
public struct ValY1<T> {}
public struct ValX2<T,U> {}
public struct ValY2<T,U>{}
public struct ValX3<T,U,V>{}
public struct ValY3<T,U,V>{}
public class RefX0 {}
public class RefY0 {}
public class RefX1<T> {}
public class RefY1<T> {}
public class RefX2<T,U> {}
public class RefY2<T,U>{}
public class RefX3<T,U,V>{}
public class RefY3<T,U,V>{}


public struct GenOuter<U>
{
	public class GenBase<T>
	{
		public T Fld1;
	
		public GenBase(T fld1)
		{
			Fld1 =  fld1;
		}

		public bool InstVerify(System.Type t1)
		{
			bool result = true;	

			if (!(Fld1.GetType().Equals(t1)))
			{	
				result = false;
				Console.WriteLine("Failed to verify type of Fld1 in: " + typeof(GenBase<T>) );
			}
			
			return result;
		}
	}
}

public class GenInt : GenOuter<int>.GenBase<int>
{	
	public GenInt() : base(1) {}

	public bool InstVerify()
	{
		return base.InstVerify(typeof(int));	
	}
}

public class GenDouble: GenOuter<int>.GenBase<double>
{	
	public GenDouble() : base(1) {}

	public bool InstVerify()
	{
		return base.InstVerify(typeof(double));	
	}
}

public class GenString : GenOuter<string>.GenBase<String>
{	
	public GenString() : base("string") {}

	public bool InstVerify()
	{
		return base.InstVerify(typeof(string));	
	}
}

public class GenObject : GenOuter<string>.GenBase<object>
{	
	public GenObject() : base(new object()) {}

	public bool InstVerify()
	{
		return base.InstVerify(typeof(object));	
	}
}

public class GenGuid : GenOuter<object>.GenBase<Guid>
{	
	public GenGuid() : base(new Guid()) {}

	public bool InstVerify()
	{
		return base.InstVerify(typeof(Guid));	
	}
}

public class GenConstructedReference : GenOuter<object>.GenBase<RefX1<int>>
{	
	public GenConstructedReference() : base(new RefX1<int>()) {}

	public bool InstVerify()
	{
		return base.InstVerify(typeof(RefX1<int>));	
	}
}

public class GenConstructedValue: GenOuter<double>.GenBase<ValX1<string>>
{	
	public GenConstructedValue() : base(new ValX1<string>()) {}

	public bool InstVerify()
	{
		return base.InstVerify(typeof(ValX1<string>));	
	}
}


public class GenInt1DArray : GenOuter<double>.GenBase<int[]>
{	
	public GenInt1DArray() : base(new int[1]) {}

	public bool InstVerify()
	{
		return base.InstVerify(typeof(int[]));	
	}
}

public class GenString2DArray : GenOuter<Guid>.GenBase<string[,]>
{	
	public GenString2DArray() : base(new string[1,1]) {}

	public bool InstVerify()
	{
		return base.InstVerify(typeof(string[,]));	
	}
}

public class GenIntJaggedArray : GenOuter<Guid>.GenBase<int[][]>
{	
	public GenIntJaggedArray() : base(new int[1][]) {}

	public bool InstVerify()
	{
		return base.InstVerify(typeof(int[][]));	
	}
}


public class Test_NestedBaseClass04
{
	public static int counter = 0;
	public static bool result = true;
	public static void Eval(bool exp)
	{
		counter++;
		if (!exp)
		{
			result = exp;
			Console.WriteLine("Test Failed at location: " + counter);
		}
	
	}
	
	[Fact]
	public static int TestEntryPoint()
	{
		Eval(new GenInt().InstVerify());
		Eval(new GenDouble().InstVerify());
		Eval(new GenString().InstVerify());
		Eval(new GenObject().InstVerify());
		Eval(new GenGuid().InstVerify());
		Eval(new GenConstructedReference().InstVerify());
		Eval(new GenConstructedValue().InstVerify());
		Eval(new GenInt1DArray().InstVerify());
		Eval(new GenString2DArray().InstVerify());
		Eval(new GenIntJaggedArray().InstVerify());
		
		if (result)
		{
			Console.WriteLine("Test Passed");
			return 100;
		}
		else
		{
			Console.WriteLine("Test Failed");
			return 1;
		}
	}
		
}
