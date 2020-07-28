// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        TypeDescriptionProviderAttribute attr = new TypeDescriptionProviderAttribute("Program+MyTypeDescriptionProvider");
        if (!RunTest(attr))
        {
            return -1;
        }

        attr = new TypeDescriptionProviderAttribute(typeof(MyOtherTypeDescriptionProvider));
        if (!RunTest(attr))
        {
            return -1;
        }

        return 100;
    }

    private static bool RunTest(TypeDescriptionProviderAttribute attr)
    {
        Type providerType = Type.GetType(attr.TypeName);

        if (providerType != null && typeof(TypeDescriptionProvider).IsAssignableFrom(providerType))
        {
            TypeDescriptionProvider provider = (TypeDescriptionProvider)Activator.CreateInstance(providerType);
            if (provider == null)
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        return true;
    }

    private class MyTypeDescriptionProvider : TypeDescriptionProvider { }

    private class MyOtherTypeDescriptionProvider : TypeDescriptionProvider { }
}
