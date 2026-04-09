
namespace Baz
{
    class Foo
    {
        public Foo()
        {
        }

        public string Bar()
        {
            return "Hello, World!";
		}

        public string Bar(string who)
        {
            return "Hello, " + who + "!";
        }
	}

    class Goo
    {
        public Goo()
        {
        }

        public string Bar(string greet)
        {
            return greet + ", World!";
        }
    }

    class Foo2
    {
        public Foo2()
        {
        }

        public string Bar(string greet)
        {
            return greet + ", World!!!";
        }
    }

    class MainClass
    {
        public static void Main(string[] args)
        {
            var foo = new Foo();
            System.Console.WriteLine(foo.Bar());
            System.Console.WriteLine(foo.Bar("World"));
            var goo = new Goo();
            System.Console.WriteLine(goo.Bar("Hello"));
            var foo2 = new Foo2();
            System.Console.WriteLine(foo2.Bar("Hello"));
        }
    }
}
