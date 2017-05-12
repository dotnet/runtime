// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

////////////////////////////////////////////////////////////////////////////////
////////////////////////////////////////////////////////////////////////////////

using System;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Interface | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
    public sealed class TypeIdentifierAttribute : Attribute
    {
        public TypeIdentifierAttribute() { }
        public TypeIdentifierAttribute(string scope, string identifier) { Scope_ = scope; Identifier_ = identifier; }

        public String Scope { get { return Scope_; } }
        public String Identifier { get { return Identifier_; } }

        internal String Scope_;
        internal String Identifier_;
    }

    // To be used on methods that sink reverse P/Invoke calls.
    // This attribute is a CoreCLR-only security measure, currently ignored by the desktop CLR.
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class AllowReversePInvokeCallsAttribute : Attribute
    {
        public AllowReversePInvokeCallsAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Event, Inherited = false)]
    public sealed class DispIdAttribute : Attribute
    {
        internal int _val;
        public DispIdAttribute(int dispId)
        {
            _val = dispId;
        }
        public int Value { get { return _val; } }
    }

    public enum ComInterfaceType
    {
        InterfaceIsDual = 0,
        InterfaceIsIUnknown = 1,
        InterfaceIsIDispatch = 2,

        InterfaceIsIInspectable = 3,
    }

    [AttributeUsage(AttributeTargets.Interface, Inherited = false)]
    public sealed class InterfaceTypeAttribute : Attribute
    {
        internal ComInterfaceType _val;
        public InterfaceTypeAttribute(ComInterfaceType interfaceType)
        {
            _val = interfaceType;
        }
        public InterfaceTypeAttribute(short interfaceType)
        {
            _val = (ComInterfaceType)interfaceType;
        }
        public ComInterfaceType Value { get { return _val; } }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ComDefaultInterfaceAttribute : Attribute
    {
        internal Type _val;

        public ComDefaultInterfaceAttribute(Type defaultInterface)
        {
            _val = defaultInterface;
        }

        public Type Value { get { return _val; } }
    }

    public enum ClassInterfaceType
    {
        None = 0,
        AutoDispatch = 1,
        AutoDual = 2
    }

    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class, Inherited = false)]
    public sealed class ClassInterfaceAttribute : Attribute
    {
        internal ClassInterfaceType _val;
        public ClassInterfaceAttribute(ClassInterfaceType classInterfaceType)
        {
            _val = classInterfaceType;
        }
        public ClassInterfaceAttribute(short classInterfaceType)
        {
            _val = (ClassInterfaceType)classInterfaceType;
        }
        public ClassInterfaceType Value { get { return _val; } }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class LCIDConversionAttribute : Attribute
    {
        internal int _val;
        public LCIDConversionAttribute(int lcid)
        {
            _val = lcid;
        }
        public int Value { get { return _val; } }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ProgIdAttribute : Attribute
    {
        internal String _val;
        public ProgIdAttribute(String progId)
        {
            _val = progId;
        }
        public String Value { get { return _val; } }
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class ComSourceInterfacesAttribute : Attribute
    {
        internal String _val;
        public ComSourceInterfacesAttribute(String sourceInterfaces)
        {
            _val = sourceInterfaces;
        }
        public ComSourceInterfacesAttribute(Type sourceInterface)
        {
            _val = sourceInterface.FullName;
        }
        public ComSourceInterfacesAttribute(Type sourceInterface1, Type sourceInterface2)
        {
            _val = sourceInterface1.FullName + "\0" + sourceInterface2.FullName;
        }
        public ComSourceInterfacesAttribute(Type sourceInterface1, Type sourceInterface2, Type sourceInterface3)
        {
            _val = sourceInterface1.FullName + "\0" + sourceInterface2.FullName + "\0" + sourceInterface3.FullName;
        }
        public ComSourceInterfacesAttribute(Type sourceInterface1, Type sourceInterface2, Type sourceInterface3, Type sourceInterface4)
        {
            _val = sourceInterface1.FullName + "\0" + sourceInterface2.FullName + "\0" + sourceInterface3.FullName + "\0" + sourceInterface4.FullName;
        }
        public String Value { get { return _val; } }
    }

    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.ReturnValue, Inherited = false)]
    public unsafe sealed class MarshalAsAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeParameterInfo parameter)
        {
            return GetCustomAttribute(parameter.MetadataToken, parameter.GetRuntimeModule());
        }

        internal static bool IsDefined(RuntimeParameterInfo parameter)
        {
            return GetCustomAttribute(parameter) != null;
        }

        internal static Attribute GetCustomAttribute(RuntimeFieldInfo field)
        {
            return GetCustomAttribute(field.MetadataToken, field.GetRuntimeModule()); ;
        }

        internal static bool IsDefined(RuntimeFieldInfo field)
        {
            return GetCustomAttribute(field) != null;
        }

        internal static Attribute GetCustomAttribute(int token, RuntimeModule scope)
        {
            UnmanagedType unmanagedType, arraySubType;
            VarEnum safeArraySubType;
            int sizeParamIndex = 0, sizeConst = 0;
            string marshalTypeName = null, marshalCookie = null, safeArrayUserDefinedTypeName = null;
            int iidParamIndex = 0;
            ConstArray nativeType = ModuleHandle.GetMetadataImport(scope.GetNativeHandle()).GetFieldMarshal(token);

            if (nativeType.Length == 0)
                return null;

            MetadataImport.GetMarshalAs(nativeType,
                out unmanagedType, out safeArraySubType, out safeArrayUserDefinedTypeName, out arraySubType, out sizeParamIndex,
                out sizeConst, out marshalTypeName, out marshalCookie, out iidParamIndex);

            RuntimeType safeArrayUserDefinedType = safeArrayUserDefinedTypeName == null || safeArrayUserDefinedTypeName.Length == 0 ? null :
                RuntimeTypeHandle.GetTypeByNameUsingCARules(safeArrayUserDefinedTypeName, scope);
            RuntimeType marshalTypeRef = null;

            try
            {
                marshalTypeRef = marshalTypeName == null ? null : RuntimeTypeHandle.GetTypeByNameUsingCARules(marshalTypeName, scope);
            }
            catch (System.TypeLoadException)
            {
                // The user may have supplied a bad type name string causing this TypeLoadException
                // Regardless, we return the bad type name
                Debug.Assert(marshalTypeName != null);
            }

            return new MarshalAsAttribute(
                unmanagedType, safeArraySubType, safeArrayUserDefinedType, arraySubType,
                (short)sizeParamIndex, sizeConst, marshalTypeName, marshalTypeRef, marshalCookie, iidParamIndex);
        }

        internal MarshalAsAttribute(UnmanagedType val, VarEnum safeArraySubType, RuntimeType safeArrayUserDefinedSubType, UnmanagedType arraySubType,
            short sizeParamIndex, int sizeConst, string marshalType, RuntimeType marshalTypeRef, string marshalCookie, int iidParamIndex)
        {
            _val = val;
            SafeArraySubType = safeArraySubType;
            SafeArrayUserDefinedSubType = safeArrayUserDefinedSubType;
            IidParameterIndex = iidParamIndex;
            ArraySubType = arraySubType;
            SizeParamIndex = sizeParamIndex;
            SizeConst = sizeConst;
            MarshalType = marshalType;
            MarshalTypeRef = marshalTypeRef;
            MarshalCookie = marshalCookie;
        }

        internal UnmanagedType _val;
        public MarshalAsAttribute(UnmanagedType unmanagedType)
        {
            _val = unmanagedType;
        }
        public MarshalAsAttribute(short unmanagedType)
        {
            _val = (UnmanagedType)unmanagedType;
        }
        public UnmanagedType Value { get { return _val; } }

        // Fields used with SubType = SafeArray.
        public VarEnum SafeArraySubType;
        public Type SafeArrayUserDefinedSubType;

        // Field used with iid_is attribute (interface pointers).
        public int IidParameterIndex;

        // Fields used with SubType = ByValArray and LPArray.
        // Array size =  parameter(PI) * PM + C
        public UnmanagedType ArraySubType;
        public short SizeParamIndex;           // param index PI
        public int SizeConst;                // constant C

        // Fields used with SubType = CustomMarshaler
        public String MarshalType;              // Name of marshaler class
        public Type MarshalTypeRef;           // Type of marshaler class
        public String MarshalCookie;            // cookie to pass to marshaler
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, Inherited = false)]
    public sealed class ComImportAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeType type)
        {
            if ((type.Attributes & TypeAttributes.Import) == 0)
                return null;

            return new ComImportAttribute();
        }

        internal static bool IsDefined(RuntimeType type)
        {
            return (type.Attributes & TypeAttributes.Import) != 0;
        }

        public ComImportAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Enum | AttributeTargets.Struct | AttributeTargets.Delegate, Inherited = false)]
    public sealed class GuidAttribute : Attribute
    {
        internal String _val;
        public GuidAttribute(String guid)
        {
            _val = guid;
        }
        public String Value { get { return _val; } }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public sealed class PreserveSigAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeMethodInfo method)
        {
            if ((method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) == 0)
                return null;

            return new PreserveSigAttribute();
        }

        internal static bool IsDefined(RuntimeMethodInfo method)
        {
            return (method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0;
        }

        public PreserveSigAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class InAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeParameterInfo parameter)
        {
            return parameter.IsIn ? new InAttribute() : null;
        }
        internal static bool IsDefined(RuntimeParameterInfo parameter)
        {
            return parameter.IsIn;
        }

        public InAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class OutAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeParameterInfo parameter)
        {
            return parameter.IsOut ? new OutAttribute() : null;
        }
        internal static bool IsDefined(RuntimeParameterInfo parameter)
        {
            return parameter.IsOut;
        }

        public OutAttribute()
        {
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public sealed class OptionalAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeParameterInfo parameter)
        {
            return parameter.IsOptional ? new OptionalAttribute() : null;
        }
        internal static bool IsDefined(RuntimeParameterInfo parameter)
        {
            return parameter.IsOptional;
        }

        public OptionalAttribute()
        {
        }
    }

    [Flags]
    public enum DllImportSearchPath
    {
        UseDllDirectoryForDependencies = 0x100,
        ApplicationDirectory = 0x200,
        UserDirectories = 0x400,
        System32 = 0x800,
        SafeDirectories = 0x1000,
        AssemblyDirectory = 0x2,
        LegacyBehavior = 0x0
    }

    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Method, AllowMultiple = false)]
    public sealed class DefaultDllImportSearchPathsAttribute : Attribute
    {
        internal DllImportSearchPath _paths;
        public DefaultDllImportSearchPathsAttribute(DllImportSearchPath paths)
        {
            _paths = paths;
        }

        public DllImportSearchPath Paths { get { return _paths; } }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public unsafe sealed class DllImportAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeMethodInfo method)
        {
            if ((method.Attributes & MethodAttributes.PinvokeImpl) == 0)
                return null;

            MetadataImport scope = ModuleHandle.GetMetadataImport(method.Module.ModuleHandle.GetRuntimeModule());
            string entryPoint, dllName = null;
            int token = method.MetadataToken;
            PInvokeAttributes flags = 0;

            scope.GetPInvokeMap(token, out flags, out entryPoint, out dllName);

            CharSet charSet = CharSet.None;

            switch (flags & PInvokeAttributes.CharSetMask)
            {
                case PInvokeAttributes.CharSetNotSpec: charSet = CharSet.None; break;
                case PInvokeAttributes.CharSetAnsi: charSet = CharSet.Ansi; break;
                case PInvokeAttributes.CharSetUnicode: charSet = CharSet.Unicode; break;
                case PInvokeAttributes.CharSetAuto: charSet = CharSet.Auto; break;

                // Invalid: default to CharSet.None
                default: break;
            }

            CallingConvention callingConvention = CallingConvention.Cdecl;

            switch (flags & PInvokeAttributes.CallConvMask)
            {
                case PInvokeAttributes.CallConvWinapi: callingConvention = CallingConvention.Winapi; break;
                case PInvokeAttributes.CallConvCdecl: callingConvention = CallingConvention.Cdecl; break;
                case PInvokeAttributes.CallConvStdcall: callingConvention = CallingConvention.StdCall; break;
                case PInvokeAttributes.CallConvThiscall: callingConvention = CallingConvention.ThisCall; break;
                case PInvokeAttributes.CallConvFastcall: callingConvention = CallingConvention.FastCall; break;

                // Invalid: default to CallingConvention.Cdecl
                default: break;
            }

            bool exactSpelling = (flags & PInvokeAttributes.NoMangle) != 0;
            bool setLastError = (flags & PInvokeAttributes.SupportsLastError) != 0;
            bool bestFitMapping = (flags & PInvokeAttributes.BestFitMask) == PInvokeAttributes.BestFitEnabled;
            bool throwOnUnmappableChar = (flags & PInvokeAttributes.ThrowOnUnmappableCharMask) == PInvokeAttributes.ThrowOnUnmappableCharEnabled;
            bool preserveSig = (method.GetMethodImplementationFlags() & MethodImplAttributes.PreserveSig) != 0;

            return new DllImportAttribute(
                dllName, entryPoint, charSet, exactSpelling, setLastError, preserveSig,
                callingConvention, bestFitMapping, throwOnUnmappableChar);
        }

        internal static bool IsDefined(RuntimeMethodInfo method)
        {
            return (method.Attributes & MethodAttributes.PinvokeImpl) != 0;
        }


        internal DllImportAttribute(
            string dllName, string entryPoint, CharSet charSet, bool exactSpelling, bool setLastError, bool preserveSig,
            CallingConvention callingConvention, bool bestFitMapping, bool throwOnUnmappableChar)
        {
            _val = dllName;
            EntryPoint = entryPoint;
            CharSet = charSet;
            ExactSpelling = exactSpelling;
            SetLastError = setLastError;
            PreserveSig = preserveSig;
            CallingConvention = callingConvention;
            BestFitMapping = bestFitMapping;
            ThrowOnUnmappableChar = throwOnUnmappableChar;
        }

        internal String _val;

        public DllImportAttribute(String dllName)
        {
            _val = dllName;
        }
        public String Value { get { return _val; } }

        public String EntryPoint;
        public CharSet CharSet;
        public bool SetLastError;
        public bool ExactSpelling;
        public bool PreserveSig;
        public CallingConvention CallingConvention;
        public bool BestFitMapping;
        public bool ThrowOnUnmappableChar;
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public unsafe sealed class StructLayoutAttribute : Attribute
    {
        private const int DEFAULT_PACKING_SIZE = 8;

        internal static Attribute GetCustomAttribute(RuntimeType type)
        {
            if (!IsDefined(type))
                return null;

            int pack = 0, size = 0;
            LayoutKind layoutKind = LayoutKind.Auto;
            switch (type.Attributes & TypeAttributes.LayoutMask)
            {
                case TypeAttributes.ExplicitLayout: layoutKind = LayoutKind.Explicit; break;
                case TypeAttributes.AutoLayout: layoutKind = LayoutKind.Auto; break;
                case TypeAttributes.SequentialLayout: layoutKind = LayoutKind.Sequential; break;
                default: Contract.Assume(false); break;
            }

            CharSet charSet = CharSet.None;
            switch (type.Attributes & TypeAttributes.StringFormatMask)
            {
                case TypeAttributes.AnsiClass: charSet = CharSet.Ansi; break;
                case TypeAttributes.AutoClass: charSet = CharSet.Auto; break;
                case TypeAttributes.UnicodeClass: charSet = CharSet.Unicode; break;
                default: Contract.Assume(false); break;
            }
            type.GetRuntimeModule().MetadataImport.GetClassLayout(type.MetadataToken, out pack, out size);

            // Metadata parameter checking should not have allowed 0 for packing size.
            // The runtime later converts a packing size of 0 to 8 so do the same here
            // because it's more useful from a user perspective. 
            if (pack == 0)
                pack = DEFAULT_PACKING_SIZE;

            return new StructLayoutAttribute(layoutKind, pack, size, charSet);
        }

        internal static bool IsDefined(RuntimeType type)
        {
            if (type.IsInterface || type.HasElementType || type.IsGenericParameter)
                return false;

            return true;
        }

        internal LayoutKind _val;

        internal StructLayoutAttribute(LayoutKind layoutKind, int pack, int size, CharSet charSet)
        {
            _val = layoutKind;
            Pack = pack;
            Size = size;
            CharSet = charSet;
        }

        public StructLayoutAttribute(LayoutKind layoutKind)
        {
            _val = layoutKind;
        }
        public StructLayoutAttribute(short layoutKind)
        {
            _val = (LayoutKind)layoutKind;
        }
        public LayoutKind Value { get { return _val; } }
        public int Pack;
        public int Size;
        public CharSet CharSet;
    }

    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public unsafe sealed class FieldOffsetAttribute : Attribute
    {
        internal static Attribute GetCustomAttribute(RuntimeFieldInfo field)
        {
            int fieldOffset;

            if (field.DeclaringType != null &&
                field.GetRuntimeModule().MetadataImport.GetFieldOffset(field.DeclaringType.MetadataToken, field.MetadataToken, out fieldOffset))
                return new FieldOffsetAttribute(fieldOffset);

            return null;
        }

        internal static bool IsDefined(RuntimeFieldInfo field)
        {
            return GetCustomAttribute(field) != null;
        }

        internal int _val;
        public FieldOffsetAttribute(int offset)
        {
            _val = offset;
        }
        public int Value { get { return _val; } }
    }

    [AttributeUsage(AttributeTargets.Interface, Inherited = false)]
    public sealed class CoClassAttribute : Attribute
    {
        internal Type _CoClass;

        public CoClassAttribute(Type coClass)
        {
            _CoClass = coClass;
        }

        public Type CoClass { get { return _CoClass; } }
    }

    [AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Interface | AttributeTargets.Class | AttributeTargets.Struct, Inherited = false)]
    public sealed class BestFitMappingAttribute : Attribute
    {
        internal bool _bestFitMapping;

        public BestFitMappingAttribute(bool BestFitMapping)
        {
            _bestFitMapping = BestFitMapping;
        }

        public bool BestFitMapping { get { return _bestFitMapping; } }
        public bool ThrowOnUnmappableChar;
    }

    [AttributeUsage(AttributeTargets.Module, Inherited = false)]
    public sealed class DefaultCharSetAttribute : Attribute
    {
        internal CharSet _CharSet;

        public DefaultCharSetAttribute(CharSet charSet)
        {
            _CharSet = charSet;
        }

        public CharSet CharSet { get { return _CharSet; } }
    }
}

