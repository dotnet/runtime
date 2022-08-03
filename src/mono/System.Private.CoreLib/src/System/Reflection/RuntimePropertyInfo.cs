// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// (C) 2001 Ximian, Inc.  http://www.ximian.com
// Copyright (C) 2004-2005 Novell, Inc (http://www.novell.com)
// Copyright 2013 Xamarin, Inc (http://www.xamarin.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;
using System.Globalization;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Mono;

namespace System.Reflection
{
    internal struct MonoPropertyInfo
    {
        public Type parent;
        public Type declaring_type;
        public string name;
        public MethodInfo get_method;
        public MethodInfo set_method;
        public PropertyAttributes attrs;
    }

    [Flags]
    internal enum PInfo
    {
        Attributes = 1,
        GetMethod = 1 << 1,
        SetMethod = 1 << 2,
        ReflectedType = 1 << 3,
        DeclaringType = 1 << 4,
        Name = 1 << 5

    }

    internal delegate object GetterAdapter(object _this);
    internal delegate R Getter<T, R>(T _this);

    [StructLayout(LayoutKind.Sequential)]
    internal sealed class RuntimePropertyInfo : PropertyInfo
    {
#pragma warning disable 649
        internal IntPtr klass;
        internal IntPtr prop;
        private MonoPropertyInfo info;
        private PInfo cached;
        private GetterAdapter? cached_getter;
#pragma warning restore 649

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void get_property_info(RuntimePropertyInfo prop, ref MonoPropertyInfo info,
                                   PInfo req_info);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern Type[] GetTypeModifiers(RuntimePropertyInfo prop, bool optional);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern object get_default_value(RuntimePropertyInfo prop);


        internal BindingFlags BindingFlags
        {
            get
            {
                CachePropertyInfo(PInfo.GetMethod | PInfo.SetMethod);
                bool isPublic = info.set_method?.IsPublic == true || info.get_method?.IsPublic == true;
                bool isStatic = info.set_method?.IsStatic == true || info.get_method?.IsStatic == true;
                bool isInherited = DeclaringType != ReflectedType;
                return FilterPreCalculate(isPublic, isInherited, isStatic);
            }
        }

        // Copied from https://github.com/dotnet/coreclr/blob/7a24a538cd265993e5864179f51781398c28ecdf/src/System.Private.CoreLib/src/System/RtType.cs#L2022
        private static BindingFlags FilterPreCalculate(bool isPublic, bool isInherited, bool isStatic)
        {
            BindingFlags bindingFlags = isPublic ? BindingFlags.Public : BindingFlags.NonPublic;
            if (isInherited)
            {
                // We arrange things so the DeclaredOnly flag means "include inherited members"
                bindingFlags |= BindingFlags.DeclaredOnly;
                if (isStatic)
                    bindingFlags |= BindingFlags.Static | BindingFlags.FlattenHierarchy;
                else
                    bindingFlags |= BindingFlags.Instance;
            }
            else
            {
                if (isStatic)
                    bindingFlags |= BindingFlags.Static;
                else
                    bindingFlags |= BindingFlags.Instance;
            }
            return bindingFlags;
        }

        public override Module Module
        {
            get
            {
                return GetRuntimeModule();
            }
        }

        internal RuntimeType GetDeclaringTypeInternal()
        {
            return (RuntimeType)DeclaringType;
        }

        internal RuntimeModule GetRuntimeModule()
        {
            return GetDeclaringTypeInternal().GetRuntimeModule();
        }

        #region Object Overrides
        public override string ToString()
        {
            return FormatNameAndSig();
        }

        private string FormatNameAndSig()
        {
            StringBuilder sbName = new StringBuilder(PropertyType.FormatTypeName());

            sbName.Append(' ');
            sbName.Append(Name);

            ParameterInfo[] pi = GetIndexParameters();
            if (pi.Length > 0)
            {
                sbName.Append(" [");
                RuntimeParameterInfo.FormatParameters(sbName, pi, 0);
                sbName.Append(']');
            }

            return sbName.ToString();
        }
        #endregion

        private void CachePropertyInfo(PInfo flags)
        {
            if ((cached & flags) != flags)
            {
                get_property_info(this, ref info, flags);
                cached |= flags;
            }
        }

        public override PropertyAttributes Attributes
        {
            get
            {
                CachePropertyInfo(PInfo.Attributes);
                return info.attrs;
            }
        }

        public override bool CanRead
        {
            get
            {
                CachePropertyInfo(PInfo.GetMethod);
                return (info.get_method != null);
            }
        }

        public override bool CanWrite
        {
            get
            {
                CachePropertyInfo(PInfo.SetMethod);
                return (info.set_method != null);
            }
        }

        public override Type PropertyType
        {
            get
            {
                CachePropertyInfo(PInfo.GetMethod | PInfo.SetMethod);

                if (info.get_method != null)
                {
                    return info.get_method.ReturnType;
                }
                else
                {
                    ParameterInfo[] parameters = info.set_method.GetParametersInternal();
                    if (parameters.Length == 0)
                        throw new ArgumentException(SR.SetterHasNoParams, "indexer");

                    return parameters[parameters.Length - 1].ParameterType;
                }
            }
        }

        public override Type ReflectedType
        {
            get
            {
                CachePropertyInfo(PInfo.ReflectedType);
                return info.parent;
            }
        }

        public override Type DeclaringType
        {
            get
            {
                CachePropertyInfo(PInfo.DeclaringType);
                return info.declaring_type;
            }
        }

        public override string Name
        {
            get
            {
                CachePropertyInfo(PInfo.Name);
                return info.name;
            }
        }

        public override MethodInfo[] GetAccessors(bool nonPublic)
        {
            int nget = 0;
            int nset = 0;

            CachePropertyInfo(PInfo.GetMethod | PInfo.SetMethod);

            if (info.set_method != null && (nonPublic || info.set_method.IsPublic))
                nset = 1;
            if (info.get_method != null && (nonPublic || info.get_method.IsPublic))
                nget = 1;

            MethodInfo[] res = new MethodInfo[nget + nset];
            int n = 0;
            if (nset != 0)
                res[n++] = info.set_method!;
            if (nget != 0)
                res[n++] = info.get_method!;
            return res;
        }

        public override MethodInfo? GetGetMethod(bool nonPublic)
        {
            CachePropertyInfo(PInfo.GetMethod);
            if (info.get_method != null && (nonPublic || info.get_method.IsPublic))
                return info.get_method;
            else
                return null;
        }

        public override ParameterInfo[] GetIndexParameters()
        {
            CachePropertyInfo(PInfo.GetMethod | PInfo.SetMethod);
            ParameterInfo[] src;
            int length;
            if (info.get_method != null)
            {
                src = info.get_method.GetParametersInternal();
                length = src.Length;
            }
            else if (info.set_method != null)
            {
                src = info.set_method.GetParametersInternal();
                length = src.Length - 1;
            }
            else
                return Array.Empty<ParameterInfo>();

            var dest = new ParameterInfo[length];
            for (int i = 0; i < length; ++i)
            {
                dest[i] = RuntimeParameterInfo.New(src[i], this);
            }
            return dest;
        }

        public override MethodInfo? GetSetMethod(bool nonPublic)
        {
            CachePropertyInfo(PInfo.SetMethod);
            if (info.set_method != null && (nonPublic || info.set_method.IsPublic))
                return info.set_method;
            else
                return null;
        }


        /*TODO verify for attribute based default values, just like ParameterInfo*/
        public override object GetConstantValue()
        {
            return get_default_value(this);
        }

        public override object GetRawConstantValue()
        {
            return get_default_value(this);
        }

        // According to MSDN the inherit parameter is ignored here and
        // the behavior always defaults to inherit = false
        //
        public override bool IsDefined(Type attributeType, bool inherit)
        {
            return CustomAttribute.IsDefined(this, attributeType, false);
        }

        public override object[] GetCustomAttributes(bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, false);
        }

        public override object[] GetCustomAttributes(Type attributeType, bool inherit)
        {
            return CustomAttribute.GetCustomAttributes(this, attributeType, false);
        }


        private delegate object? GetterAdapter(object? _this);
        private delegate R Getter<T, R>(T _this);
        private delegate R StaticGetter<R>();

#pragma warning disable 169
        // Used via reflection
        private static object? GetterAdapterFrame<T, R>(Getter<T, R> getter, object? obj)
        {
            return getter((T)obj!);
        }

        private static object? StaticGetterAdapterFrame<R>(StaticGetter<R> getter, object? _)
        {
            return getter();
        }
#pragma warning restore 169

        /*
         * The idea behind this optimization is to use a pair of delegates to simulate the same effect of doing a reflection call.
         * The first delegate cast the this argument to the right type and the second does points to the target method.
         */
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060:UnrecognizedReflectionPattern",
            Justification = "MethodInfo used with MakeGenericMethod doesn't have DynamicallyAccessedMembers generic parameters")]
        [DynamicDependency("GetterAdapterFrame`2")]
        [DynamicDependency("StaticGetterAdapterFrame`1")]
        private static GetterAdapter CreateGetterDelegate(MethodInfo method)
        {
            Type[] typeVector;
            Type getterType;
            object getterDelegate;
            MethodInfo adapterFrame;
            Type getterDelegateType;
            string frameName;

            if (method.IsStatic)
            {
                typeVector = new Type[] { method.ReturnType };
                getterDelegateType = typeof(StaticGetter<>);
                frameName = "StaticGetterAdapterFrame";
            }
            else
            {
                typeVector = new Type[] { method.DeclaringType!, method.ReturnType };
                getterDelegateType = typeof(Getter<,>);
                frameName = "GetterAdapterFrame";
            }

            getterType = getterDelegateType.MakeGenericType(typeVector);
            getterDelegate = Delegate.CreateDelegate(getterType, method);
            adapterFrame = typeof(RuntimePropertyInfo).GetMethod(frameName, BindingFlags.Static | BindingFlags.NonPublic)!;
            adapterFrame = adapterFrame.MakeGenericMethod(typeVector);
            return (GetterAdapter)Delegate.CreateDelegate(typeof(GetterAdapter), getterDelegate, adapterFrame, true);
        }

        public override object? GetValue(object? obj, object?[]? index)
        {
            if ((index == null || index.Length == 0) && RuntimeFeature.IsDynamicCodeSupported)
            {
                /*FIXME we should check if the number of arguments matches the expected one, otherwise the error message will be pretty criptic.*/
                if (cached_getter == null)
                {
                    MethodInfo? method = GetGetMethod(true);
                    if (method == null)
                        throw new ArgumentException($"Get Method not found for '{Name}'");
                    if (!DeclaringType.IsValueType && !PropertyType.IsByRef && !method.ContainsGenericParameters)
                    {
                        //FIXME find a way to build an invoke delegate for value types.
                        cached_getter = CreateGetterDelegate(method);
                        // The try-catch preserves the .Invoke () behaviour
                        try
                        {
                            return cached_getter(obj);
                        }
                        catch (Exception ex)
                        {
                            throw new TargetInvocationException(ex);
                        }
                    }
                }
                else
                {
                    try
                    {
                        return cached_getter(obj);
                    }
                    catch (Exception ex)
                    {
                        throw new TargetInvocationException(ex);
                    }
                }
            }

            return GetValue(obj, BindingFlags.Default, null, index, null);
        }

        public override object? GetValue(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            object? ret;

            MethodInfo? method = GetGetMethod(true);
            if (method == null)
                throw new ArgumentException($"Get Method not found for '{Name}'");

            if (index == null || index.Length == 0)
                ret = method.Invoke(obj, invokeAttr, binder, null, culture);
            else
                ret = method.Invoke(obj, invokeAttr, binder, index, culture);

            return ret;
        }

        public override void SetValue(object? obj, object? value, BindingFlags invokeAttr, Binder? binder, object?[]? index, CultureInfo? culture)
        {
            MethodInfo? method = GetSetMethod(true);
            if (method == null)
                throw new ArgumentException("Set Method not found for '" + Name + "'");

            object?[] parms;
            if (index == null || index.Length == 0)
                parms = new object?[] { value };
            else
            {
                int ilen = index.Length;
                parms = new object[ilen + 1];
                index.CopyTo(parms, 0);
                parms[ilen] = value;
            }

            method.Invoke(obj, invokeAttr, binder, parms, culture);
        }

        public override Type[] GetOptionalCustomModifiers() => GetCustomModifiers(true);

        public override Type[] GetRequiredCustomModifiers() => GetCustomModifiers(false);

        private Type[] GetCustomModifiers(bool optional) => GetTypeModifiers(this, optional) ?? Type.EmptyTypes;

        public override IList<CustomAttributeData> GetCustomAttributesData()
        {
            return RuntimeCustomAttributeData.GetCustomAttributesInternal(this);
        }

        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) => HasSameMetadataDefinitionAsCore<RuntimePropertyInfo>(other);

        public override int MetadataToken
        {
            get
            {
                return get_metadata_token(this);
            }
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern int get_metadata_token(RuntimePropertyInfo monoProperty);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern PropertyInfo internal_from_handle_type(IntPtr event_handle, IntPtr type_handle);

        internal static PropertyInfo GetPropertyFromHandle(RuntimePropertyHandle handle, RuntimeTypeHandle reflectedType)
        {
            if (handle.Value == IntPtr.Zero)
                throw new ArgumentException("The handle is invalid.");
            PropertyInfo pi = internal_from_handle_type(handle.Value, reflectedType.Value);
            if (pi == null)
                throw new ArgumentException("The property handle and the type handle are incompatible.");
            return pi;
        }
    }
}
