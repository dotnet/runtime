// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Xunit;

public static class Program
{
    class MemberInfo { }

    class PropertyInfo : MemberInfo {  }

    class CustomAttributeData
    {
        public Attribute Instantiate() => new CLSCompliantAttribute(false);
    }

    private static IEnumerable<CustomAttributeData> GetMatchingCustomAttributes(this MemberInfo element, Type optionalAttributeTypeFilter, bool inherit, bool skipTypeValidation = false)
    {
        {
            PropertyInfo propertyInfo = element as PropertyInfo;
            if (propertyInfo != null)
                yield return new CustomAttributeData();
        }

        if (element == null)
            throw new ArgumentNullException();

        throw new NotSupportedException(); // Shouldn't get here.
    }

    private static IEnumerable<TOut> Select<TIn, TOut>(this IEnumerable<TIn> source, Func<TIn, TOut> transform)
    {
        foreach (var s in source)
            yield return transform(s);
    }

    private static IEnumerable<T> GetCustomAttributes<T>(this MemberInfo element, bool inherit) where T : Attribute
    {
        IEnumerable<CustomAttributeData> matches = element.GetMatchingCustomAttributes(typeof(T), inherit, skipTypeValidation: true);
        return matches.Select(m => (T)(m.Instantiate()));
    }

    private static AttributeType GetCustomAttribute<AttributeType>(PropertyInfo propInfo)
        where AttributeType : Attribute
    {
        AttributeType result = null;
        foreach (var attrib in propInfo.GetCustomAttributes<AttributeType>(false))
        {
            result = attrib;
            break;
        }
        return result;
    }

    [Fact]
    public static int TestEntryPoint()
    {
        return GetCustomAttribute<Attribute>(new PropertyInfo()) != null ? 100 : -1;
    }
}
