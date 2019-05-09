// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.Reflection
{
    public interface IReflect
    {
        // Return the requested method if it is implemented by the Reflection object.  The
        // match is based upon the name and DescriptorInfo which describes the signature
        // of the method. 
        MethodInfo? GetMethod(string name, BindingFlags bindingAttr, Binder? binder, Type[] types, ParameterModifier[]? modifiers);

        // Return the requested method if it is implemented by the Reflection object.  The
        // match is based upon the name of the method.  If the object implementes multiple methods
        // with the same name an AmbiguousMatchException is thrown.
        MethodInfo? GetMethod(string name, BindingFlags bindingAttr);

        MethodInfo[] GetMethods(BindingFlags bindingAttr);

        // Return the requestion field if it is implemented by the Reflection object.  The
        // match is based upon a name.  There cannot be more than a single field with
        // a name.
        FieldInfo? GetField(string name, BindingFlags bindingAttr);

        FieldInfo[] GetFields(BindingFlags bindingAttr);

        // Return the property based upon name.  If more than one property has the given
        // name an AmbiguousMatchException will be thrown.  Returns null if no property
        // is found.
        PropertyInfo? GetProperty(string name, BindingFlags bindingAttr);

        // Return the property based upon the name and Descriptor info describing the property
        // indexing.  Return null if no property is found.
        PropertyInfo? GetProperty(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[] types, ParameterModifier[]? modifiers);

        // Returns an array of PropertyInfos for all the properties defined on 
        // the Reflection object.
        PropertyInfo[] GetProperties(BindingFlags bindingAttr);

        // Return an array of members which match the passed in name.
        MemberInfo[] GetMember(string name, BindingFlags bindingAttr);

        // Return an array of all of the members defined for this object.
        MemberInfo[] GetMembers(BindingFlags bindingAttr);

        // Description of the Binding Process.
        // We must invoke a method that is accessible and for which the provided
        // parameters have the most specific match.  A method may be called if
        // 1. The number of parameters in the method declaration equals the number of 
        //    arguments provided to the invocation
        // 2. The type of each argument can be converted by the binder to the
        //    type of the type of the parameter.
        // 
        // The binder will find all of the matching methods.  These method are found based
        // upon the type of binding requested (MethodInvoke, Get/Set Properties).  The set
        // of methods is filtered by the name, number of arguments and a set of search modifiers
        // defined in the Binder.
        // 
        // After the method is selected, it will be invoked.  Accessibility is checked
        // at that point.  The search may be control which set of methods are searched based
        // upon the accessibility attribute associated with the method.
        // 
        // The BindToMethod method is responsible for selecting the method to be invoked.
        // For the default binder, the most specific method will be selected.
        // 
        // This will invoke a specific member...
        object? InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters);

        // Return the underlying Type that represents the IReflect Object.  For expando object,
        // this is the (Object) IReflectInstance.GetType().  For Type object it is this.
        Type UnderlyingSystemType { get; }
    }
}
