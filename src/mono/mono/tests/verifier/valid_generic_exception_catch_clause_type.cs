public class MyException<T> : System.Exception
{
}

public class A<T>
{
    public void F()
    {
        try
        {
        }
        catch(MyException<T> ex)
        {
            System.Console.WriteLine(ex.ToString());
        }
    }
}

public class B
{
    public static void Main(string[] args)
    {
        A<string> a = new A<string>();
        a.F();
    }
}
