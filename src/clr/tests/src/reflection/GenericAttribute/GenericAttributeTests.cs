using System;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

class Program
{
    static int Main(string[] args)
    {
        Assembly assembly = typeof(Class).GetTypeInfo().Assembly;
        Assert(CustomAttributeExtensions.GetCustomAttribute<SingleAttribute<int>>(assembly) != null);
        Assert(((ICustomAttributeProvider)assembly).GetCustomAttributes(typeof(SingleAttribute<int>), true) != null);
        Assert(CustomAttributeExtensions.GetCustomAttribute<SingleAttribute<bool>>(assembly) != null);
        Assert(((ICustomAttributeProvider)assembly).GetCustomAttributes(typeof(SingleAttribute<bool>), true) != null);
        Assert(CustomAttributeExtensions.IsDefined(assembly, typeof(SingleAttribute<int>)));
        Assert(((ICustomAttributeProvider)assembly).IsDefined(typeof(SingleAttribute<int>), true));
        Assert(CustomAttributeExtensions.IsDefined(assembly, typeof(SingleAttribute<bool>)));
        Assert(((ICustomAttributeProvider)assembly).IsDefined(typeof(SingleAttribute<bool>), true));

        TypeInfo programTypeInfo = typeof(Class).GetTypeInfo();
        Assert(CustomAttributeExtensions.GetCustomAttribute<SingleAttribute<int>>(programTypeInfo) != null);
        Assert(((ICustomAttributeProvider)programTypeInfo).GetCustomAttributes(typeof(SingleAttribute<int>), true) != null);
        Assert(CustomAttributeExtensions.GetCustomAttribute<SingleAttribute<bool>>(programTypeInfo) != null);
        Assert(((ICustomAttributeProvider)programTypeInfo).GetCustomAttributes(typeof(SingleAttribute<bool>), true) != null);
        Assert(CustomAttributeExtensions.IsDefined(programTypeInfo, typeof(SingleAttribute<int>)));    
        Assert(((ICustomAttributeProvider)programTypeInfo).IsDefined(typeof(SingleAttribute<int>), true));        
        Assert(CustomAttributeExtensions.IsDefined(programTypeInfo, typeof(SingleAttribute<bool>)));
        Assert(((ICustomAttributeProvider)programTypeInfo).IsDefined(typeof(SingleAttribute<bool>), true));    

        var propertyPropertyInfo = typeof(Class).GetTypeInfo().GetProperty(nameof(Class.Property));
        Assert(CustomAttributeExtensions.GetCustomAttribute<SingleAttribute<int>>(propertyPropertyInfo) != null);
        Assert(((ICustomAttributeProvider)propertyPropertyInfo).GetCustomAttributes(typeof(SingleAttribute<int>), true) != null);
        Assert(CustomAttributeExtensions.GetCustomAttribute<SingleAttribute<bool>>(propertyPropertyInfo) != null);
        Assert(((ICustomAttributeProvider)propertyPropertyInfo).GetCustomAttributes(typeof(SingleAttribute<bool>), true) != null);
        Assert(CustomAttributeExtensions.IsDefined(propertyPropertyInfo, typeof(SingleAttribute<int>)));    
        Assert(((ICustomAttributeProvider)propertyPropertyInfo).IsDefined(typeof(SingleAttribute<int>), true));              
        Assert(CustomAttributeExtensions.IsDefined(propertyPropertyInfo, typeof(SingleAttribute<bool>)));
        Assert(((ICustomAttributeProvider)propertyPropertyInfo).IsDefined(typeof(SingleAttribute<bool>), true));              

        var deriveTypeInfo = typeof(Class.Derive).GetTypeInfo();
        Assert(CustomAttributeExtensions.GetCustomAttribute<SingleAttribute<int>>(deriveTypeInfo, false) == null);
        Assert(((ICustomAttributeProvider)deriveTypeInfo).GetCustomAttributes(typeof(SingleAttribute<int>), true) != null);
        Assert(CustomAttributeExtensions.GetCustomAttribute<SingleAttribute<bool>>(deriveTypeInfo, false) == null);
        Assert(((ICustomAttributeProvider)deriveTypeInfo).GetCustomAttributes(typeof(SingleAttribute<bool>), true) != null);
        Assert(!CustomAttributeExtensions.IsDefined(deriveTypeInfo, typeof(SingleAttribute<int>), false));            
        Assert(!CustomAttributeExtensions.IsDefined(deriveTypeInfo, typeof(SingleAttribute<bool>), false));

        Assert(CustomAttributeExtensions.GetCustomAttribute<SingleAttribute<int>>(deriveTypeInfo, true) != null);
        Assert(CustomAttributeExtensions.GetCustomAttribute<SingleAttribute<bool>>(deriveTypeInfo, true) != null);
        Assert(CustomAttributeExtensions.IsDefined(deriveTypeInfo, typeof(SingleAttribute<int>), true));   
        Assert(((ICustomAttributeProvider)deriveTypeInfo).IsDefined(typeof(SingleAttribute<int>), true));           
        Assert(CustomAttributeExtensions.IsDefined(deriveTypeInfo, typeof(SingleAttribute<bool>), true));
        Assert(((ICustomAttributeProvider)deriveTypeInfo).IsDefined(typeof(SingleAttribute<bool>), true));           

        var a1 = CustomAttributeExtensions.GetCustomAttributes(programTypeInfo, true);
        AssertAny(a1, a => a is SingleAttribute<int>);
        AssertAny(a1, a => a is SingleAttribute<bool>);
        AssertAny(a1, a => (a as MultiAttribute<int>)?.Value == 0);
        AssertAny(a1, a => (a as MultiAttribute<int>)?.Value == 1);
        AssertAny(a1, a => (a as MultiAttribute<int>)?.Value == 2);
        AssertAny(a1, a => (a as MultiAttribute<bool>)?.Value == false);
        AssertAny(a1, a => (a as MultiAttribute<bool>)?.Value == true, 2);
        AssertAny(a1, a => (a as MultiAttribute<bool?>)?.Value == null);

        var b1 = ((ICustomAttributeProvider)programTypeInfo).GetCustomAttributes(true);
        AssertAny(b1, a => a is SingleAttribute<int>);
        AssertAny(b1, a => a is SingleAttribute<bool>);
        AssertAny(b1, a => (a as MultiAttribute<int>)?.Value == 0);
        AssertAny(b1, a => (a as MultiAttribute<int>)?.Value == 1);
        AssertAny(b1, a => (a as MultiAttribute<int>)?.Value == 2);
        AssertAny(b1, a => (a as MultiAttribute<bool>)?.Value == false);
        AssertAny(b1, a => (a as MultiAttribute<bool>)?.Value == true, 2);
        AssertAny(b1, a => (a as MultiAttribute<bool?>)?.Value == null);
        
        var a2 = CustomAttributeExtensions.GetCustomAttributes(deriveTypeInfo, false);
        Assert(!a2.GetEnumerator().MoveNext());

        var b2 = ((ICustomAttributeProvider)deriveTypeInfo).GetCustomAttributes(false);
        Assert(!b2.GetEnumerator().MoveNext());

        var a3 = CustomAttributeExtensions.GetCustomAttributes(deriveTypeInfo, true);
        AssertAny(a3, a => a is SingleAttribute<int>);
        AssertAny(a3, a => a is SingleAttribute<bool>);
        AssertAny(a3, a => (a as MultiAttribute<int>)?.Value == 0);
        AssertAny(a3, a => (a as MultiAttribute<int>)?.Value == 1);
        AssertAny(a3, a => (a as MultiAttribute<int>)?.Value == 2);
        AssertAny(a3, a => (a as MultiAttribute<bool>)?.Value == false);
        AssertAny(a3, a => (a as MultiAttribute<bool>)?.Value == true);

        var b3 = ((ICustomAttributeProvider)deriveTypeInfo).GetCustomAttributes(true);
        AssertAny(b3, a => a is SingleAttribute<int>);
        AssertAny(b3, a => a is SingleAttribute<bool>);
        AssertAny(b3, a => (a as MultiAttribute<int>)?.Value == 0);
        AssertAny(b3, a => (a as MultiAttribute<int>)?.Value == 1);
        AssertAny(b3, a => (a as MultiAttribute<int>)?.Value == 2);
        AssertAny(b3, a => (a as MultiAttribute<bool>)?.Value == false);
        AssertAny(b3, a => (a as MultiAttribute<bool>)?.Value == true);

        var a4 = CustomAttributeExtensions.GetCustomAttributes<SingleAttribute<int>>(programTypeInfo, true);
        AssertAny(a4, a => a is SingleAttribute<int>);

        var b4 = ((ICustomAttributeProvider)programTypeInfo).GetCustomAttributes(typeof(SingleAttribute<int>), true);
        AssertAny(b4, a => a is SingleAttribute<int>);

        var a5 = CustomAttributeExtensions.GetCustomAttributes<SingleAttribute<bool>>(programTypeInfo);
        AssertAny(a5, a => a is SingleAttribute<bool>);

        var b5 = ((ICustomAttributeProvider)programTypeInfo).GetCustomAttributes(typeof(SingleAttribute<bool>), true);
        AssertAny(b5, a => a is SingleAttribute<bool>);

        var a6 = CustomAttributeExtensions.GetCustomAttributes<MultiAttribute<int>>(programTypeInfo, true);
        AssertAny(a6, a => (a as MultiAttribute<int>)?.Value == 0);
        AssertAny(a6, a => (a as MultiAttribute<int>)?.Value == 1);
        AssertAny(a6, a => (a as MultiAttribute<int>)?.Value == 2);

        var b6 = ((ICustomAttributeProvider)programTypeInfo).GetCustomAttributes(typeof(MultiAttribute<int>), true);
        AssertAny(b6, a => (a as MultiAttribute<int>)?.Value == 0);
        AssertAny(b6, a => (a as MultiAttribute<int>)?.Value == 1);
        AssertAny(b6, a => (a as MultiAttribute<int>)?.Value == 2);

        var a7 = CustomAttributeExtensions.GetCustomAttributes<MultiAttribute<bool>>(programTypeInfo, true);
        AssertAny(a7, a => (a as MultiAttribute<bool>)?.Value == false);
        AssertAny(a7, a => (a as MultiAttribute<bool>)?.Value == true);

        var b7 = ((ICustomAttributeProvider)programTypeInfo).GetCustomAttributes(typeof(MultiAttribute<bool>), true);
        AssertAny(b7, a => (a as MultiAttribute<bool>)?.Value == false);
        AssertAny(b7, a => (a as MultiAttribute<bool>)?.Value == true);

        var a8 = CustomAttributeExtensions.GetCustomAttributes<MultiAttribute<bool?>>(programTypeInfo, true);
        AssertAny(a8, a => (a as MultiAttribute<bool?>)?.Value == null);

        var b8 = ((ICustomAttributeProvider)programTypeInfo).GetCustomAttributes(typeof(MultiAttribute<bool?>), true);
        AssertAny(b8, a => (a as MultiAttribute<bool?>)?.Value == null);

        var a9 = CustomAttributeExtensions.GetCustomAttributes<MultiAttribute<string>>(programTypeInfo, true);
        AssertAny(a9, a => (a as MultiAttribute<string>)?.Value == "Ctor");
        AssertAny(a9, a => (a as MultiAttribute<string>)?.Value == "Property");

        var b9 = ((ICustomAttributeProvider)programTypeInfo).GetCustomAttributes(typeof(MultiAttribute<string>), true);
        AssertAny(b9, a => (a as MultiAttribute<string>)?.Value == "Ctor");
        AssertAny(b9, a => (a as MultiAttribute<string>)?.Value == "Property");

        var a10 = CustomAttributeExtensions.GetCustomAttributes<MultiAttribute<Type>>(programTypeInfo, true);
        AssertAny(a10, a => (a as MultiAttribute<Type>)?.Value == typeof(Class));
        AssertAny(a10, a => (a as MultiAttribute<Type>)?.Value == typeof(Class.Derive));

        var b10 = ((ICustomAttributeProvider)programTypeInfo).GetCustomAttributes(typeof(MultiAttribute<Type>), true);
        AssertAny(b10, a => (a as MultiAttribute<Type>)?.Value == typeof(Class));
        AssertAny(b10, a => (a as MultiAttribute<Type>)?.Value == typeof(Class.Derive));

        Assert(CustomAttributeExtensions.GetCustomAttributes(programTypeInfo, typeof(MultiAttribute<>), false) == null);
        Assert(CustomAttributeExtensions.GetCustomAttributes(programTypeInfo, typeof(MultiAttribute<>), true) == null);
        Assert(!((ICustomAttributeProvider)programTypeInfo).GetCustomAttributes(typeof(MultiAttribute<>), true).GetEnumerator().MoveNext());

        return 100;
    }

    static void Assert(bool condition, [CallerLineNumberAttribute]int line = 0)
    {
        if(!condition)
        {
            throw new Exception($"Error in line: {line}");
        }
    }

    static void AssertAny(IEnumerable<object> source, Func<Attribute, bool> condition, int count = 1, [CallerLineNumberAttribute]int line = 0)
    {
        var enumerator = source.GetEnumerator();
        while (enumerator.MoveNext())
        {
            if(condition(enumerator.Current as Attribute) && --count == 0)
            {
                return;
            }
        }
        throw new Exception($"Error in line: {line}");
    }
}
