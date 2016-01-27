// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
//
// 
// This interface defines a set of methods which interact with reflection
//    during the binding process.  This control allows systems to apply language
//    specific semantics to the binding and invocation process.
//
//
namespace System.Reflection {
    using System;
    using System.Runtime.InteropServices;
    using CultureInfo = System.Globalization.CultureInfo;
    
    [Serializable]
    [ClassInterface(ClassInterfaceType.AutoDual)]
[System.Runtime.InteropServices.ComVisible(true)]
    public abstract class Binder
    {    
        // Given a set of methods that match the basic criteria, select a method to
        // invoke.  When this method is finished, we should have 
        public abstract MethodBase BindToMethod(BindingFlags bindingAttr,MethodBase[] match,ref Object[] args,
            ParameterModifier[] modifiers,CultureInfo culture,String[] names, out Object state);
    
        // Given a set of methods that match the basic criteria, select a method to
        // invoke.  When this method is finished, we should have 
        public abstract FieldInfo BindToField(BindingFlags bindingAttr,FieldInfo[] match,
            Object value,CultureInfo culture);
                                       
        // Given a set of methods that match the base criteria, select a method based
        // upon an array of types.  This method should return null if no method matchs
        // the criteria.
        public abstract MethodBase SelectMethod(BindingFlags bindingAttr,MethodBase[] match,
            Type[] types,ParameterModifier[] modifiers);
        
        
        // Given a set of propreties that match the base criteria, select one.
        public abstract PropertyInfo SelectProperty(BindingFlags bindingAttr,PropertyInfo[] match,
            Type returnType,Type[] indexes,ParameterModifier[] modifiers);
        
        // ChangeType
        // This method will convert the value into the property type.
        //    It throws a cast exception if this fails.
        public abstract Object ChangeType(Object value,Type type,CultureInfo culture);        

        public abstract void ReorderArgumentArray(ref Object[] args, Object state);

#if !FEATURE_COMINTEROP
        // CanChangeType
        // This method checks whether the value can be converted into the property type.
        public virtual bool CanChangeType(Object value,Type type,CultureInfo culture)
        {
            return false;
        }
#endif
    }
}
