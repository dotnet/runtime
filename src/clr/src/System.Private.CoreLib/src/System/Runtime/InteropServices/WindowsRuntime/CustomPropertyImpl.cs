// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Runtime.InteropServices.WindowsRuntime
{
    //
    // ICustomProperty implementation - basically a wrapper of PropertyInfo
    //
    internal sealed class CustomPropertyImpl : ICustomProperty
    {
        private PropertyInfo m_property;

        //
        // Constructor
        //
        public CustomPropertyImpl(PropertyInfo propertyInfo)
        {
            if (propertyInfo == null)
                throw new ArgumentNullException(nameof(propertyInfo));

            m_property = propertyInfo;
        }

        //
        // ICustomProperty interface implementation
        //

        public string Name
        {
            get
            {
                return m_property.Name;
            }
        }

        public bool CanRead
        {
            get
            {
                // Return false if the getter is not public
                return m_property.GetGetMethod() != null;
            }
        }

        public bool CanWrite
        {
            get
            {
                // Return false if the setter is not public
                return m_property.GetSetMethod() != null;
            }
        }

        public object? GetValue(object target)
        {
            return InvokeInternal(target, null, true);
        }

        // Unlike normal .Net, Jupiter properties can have at most one indexer parameter. A null
        // indexValue here means that the property has an indexer argument and its value is null.
        public object? GetValue(object target, object indexValue)
        {
            return InvokeInternal(target, new object[] { indexValue }, true);
        }

        public void SetValue(object target, object value)
        {
            InvokeInternal(target, new object[] { value }, false);
        }

        // Unlike normal .Net, Jupiter properties can have at most one indexer parameter. A null
        // indexValue here means that the property has an indexer argument and its value is null.
        public void SetValue(object target, object value, object indexValue)
        {
            InvokeInternal(target, new object[] { indexValue, value }, false);
        }

        private object? InvokeInternal(object target, object[]? args, bool getValue)
        {
            // Forward to the right object if we are dealing with a proxy
            if (target is IGetProxyTarget proxy)
            {
                target = proxy.GetTarget();
            }

            // You can get PropertyInfo for properties with a private getter/public setter (or vice versa) 
            // even if you pass BindingFlags.Public only. And in this case, passing binding flags to 
            // GetValue/SetValue won't work as the default binder ignores those values
            // Use GetGetMethod/GetSetMethod instead

            // We get non-public accessors just so that we can throw the correct exception.
            MethodInfo? accessor = getValue ? m_property.GetGetMethod(true) : m_property.GetSetMethod(true);

            if (accessor == null)
                throw new ArgumentException(getValue ? SR.Arg_GetMethNotFnd : SR.Arg_SetMethNotFnd);

            if (!accessor.IsPublic)
                throw new MethodAccessException(
                    SR.Format(
                        SR.Arg_MethodAccessException_WithMethodName,
                        accessor,
                        accessor.DeclaringType!.FullName));

            RuntimeMethodInfo? rtMethod = accessor as RuntimeMethodInfo;
            if (rtMethod == null)
                throw new ArgumentException(SR.Argument_MustBeRuntimeMethodInfo);

            // We can safely skip access check because this is only used in full trust scenarios.
            // And we have already verified that the property accessor is public.
            return rtMethod.Invoke(target, BindingFlags.Default, null, args, null);
        }

        public Type Type
        {
            get
            {
                return m_property.PropertyType;
            }
        }
    }
}
