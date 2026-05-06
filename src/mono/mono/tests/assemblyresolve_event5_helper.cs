using System;

public class SimpleTypedAttribute : Attribute {
	public SimpleTypedAttribute (Type t) { }
}

[SimpleTypedAttribute(typeof(Foo))] /* Foo defined in the assemblyresolve_event5_label assembly */
public class MyClass {
	public MyClass () { }
}
