// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System
{
    // This file collects the longer methods of Type to make the main Type class more readable.
    public abstract partial class Type : MemberInfo, IReflect
    {
        public virtual bool IsSerializable
        {
            get
            {
                if ((GetAttributeFlagsImpl() & TypeAttributes.Serializable) != 0)
                    return true;

                Type underlyingType = UnderlyingSystemType;
                if (underlyingType.IsRuntimeImplemented())
                {
                    do
                    {
                        // In all sane cases we only need to compare the direct level base type with
                        // System.Enum and System.MulticastDelegate. However, a generic parameter can
                        // have a base type constraint that is Delegate or even a real delegate type.
                        // Let's maintain compatibility and return true for them.
                        if (underlyingType == typeof(Delegate) || underlyingType == typeof(Enum))
                            return true;

                        underlyingType = underlyingType.BaseType;
                    }
                    while (underlyingType != null);
                }

                return false;
            }
        }

        public virtual bool ContainsGenericParameters
        {
            get
            {
                if (HasElementType)
                    return GetRootElementType().ContainsGenericParameters;

                if (IsGenericParameter)
                    return true;

                if (!IsGenericType)
                    return false;

                Type[] genericArguments = GetGenericArguments();
                for (int i = 0; i < genericArguments.Length; i++)
                {
                    if (genericArguments[i].ContainsGenericParameters)
                        return true;
                }

                return false;
            }
        }

        internal Type GetRootElementType()
        {
            Type rootElementType = this;

            while (rootElementType.HasElementType)
                rootElementType = rootElementType.GetElementType();

            return rootElementType;
        }

        public bool IsVisible
        {
            get
            {
#if CORECLR
                RuntimeType rt = this as RuntimeType;
                if (rt != null)
                    return RuntimeTypeHandle.IsVisible(rt);
#endif //CORECLR

                if (IsGenericParameter)
                    return true;

                if (HasElementType)
                    return GetElementType().IsVisible;

                Type type = this;
                while (type.IsNested)
                {
                    if (!type.IsNestedPublic)
                        return false;

                    // this should be null for non-nested types.
                    type = type.DeclaringType;
                }

                // Now "type" should be a top level type
                if (!type.IsPublic)
                    return false;

                if (IsGenericType && !IsGenericTypeDefinition)
                {
                    foreach (Type t in GetGenericArguments())
                    {
                        if (!t.IsVisible)
                            return false;
                    }
                }

                return true;
            }
        }

        public virtual Type[] FindInterfaces(TypeFilter filter, object filterCriteria)
        {
            if (filter == null)
                throw new ArgumentNullException(nameof(filter));

            Type[] c = GetInterfaces();
            int cnt = 0;
            for (int i = 0; i < c.Length; i++)
            {
                if (!filter(c[i], filterCriteria))
                    c[i] = null;
                else
                    cnt++;
            }
            if (cnt == c.Length)
                return c;

            Type[] ret = new Type[cnt];
            cnt = 0;
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] != null)
                    ret[cnt++] = c[i];
            }
            return ret;
        }

        public virtual MemberInfo[] FindMembers(MemberTypes memberType, BindingFlags bindingAttr, MemberFilter filter, object filterCriteria)
        {
            // Define the work arrays
            MethodInfo[] m = null;
            ConstructorInfo[] c = null;
            FieldInfo[] f = null;
            PropertyInfo[] p = null;
            EventInfo[] e = null;
            Type[] t = null;

            int i = 0;
            int cnt = 0;            // Total Matchs

            // Check the methods
            if ((memberType & MemberTypes.Method) != 0)
            {
                m = GetMethods(bindingAttr);
                if (filter != null)
                {
                    for (i = 0; i < m.Length; i++)
                        if (!filter(m[i], filterCriteria))
                            m[i] = null;
                        else
                            cnt++;
                }
                else
                {
                    cnt += m.Length;
                }
            }

            // Check the constructors
            if ((memberType & MemberTypes.Constructor) != 0)
            {
                c = GetConstructors(bindingAttr);
                if (filter != null)
                {
                    for (i = 0; i < c.Length; i++)
                        if (!filter(c[i], filterCriteria))
                            c[i] = null;
                        else
                            cnt++;
                }
                else
                {
                    cnt += c.Length;
                }
            }

            // Check the fields
            if ((memberType & MemberTypes.Field) != 0)
            {
                f = GetFields(bindingAttr);
                if (filter != null)
                {
                    for (i = 0; i < f.Length; i++)
                        if (!filter(f[i], filterCriteria))
                            f[i] = null;
                        else
                            cnt++;
                }
                else
                {
                    cnt += f.Length;
                }
            }

            // Check the Properties
            if ((memberType & MemberTypes.Property) != 0)
            {
                p = GetProperties(bindingAttr);
                if (filter != null)
                {
                    for (i = 0; i < p.Length; i++)
                        if (!filter(p[i], filterCriteria))
                            p[i] = null;
                        else
                            cnt++;
                }
                else
                {
                    cnt += p.Length;
                }
            }

            // Check the Events
            if ((memberType & MemberTypes.Event) != 0)
            {
                e = GetEvents(bindingAttr);
                if (filter != null)
                {
                    for (i = 0; i < e.Length; i++)
                        if (!filter(e[i], filterCriteria))
                            e[i] = null;
                        else
                            cnt++;
                }
                else
                {
                    cnt += e.Length;
                }
            }

            // Check the Types
            if ((memberType & MemberTypes.NestedType) != 0)
            {
                t = GetNestedTypes(bindingAttr);
                if (filter != null)
                {
                    for (i = 0; i < t.Length; i++)
                        if (!filter(t[i], filterCriteria))
                            t[i] = null;
                        else
                            cnt++;
                }
                else
                {
                    cnt += t.Length;
                }
            }

            // Allocate the Member Info
            MemberInfo[] ret = new MemberInfo[cnt];

            // Copy the Methods
            cnt = 0;
            if (m != null)
            {
                for (i = 0; i < m.Length; i++)
                    if (m[i] != null)
                        ret[cnt++] = m[i];
            }

            // Copy the Constructors
            if (c != null)
            {
                for (i = 0; i < c.Length; i++)
                    if (c[i] != null)
                        ret[cnt++] = c[i];
            }

            // Copy the Fields
            if (f != null)
            {
                for (i = 0; i < f.Length; i++)
                    if (f[i] != null)
                        ret[cnt++] = f[i];
            }

            // Copy the Properties
            if (p != null)
            {
                for (i = 0; i < p.Length; i++)
                    if (p[i] != null)
                        ret[cnt++] = p[i];
            }

            // Copy the Events
            if (e != null)
            {
                for (i = 0; i < e.Length; i++)
                    if (e[i] != null)
                        ret[cnt++] = e[i];
            }

            // Copy the Types
            if (t != null)
            {
                for (i = 0; i < t.Length; i++)
                    if (t[i] != null)
                        ret[cnt++] = t[i];
            }

            return ret;
        }

        public virtual bool IsSubclassOf(Type c)
        {
            Type p = this;
            if (p == c)
                return false;
            while (p != null)
            {
                if (p == c)
                    return true;
                p = p.BaseType;
            }
            return false;
        }

        public virtual bool IsAssignableFrom(Type c)
        {
            if (c == null)
                return false;

            if (this == c)
                return true;

            // For backward-compatibility, we need to special case for the types
            // whose UnderlyingSystemType are runtime implemented. 
            Type toType = this.UnderlyingSystemType;
            if (toType.IsRuntimeImplemented())
                return toType.IsAssignableFrom(c);

            // If c is a subclass of this class, then c can be cast to this type.
            if (c.IsSubclassOf(this))
                return true;

            if (this.IsInterface)
            {
                return c.ImplementInterface(this);
            }
            else if (IsGenericParameter)
            {
                Type[] constraints = GetGenericParameterConstraints();
                for (int i = 0; i < constraints.Length; i++)
                    if (!constraints[i].IsAssignableFrom(c))
                        return false;

                return true;
            }

            return false;
        }

        internal bool ImplementInterface(Type ifaceType)
        {
            Type t = this;
            while (t != null)
            {
                Type[] interfaces = t.GetInterfaces();
                if (interfaces != null)
                {
                    for (int i = 0; i < interfaces.Length; i++)
                    {
                        // Interfaces don't derive from other interfaces, they implement them.
                        // So instead of IsSubclassOf, we should use ImplementInterface instead.
                        if (interfaces[i] == ifaceType ||
                            (interfaces[i] != null && interfaces[i].ImplementInterface(ifaceType)))
                            return true;
                    }
                }

                t = t.BaseType;
            }

            return false;
        }

        // FilterAttribute
        //  This method will search for a member based upon the attribute passed in.
        //  filterCriteria -- an Int32 representing the attribute
        private static bool FilterAttributeImpl(MemberInfo m, object filterCriteria)
        {
            // Check that the criteria object is an Integer object
            if (filterCriteria == null)
                throw new InvalidFilterCriteriaException(SR.InvalidFilterCriteriaException_CritInt);

            switch (m.MemberType)
            {
                case MemberTypes.Constructor:
                case MemberTypes.Method:
                    {
                        MethodAttributes criteria = 0;
                        try
                        {
                            int i = (int)filterCriteria;
                            criteria = (MethodAttributes)i;
                        }
                        catch
                        {
                            throw new InvalidFilterCriteriaException(SR.InvalidFilterCriteriaException_CritInt);
                        }


                        MethodAttributes attr;
                        if (m.MemberType == MemberTypes.Method)
                            attr = ((MethodInfo)m).Attributes;
                        else
                            attr = ((ConstructorInfo)m).Attributes;

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
                        if (((criteria & MethodAttributes.SpecialName) != 0) && (attr & MethodAttributes.SpecialName) == 0)
                            return false;
                        return true;
                    }
                case MemberTypes.Field:
                    {
                        FieldAttributes criteria = 0;
                        try
                        {
                            int i = (int)filterCriteria;
                            criteria = (FieldAttributes)i;
                        }
                        catch
                        {
                            throw new InvalidFilterCriteriaException(SR.InvalidFilterCriteriaException_CritInt);
                        }

                        FieldAttributes attr = ((FieldInfo)m).Attributes;
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
        private static bool FilterNameImpl(MemberInfo m, object filterCriteria)
        {
            // Check that the criteria object is a String object
            if (filterCriteria == null || !(filterCriteria is string))
                throw new InvalidFilterCriteriaException(SR.InvalidFilterCriteriaException_CritString);

            // At the moment this fails if its done on a single line....
            string str = ((string)filterCriteria);
            str = str.Trim();

            string name = m.Name;
            // Get the nested class name only, as opposed to the mangled one
            if (m.MemberType == MemberTypes.NestedType)
                name = name.Substring(name.LastIndexOf('+') + 1);
            // Check to see if this is a prefix or exact match requirement
            if (str.Length > 0 && str[str.Length - 1] == '*')
            {
                str = str.Substring(0, str.Length - 1);
                return (name.StartsWith(str, StringComparison.Ordinal));
            }

            return (name.Equals(str));
        }

        // FilterIgnoreCase
        // This delegate will do a name search but does it with the
        //  ignore case specified.
        private static bool FilterNameIgnoreCaseImpl(MemberInfo m, object filterCriteria)
        {
            // Check that the criteria object is a String object
            if (filterCriteria == null || !(filterCriteria is string))
                throw new InvalidFilterCriteriaException(SR.InvalidFilterCriteriaException_CritString);

            string str = (string)filterCriteria;
            str = str.Trim();

            string name = m.Name;
            // Get the nested class name only, as opposed to the mangled one
            if (m.MemberType == MemberTypes.NestedType)
                name = name.Substring(name.LastIndexOf('+') + 1);
            // Check to see if this is a prefix or exact match requirement
            if (str.Length > 0 && str[str.Length - 1] == '*')
            {
                str = str.Substring(0, str.Length - 1);
                return (string.Compare(name, 0, str, 0, str.Length, StringComparison.OrdinalIgnoreCase) == 0);
            }

            return (string.Compare(str, name, StringComparison.OrdinalIgnoreCase) == 0);
        }
    }
}

