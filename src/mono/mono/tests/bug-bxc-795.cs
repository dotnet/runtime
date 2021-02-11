public abstract class BaseClass<T> {}
public class ClassA : BaseClass<ClassB> { }
public class ClassB : ClassA {}

public class TestClass
{
	static int Main () {
        object x = new ClassB();
		if (!(x is ClassA))
			return 1;
		if (!(x is BaseClass <ClassB>))
			return 2;
		return 0;
	}
}
