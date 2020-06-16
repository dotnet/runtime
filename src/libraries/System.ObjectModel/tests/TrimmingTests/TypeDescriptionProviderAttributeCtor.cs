using System;
using System.ComponentModel;
using System.Xml.Linq;

class Program
{
    static int Main(string[] args)
    {
        // The public parameterless ctor of the Type argument here is preserved.
        TypeDescriptionProviderAttribute attr = new TypeDescriptionProviderAttribute(typeof(MyTypeDescriptionProvider));
        Activator.CreateInstance(Type.GetType(attr.TypeName));
        return 100;
    }

    private class MyTypeDescriptionProvider : TypeDescriptionProvider { }
}
