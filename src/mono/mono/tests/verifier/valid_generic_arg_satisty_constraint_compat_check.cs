using System;


public class SerializerItemProxy<TBaseClass, TActualClass> : SerializerProxy<TActualClass>
	where TBaseClass : class
	where TActualClass : class, TBaseClass
{

}

public class SerializerProxy<T>
{
    static SerializerProxy<TItem> MakeItem<TItem>() where TItem : class, T
    {
            return new SerializerItemProxy <TItem, TItem>();
    }
}


class Driver
{
    static void Main(string[] args)
    {
    }
}

