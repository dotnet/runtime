// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Reflection;

public class TestData
{
    public virtual Guid InputGuid { get; set; }
}

class Test35000
{
    static int Main(string[] args)
    {
        bool success = false;

        PropertyInfo property = typeof(TestData).GetProperty(nameof(TestData.InputGuid),
            BindingFlags.Instance | BindingFlags.Public);

        Func<object, object> func = GetFunc(property);

        TestData data = new TestData();

        data.InputGuid = Guid.NewGuid();

        object value1 = func(data);
        object value3 = func(data);

        try
        {
            object value2 = func(null);
        }
        catch (NullReferenceException e)
        {
            Console.WriteLine(e);
            success = true;
        }

        return success ? 100 : 101;
    }

    public static Func<object, object> GetFunc(PropertyInfo propertyInfo)
    {
        var method = typeof(Test35000).GetMethod(nameof(CreateFunc));

        return (Func<object, object>)method
            .MakeGenericMethod(propertyInfo.DeclaringType, propertyInfo.PropertyType)
            .Invoke(null, new object[] { propertyInfo.GetMethod });
    }

    public static Func<object, object> CreateFunc<TTarget, TValue>(MethodInfo methodInfo)
    {

        var func = (Func<TTarget, TValue>)Delegate.CreateDelegate(typeof(Func<TTarget, TValue>), null, methodInfo);
        return (object o) => func((TTarget)o);
    }
}
