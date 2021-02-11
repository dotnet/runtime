using System;

public class Control {}
public class UserControl : Control {}

namespace test
{
    public class MainPage : UserControl
    {
        public static void Main ()
        {
            var more = new MoreConstrained<MainPage>();
            more.test(new MainPage ());
        }
    }

    public class MoreConstrained<T> where T : Control
    {
        public void test(T param)
        {
            Console.WriteLine("More " + typeof(T) + " " + param);
            var x = new LessConstrained<T>();
            x.test<T>();
        }
    }

    public class LessConstrained<T> where T : class
    {
        public void test<T2>()
        {
            Console.WriteLine("Less " + typeof(T2));
        }
    }

}
