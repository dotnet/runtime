// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

#if ES_BUILD_STANDALONE
namespace Microsoft.Diagnostics.Tracing.Internal
#else
namespace System.Diagnostics.Tracing.Internal
#endif
{
#if ES_BUILD_AGAINST_DOTNET_V35
    using Microsoft.Internal;
#endif
    using Microsoft.Reflection;
    using System.Reflection;

    internal static class Environment
    {
        public static readonly string NewLine = System.Environment.NewLine;

        public static int TickCount
        { get { return System.Environment.TickCount; } }

        public static string GetResourceString(string key, params object[] args)
        {
            string fmt = rm.GetString(key);
            if (fmt != null)
                return string.Format(fmt, args);

            string sargs = String.Empty;
            foreach(var arg in args)
            {
                if (sargs != String.Empty)
                    sargs += ", ";
                sargs += arg.ToString();
            }

            return key + " (" + sargs + ")";
        }

        public static string GetRuntimeResourceString(string key, params object[] args)
        {
            return GetResourceString(key, args);
        }

        private static System.Resources.ResourceManager rm = new System.Resources.ResourceManager("Microsoft.Diagnostics.Tracing.Messages", typeof(Environment).Assembly());
    }
}

#if ES_BUILD_AGAINST_DOTNET_V35

namespace Microsoft.Diagnostics.Contracts.Internal
{
    internal class Contract
    {
        public static void Assert(bool invariant)
        {
            Assert(invariant, string.Empty);
        }
        public static void Assert(bool invariant, string message)
        {
            if (!invariant)
            {
                if (System.Diagnostics.Debugger.IsAttached)
                    System.Diagnostics.Debugger.Break();
                throw new Exception("Assertion failed: " + message);
            }
        }
        public static void EndContractBlock()
        { }
    }
}


namespace Microsoft.Internal
{
    using System.Text;

    internal static class Tuple
    {
        public static Tuple<T1> Create<T1>(T1 item1)
        {
            return new Tuple<T1>(item1);
        }

        public static Tuple<T1, T2> Create<T1, T2>(T1 item1, T2 item2)
        {
            return new Tuple<T1, T2>(item1, item2);
        }
    }

    [Serializable]
    internal class Tuple<T1>
    {
        private readonly T1 m_Item1;

        public T1 Item1 { get { return m_Item1; } }

        public Tuple(T1 item1)
        {
            m_Item1 = item1;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            sb.Append(m_Item1);
            sb.Append(")");
            return sb.ToString();
        }

        int Size
        {
            get
            {
                return 1;
            }
        }
    }

    [Serializable]
    public class Tuple<T1, T2>
    {
        private readonly T1 m_Item1;
        private readonly T2 m_Item2;

        public T1 Item1 { get { return m_Item1; } }
        public T2 Item2 { get { return m_Item2; } }

        public Tuple(T1 item1, T2 item2)
        {
            m_Item1 = item1;
            m_Item2 = item2;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("(");
            sb.Append(m_Item1);
            sb.Append(", ");
            sb.Append(m_Item2);
            sb.Append(")");
            return sb.ToString();
        }

        int Size
        {
            get
            {
                return 2;
            }
        }
    }
}

#endif

namespace Microsoft.Reflection
{
    using System.Reflection;

#if (ES_BUILD_PCL || PROJECTN)
    [Flags]
    public enum BindingFlags
    {
        DeclaredOnly = 0x02,        // Only look at the members declared on the Type
        Instance     = 0x04,        // Include Instance members in search
        Static       = 0x08,        // Include Static members in search
        Public       = 0x10,        // Include Public members in search
        NonPublic    = 0x20,        // Include Non-Public members in search
    }
    
    public enum TypeCode {
        Empty = 0,                  // Null reference
        Object = 1,                 // Instance that isn't a value
        DBNull = 2,                 // Database null value
        Boolean = 3,                // Boolean
        Char = 4,                   // Unicode character
        SByte = 5,                  // Signed 8-bit integer
        Byte = 6,                   // Unsigned 8-bit integer
        Int16 = 7,                  // Signed 16-bit integer
        UInt16 = 8,                 // Unsigned 16-bit integer
        Int32 = 9,                  // Signed 32-bit integer
        UInt32 = 10,                // Unsigned 32-bit integer
        Int64 = 11,                 // Signed 64-bit integer
        UInt64 = 12,                // Unsigned 64-bit integer
        Single = 13,                // IEEE 32-bit float
        Double = 14,                // IEEE 64-bit double
        Decimal = 15,               // Decimal
        DateTime = 16,              // DateTime
        String = 18,                // Unicode character string
    }
#endif
    static class ReflectionExtensions
    {
#if (!ES_BUILD_PCL && !PROJECTN)

        //
        // Type extension methods
        //
        public static bool IsEnum(this Type type) { return type.IsEnum; }
        public static bool IsAbstract(this Type type) { return type.IsAbstract; }
        public static bool IsSealed(this Type type) { return type.IsSealed; }
        public static bool IsValueType(this Type type) { return type.IsValueType; }
        public static bool IsGenericType(this Type type) { return type.IsGenericType; }
        public static Type BaseType(this Type type) { return type.BaseType; }
        public static Assembly Assembly(this Type type) { return type.Assembly; }
        public static TypeCode GetTypeCode(this Type type) { return Type.GetTypeCode(type); }

        public static bool ReflectionOnly(this Assembly assm) { return assm.ReflectionOnly; }

#else // ES_BUILD_PCL

        //
        // Type extension methods
        //
        public static bool IsEnum(this Type type) { return type.GetTypeInfo().IsEnum; }
        public static bool IsAbstract(this Type type) { return type.GetTypeInfo().IsAbstract; }
        public static bool IsSealed(this Type type) { return type.GetTypeInfo().IsSealed; }
        public static bool IsValueType(this Type type) { return type.GetTypeInfo().IsValueType; }
        public static bool IsGenericType(this Type type) { return type.IsConstructedGenericType; }
        public static Type BaseType(this Type type) { return type.GetTypeInfo().BaseType; }
        public static Assembly Assembly(this Type type) { return type.GetTypeInfo().Assembly; }
        public static IEnumerable<PropertyInfo> GetProperties(this Type type) { return type.GetRuntimeProperties(); }
        public static MethodInfo GetGetMethod(this PropertyInfo propInfo) { return propInfo.GetMethod; }
        public static Type[] GetGenericArguments(this Type type) { return type.GenericTypeArguments; }
        
        public static MethodInfo[] GetMethods(this Type type, BindingFlags flags)
        {
            // Minimal implementation to cover only the cases we need
            System.Diagnostics.Debug.Assert((flags & BindingFlags.DeclaredOnly) != 0);
            System.Diagnostics.Debug.Assert((flags & ~(BindingFlags.DeclaredOnly|BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic)) == 0);
            Func<MethodInfo, bool> visFilter;
            Func<MethodInfo, bool> instFilter;
            switch (flags & (BindingFlags.Public | BindingFlags.NonPublic))
            {
                case 0: visFilter = mi => false; break;
                case BindingFlags.Public: visFilter = mi => mi.IsPublic; break;
                case BindingFlags.NonPublic: visFilter = mi => !mi.IsPublic; break;
                default: visFilter = mi => true; break;
            }
            switch (flags & (BindingFlags.Instance | BindingFlags.Static))
            {
                case 0: instFilter = mi => false; break;
                case BindingFlags.Instance: instFilter = mi => !mi.IsStatic; break;
                case BindingFlags.Static: instFilter = mi => mi.IsStatic; break;
                default: instFilter = mi => true; break;
            }
            List<MethodInfo> methodInfos = new List<MethodInfo>();
            foreach (var declaredMethod in type.GetTypeInfo().DeclaredMethods)
            {
                if (visFilter(declaredMethod) && instFilter(declaredMethod))
                    methodInfos.Add(declaredMethod);
            }
            return methodInfos.ToArray();
        }
        public static FieldInfo[] GetFields(this Type type, BindingFlags flags)
        {
            // Minimal implementation to cover only the cases we need
            System.Diagnostics.Debug.Assert((flags & BindingFlags.DeclaredOnly) != 0);
            System.Diagnostics.Debug.Assert((flags & ~(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)) == 0);
            Func<FieldInfo, bool> visFilter;
            Func<FieldInfo, bool> instFilter;
            switch (flags & (BindingFlags.Public | BindingFlags.NonPublic))
            {
                case 0: visFilter = fi => false; break;
                case BindingFlags.Public: visFilter = fi => fi.IsPublic; break;
                case BindingFlags.NonPublic: visFilter = fi => !fi.IsPublic; break;
                default: visFilter = fi => true; break;
            }
            switch (flags & (BindingFlags.Instance | BindingFlags.Static))
            {
                case 0: instFilter = fi => false; break;
                case BindingFlags.Instance: instFilter = fi => !fi.IsStatic; break;
                case BindingFlags.Static: instFilter = fi => fi.IsStatic; break;
                default: instFilter = fi => true; break;
            }
            List<FieldInfo> fieldInfos = new List<FieldInfo>();
            foreach (var declaredField in type.GetTypeInfo().DeclaredFields)
            {
                if (visFilter(declaredField) && instFilter(declaredField))
                    fieldInfos.Add(declaredField);
            }
            return fieldInfos.ToArray();
        }
        public static Type GetNestedType(this Type type, string nestedTypeName)
        {
            TypeInfo ti = null;
            foreach(var nt in type.GetTypeInfo().DeclaredNestedTypes)
            {
                if (nt.Name == nestedTypeName)
                {
                    ti = nt;
                    break;
                }
            }
            return ti == null ? null : ti.AsType();
        }
        public static TypeCode GetTypeCode(this Type type) 
        {
            if (type == typeof(bool)) return TypeCode.Boolean;
            else if (type == typeof(byte)) return TypeCode.Byte;
            else if (type == typeof(char)) return TypeCode.Char;
            else if (type == typeof(ushort)) return TypeCode.UInt16;
            else if (type == typeof(uint)) return TypeCode.UInt32;
            else if (type == typeof(ulong)) return TypeCode.UInt64;
            else if (type == typeof(sbyte)) return TypeCode.SByte;
            else if (type == typeof(short)) return TypeCode.Int16;
            else if (type == typeof(int)) return TypeCode.Int32;
            else if (type == typeof(long)) return TypeCode.Int64;
            else if (type == typeof(string)) return TypeCode.String;
            else if (type == typeof(float)) return TypeCode.Single;
            else if (type == typeof(double)) return TypeCode.Double;
            else if (type == typeof(DateTime)) return TypeCode.DateTime;
            else if (type == (typeof(Decimal))) return TypeCode.Decimal;
            else return TypeCode.Object;
        }

        //
        // FieldInfo extension methods
        //
        public static object GetRawConstantValue(this FieldInfo fi)
        { return fi.GetValue(null); }

        //
        // Assembly extension methods
        //
        public static bool ReflectionOnly(this Assembly assm)
        {
            // In PCL we can't load in reflection-only context
            return false;
        }

#endif
    }
}

// Defining some no-ops in PCL builds
#if ES_BUILD_PCL || PROJECTN
namespace System.Security
{
    class SuppressUnmanagedCodeSecurityAttribute : Attribute { }

    enum SecurityAction { Demand }
}
namespace System.Security.Permissions
{
    class HostProtectionAttribute : Attribute { public bool MayLeakOnAbort { get; set; } }
    class PermissionSetAttribute : Attribute
    { 
        public PermissionSetAttribute(System.Security.SecurityAction action) { }
        public bool Unrestricted { get; set; }
    }
}
#endif

#if PROJECTN
namespace System
{
    public static class AppDomain
    {
        public static int GetCurrentThreadId()
        {
            return (int)Interop.Kernel32.GetCurrentThreadId();
        }
    }    
}
#endif
