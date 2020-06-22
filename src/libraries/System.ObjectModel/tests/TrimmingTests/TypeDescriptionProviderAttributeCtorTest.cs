using System;
using System.ComponentModel;
using System.Xml.Linq;

class Program
{
    /// <summary>
    /// Tests that the public parameterless constructors of types passed to the
    /// constructors of System.ComponentModel.TypeDescriptionProviderAttribute
    /// property are preserved when needed in a trimmed application.
    /// </summary>
    static int Main(string[] args)
    {
        // Test that public parameterless ctor of the Type argument here is preserved.
        Type providerType = typeof(MyTypeDescriptionProvider);
        TypeDescriptionProviderAttribute attr = new TypeDescriptionProviderAttribute(providerType);
        object obj = Activator.CreateInstance(providerType);

        if (obj == null || !(obj is MyTypeDescriptionProvider))
        {
            return -1;
        }

        // Test that public parameterless ctor of the Type argument here is preserved.
        providerType = typeof(MyOtherTypeDescriptionProvider);
        attr = new TypeDescriptionProviderAttribute(providerType.ToString());
        obj = Activator.CreateInstance(providerType);

        if (obj == null || !(obj is MyOtherTypeDescriptionProvider))
        {
            return -1;
        }

        return 100;
    }

    private class MyTypeDescriptionProvider : TypeDescriptionProvider { }

    private class MyOtherTypeDescriptionProvider : TypeDescriptionProvider { }
}
