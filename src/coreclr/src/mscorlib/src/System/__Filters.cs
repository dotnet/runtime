// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// This class defines the delegate methods for the COM+ implemented filters.
//
// 
//

namespace System {
    using System;
    using System.Reflection;
    using System.Globalization;
    [Serializable]
    internal class __Filters {
        
        // Filters...
        // The following are the built in filters defined for this class.  These
        //  should really be defined as static methods.  They are used in as delegates
        //  which currently doesn't support static methods.  We will change this 
        //  once the compiler supports delegates.
        //
        // Note that it is not possible to make this class static as suggested by 
        // the above comment anymore because of it got marked serializable.

        internal static readonly __Filters Instance = new __Filters();
        
        // FilterAttribute
        //  This method will search for a member based upon the attribute passed in.
        //  filterCriteria -- an Int32 representing the attribute
        internal virtual bool FilterAttribute(MemberInfo m,Object filterCriteria)
        {
            // Check that the criteria object is an Integer object
            if (filterCriteria == null)
                throw new InvalidFilterCriteriaException(Environment.GetResourceString("RFLCT.FltCritInt"));
    
            switch (m.MemberType) 
            {
            case MemberTypes.Constructor:
            case MemberTypes.Method: {

                MethodAttributes criteria = 0;
                try {
                    int i = (int) filterCriteria;
                    criteria = (MethodAttributes) i;
                }
                catch {
                    throw new InvalidFilterCriteriaException(Environment.GetResourceString("RFLCT.FltCritInt"));
                }

                
                MethodAttributes attr;
                if (m.MemberType == MemberTypes.Method)
                    attr = ((MethodInfo) m).Attributes;
                else
                    attr = ((ConstructorInfo) m).Attributes;
                    
                if (((criteria & MethodAttributes.MemberAccessMask) != 0) && (attr & MethodAttributes.MemberAccessMask) != (criteria & MethodAttributes.MemberAccessMask))
                    return false;
                if (((criteria & MethodAttributes.Static) != 0) && (attr & MethodAttributes.Static) == 0)
                    return false;
                if (((criteria & MethodAttributes.Final) != 0) && (attr & MethodAttributes.Final) == 0)
                    return false;
                if (((criteria & MethodAttributes.Virtual) != 0) && (attr & MethodAttributes.Virtual) == 0)
                    return false;
                if (((criteria & MethodAttributes.Abstract) != 0) && (attr & MethodAttributes.Abstract) == 0)
                    return false;
                if (((criteria & MethodAttributes.SpecialName)  != 0) && (attr & MethodAttributes.SpecialName) == 0)
                    return false;
                return true;
            }
            case MemberTypes.Field: 
            {
                FieldAttributes criteria = 0;
                try {
                    int i = (int) filterCriteria;
                    criteria = (FieldAttributes) i;
                }
                catch {
                    throw new InvalidFilterCriteriaException(Environment.GetResourceString("RFLCT.FltCritInt"));
                }

                FieldAttributes attr = ((FieldInfo) m).Attributes;
                if (((criteria & FieldAttributes.FieldAccessMask) != 0) && (attr & FieldAttributes.FieldAccessMask) != (criteria & FieldAttributes.FieldAccessMask))
                    return false;
                if (((criteria & FieldAttributes.Static) != 0) && (attr & FieldAttributes.Static) == 0)
                    return false;
                if (((criteria & FieldAttributes.InitOnly) != 0) && (attr & FieldAttributes.InitOnly) == 0)
                    return false;
                if (((criteria & FieldAttributes.Literal) != 0) && (attr & FieldAttributes.Literal) == 0)
                    return false;
                if (((criteria & FieldAttributes.NotSerialized) != 0) && (attr & FieldAttributes.NotSerialized) == 0)
                    return false;
                if (((criteria & FieldAttributes.PinvokeImpl) != 0) && (attr & FieldAttributes.PinvokeImpl) == 0)
                    return false;
                return true;
            }
            }
    
            return false;
        }
        // FilterName
        // This method will filter based upon the name.  A partial wildcard
        //  at the end of the string is supported.
        //  filterCriteria -- This is the string name
        internal virtual bool FilterName(MemberInfo m,Object filterCriteria)
        {
            // Check that the criteria object is a String object
            if(filterCriteria == null || !(filterCriteria is String))
                throw new InvalidFilterCriteriaException(Environment.GetResourceString("RFLCT.FltCritString"));
    
            // At the moment this fails if its done on a single line....
            String str = ((String) filterCriteria);
            str = str.Trim();
    
            String name = m.Name;
            // Get the nested class name only, as opposed to the mangled one
            if (m.MemberType == MemberTypes.NestedType) 
                name = name.Substring(name.LastIndexOf('+') + 1);
            // Check to see if this is a prefix or exact match requirement
            if (str.Length > 0 && str[str.Length - 1] == '*') {
                str = str.Substring(0, str.Length - 1);
                return (name.StartsWith(str, StringComparison.Ordinal));
            }
    
            return (name.Equals(str));
        }
        
        // FilterIgnoreCase
        // This delegate will do a name search but does it with the
        //  ignore case specified.
        internal virtual bool FilterIgnoreCase(MemberInfo m,Object filterCriteria)
        {
            // Check that the criteria object is a String object
            if(filterCriteria == null || !(filterCriteria is String))
                throw new InvalidFilterCriteriaException(Environment.GetResourceString("RFLCT.FltCritString"));
    
            String str = (String) filterCriteria;
            str = str.Trim();
    
            String name = m.Name;
            // Get the nested class name only, as opposed to the mangled one
            if (m.MemberType == MemberTypes.NestedType) 
                name = name.Substring(name.LastIndexOf('+') + 1);
            // Check to see if this is a prefix or exact match requirement
            if (str.Length > 0 && str[str.Length - 1] == '*') {
                str = str.Substring(0, str.Length - 1);
                return (String.Compare(name,0,str,0,str.Length,StringComparison.OrdinalIgnoreCase)==0);
            }
    
            return (String.Compare(str,name, StringComparison.OrdinalIgnoreCase) == 0);
        }
    }
}
