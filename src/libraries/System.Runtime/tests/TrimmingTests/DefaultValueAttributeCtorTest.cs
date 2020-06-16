using System;
using System.ComponentModel;
using System.Reflection;

class Program
{
    static int Main(string[] args)
    {
        var attribute = new DefaultValueAttribute(typeof(string), "Hello, world!");

        // There's a fallback in the DefaultValueAttribute ctor for when the following
        // value is null. It shouldn't be null at this point, if the linker didn't trim
        // System.ComponentModel.TypeConverter.ConvertFromInvariantString out.
        object convertFromInvariantString = typeof(DefaultValueAttribute).GetField(
            "s_convertFromInvariantString",
            BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Static).GetValue(attribute);

        return (string)attribute.Value == "Hello, world!" && convertFromInvariantString != null
            ? 100
            : -1;
    }
}
