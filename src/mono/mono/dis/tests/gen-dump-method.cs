//Test for dumping method table for generic types and generic methods
//monodis --method 
class a 
{
	void a_foo <U> (U u)
	{
	}
}

class g <T>
{
	T foo2 (int i, T t)
	{
		return default (T);
	}
	
	T foo <U> (U u, int i)
	{
		return default (T);
	}
}
