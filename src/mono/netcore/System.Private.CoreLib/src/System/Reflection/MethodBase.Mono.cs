// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Reflection
{
    public partial class MethodBase
    {
        public static MethodBase? GetMethodFromHandle(RuntimeMethodHandle handle)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(SR.Argument_InvalidHandle);

            MethodBase? m = RuntimeMethodInfo.GetMethodFromHandleInternalType(handle.Value, IntPtr.Zero);
            if (m == null)
                throw new ArgumentException(SR.Argument_InvalidHandle);

            Type? declaringType = m.DeclaringType;
            if (declaringType != null && declaringType.IsGenericType)
                throw new ArgumentException(string.Format(SR.Argument_MethodDeclaringTypeGeneric,
                                                            m, declaringType.GetGenericTypeDefinition()));

            return m;
        }

        public static MethodBase? GetMethodFromHandle(RuntimeMethodHandle handle, RuntimeTypeHandle declaringType)
        {
            if (handle.IsNullHandle())
                throw new ArgumentException(SR.Argument_InvalidHandle);
            MethodBase m = RuntimeMethodInfo.GetMethodFromHandleInternalType(handle.Value, declaringType.Value);
            if (m == null)
                throw new ArgumentException(SR.Argument_InvalidHandle);
            return m;
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        public static extern MethodBase? GetCurrentMethod();

        internal virtual ParameterInfo[] GetParametersNoCopy()
        {
            return GetParametersInternal();
        }

        internal virtual ParameterInfo[] GetParametersInternal()
        {
            throw new NotImplementedException();
        }

        internal virtual int GetParametersCount()
        {
            throw new NotImplementedException();
        }

        internal virtual Type GetParameterType(int pos)
        {
            throw new NotImplementedException();
        }

        internal virtual Type[] GetParameterTypes()
        {
            ParameterInfo[] paramInfo = GetParametersNoCopy();

            Type[] parameterTypes = new Type[paramInfo.Length];
            for (int i = 0; i < paramInfo.Length; i++)
                parameterTypes[i] = paramInfo[i].ParameterType;

            return parameterTypes;
        }

        internal virtual int get_next_table_index(object obj, int table, int count)
        {
            throw new NotImplementedException();
        }
    }
}
