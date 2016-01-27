// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////
//
// 
// This class defines the delegate methods for the COM+ implemented filters.
//    This is the reflection version of these.  There is also a _Filters class in
//    runtime which is related to this.
//
//  
//  
//
namespace System.Reflection {
    using System;
    using System.Globalization;

    [Serializable]
    internal class __Filters {
        
        // FilterTypeName 
        // This method will filter the class based upon the name.  It supports
        //    a trailing wild card.
        public virtual bool FilterTypeName(Type cls,Object filterCriteria)
        {
            // Check that the criteria object is a String object
            if (filterCriteria == null || !(filterCriteria is String))
                throw new InvalidFilterCriteriaException(System.Environment.GetResourceString("RFLCT.FltCritString"));
    
            String str = (String) filterCriteria;
            //str = str.Trim();
    
            // Check to see if this is a prefix or exact match requirement
            if (str.Length > 0 && str[str.Length - 1] == '*') {
                str = str.Substring(0, str.Length - 1);
                return cls.Name.StartsWith(str, StringComparison.Ordinal);
            }
    
            return cls.Name.Equals(str);
        }
        
        // FilterFieldNameIgnoreCase
        // This method filter the Type based upon name, it ignores case.
        public virtual bool FilterTypeNameIgnoreCase(Type cls, Object filterCriteria)
        {
            // Check that the criteria object is a String object
            if(filterCriteria == null || !(filterCriteria is String))
                throw new InvalidFilterCriteriaException(System.Environment.GetResourceString("RFLCT.FltCritString"));
    
            String str = (String) filterCriteria;
            //str = str.Trim();
    
            // Check to see if this is a prefix or exact match requirement
            if (str.Length > 0 && str[str.Length - 1] == '*') {
                str = str.Substring(0, str.Length - 1);
                String name = cls.Name;
                if (name.Length >= str.Length)
                    return (String.Compare(name,0,str,0,str.Length, StringComparison.OrdinalIgnoreCase)==0);
                else
                    return false;
            }
            return (String.Compare(str,cls.Name, StringComparison.OrdinalIgnoreCase) == 0);
        }
    }
}
