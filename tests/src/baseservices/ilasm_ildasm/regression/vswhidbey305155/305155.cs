using System;

[AttributeUsage(AttributeTargets.Method)]
public class MyAttribute : Attribute
{
	public Type[] Types;
}

public class Test
{
	[MyAttribute(Types = new Type[]{typeof(string), typeof(void)})]
	public static int Main(String[] args) { return 0; }
}
