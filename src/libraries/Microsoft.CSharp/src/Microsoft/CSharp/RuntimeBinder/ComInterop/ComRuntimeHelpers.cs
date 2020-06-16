// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;
using System.Security;
using ComTypes = System.Runtime.InteropServices.ComTypes;

namespace Microsoft.CSharp.RuntimeBinder.ComInterop
{
    internal static class ComRuntimeHelpers
    {
        public static void CheckThrowException(int hresult, ref ExcepInfo excepInfo, uint argErr, string message)
        {
            if (ComHresults.IsSuccess(hresult))
            {
                return;
            }

            switch (hresult)
            {
                case ComHresults.DISP_E_BADPARAMCOUNT:
                    // The number of elements provided to DISPPARAMS is different from the number of arguments
                    // accepted by the method or property.
                    throw Error.DispBadParamCount(message);

                case ComHresults.DISP_E_BADVARTYPE:
                    //One of the arguments in rgvarg is not a valid variant type.
                    break;

                case ComHresults.DISP_E_EXCEPTION:
                    // The application needs to raise an exception. In this case, the structure passed in pExcepInfo
                    // should be filled in.
                    throw excepInfo.GetException();

                case ComHresults.DISP_E_MEMBERNOTFOUND:
                    // The requested member does not exist, or the call to Invoke tried to set the value of a
                    // read-only property.
                    throw Error.DispMemberNotFound(message);

                case ComHresults.DISP_E_NONAMEDARGS:
                    // This implementation of IDispatch does not support named arguments.
                    throw Error.DispNoNamedArgs(message);

                case ComHresults.DISP_E_OVERFLOW:
                    // One of the arguments in rgvarg could not be coerced to the specified type.
                    throw Error.DispOverflow(message);

                case ComHresults.DISP_E_PARAMNOTFOUND:
                    // One of the parameter DISPIDs does not correspond to a parameter on the method. In this case,
                    // puArgErr should be set to the first argument that contains the error.
                    break;

                case ComHresults.DISP_E_TYPEMISMATCH:
                    // One or more of the arguments could not be coerced. The index within rgvarg of the first
                    // parameter with the incorrect type is returned in the puArgErr parameter.
                    throw Error.DispTypeMismatch(argErr, message);

                case ComHresults.DISP_E_UNKNOWNINTERFACE:
                    // The interface identifier passed in riid is not IID_NULL.
                    break;

                case ComHresults.DISP_E_UNKNOWNLCID:
                    // The member being invoked interprets string arguments according to the LCID, and the
                    // LCID is not recognized.
                    break;

                case ComHresults.DISP_E_PARAMNOTOPTIONAL:
                    // A required parameter was omitted.
                    throw Error.DispParamNotOptional(message);
            }

            Marshal.ThrowExceptionForHR(hresult);
        }

        internal static void GetInfoFromType(ComTypes.ITypeInfo typeInfo, out string name, out string documentation)
        {
            typeInfo.GetDocumentation(-1, out name, out documentation, out int _, out string _);
        }

        internal static string GetNameOfMethod(ComTypes.ITypeInfo typeInfo, int memid)
        {
            string[] rgNames = new string[1];
            typeInfo.GetNames(memid, rgNames, 1, out int _);
            return rgNames[0];
        }

        internal static string GetNameOfLib(ComTypes.ITypeLib typeLib)
        {
            typeLib.GetDocumentation(-1, out string name, out string _, out int _, out string _);
            return name;
        }

        internal static string GetNameOfType(ComTypes.ITypeInfo typeInfo)
        {
            GetInfoFromType(typeInfo, out string name, out string _);

            return name;
        }

        /// <summary>
        /// Look for typeinfo using IDispatch.GetTypeInfo
        /// </summary>
        /// <param name="dispatch">IDispatch object</param>
        /// <remarks>
        /// Some COM objects just dont expose typeinfo. In these cases, this method will return null.
        /// Some COM objects do intend to expose typeinfo, but may not be able to do so if the type-library is not properly
        /// registered. This will be considered as acceptable or as an error condition depending on throwIfMissingExpectedTypeInfo
        /// </remarks>
        /// <returns>Type info</returns>
        internal static ComTypes.ITypeInfo GetITypeInfoFromIDispatch(IDispatch dispatch)
        {
            int hresult = dispatch.TryGetTypeInfoCount(out uint typeCount);
            Marshal.ThrowExceptionForHR(hresult);
            Debug.Assert(typeCount <= 1);
            if (typeCount == 0)
            {
                return null;
            }

            IntPtr typeInfoPtr;
            hresult = dispatch.TryGetTypeInfo(0, 0, out typeInfoPtr);
            if (!ComHresults.IsSuccess(hresult))
            {
                // Word.Basic always returns this because of an incorrect implementation of IDispatch.GetTypeInfo
                // Any implementation that returns E_NOINTERFACE is likely to do so in all environments
                if (hresult == ComHresults.E_NOINTERFACE)
                {
                    return null;
                }

                // This assert is potentially over-restrictive since COM components can behave in quite unexpected ways.
                // However, asserting the common expected cases ensures that we find out about the unexpected scenarios, and
                // can investigate the scenarios to ensure that there is no bug in our own code.
                Debug.Assert(hresult == ComHresults.TYPE_E_LIBNOTREGISTERED);

                Marshal.ThrowExceptionForHR(hresult);
            }

            if (typeInfoPtr == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(ComHresults.E_FAIL);
            }

            ComTypes.ITypeInfo typeInfo = null;
            try
            {
                typeInfo = Marshal.GetObjectForIUnknown(typeInfoPtr) as ComTypes.ITypeInfo;
            }
            finally
            {
                Marshal.Release(typeInfoPtr);
            }

            return typeInfo;
        }

        internal static ComTypes.TYPEATTR GetTypeAttrForTypeInfo(ComTypes.ITypeInfo typeInfo)
        {
            IntPtr pAttrs;
            typeInfo.GetTypeAttr(out pAttrs);

            // GetTypeAttr should never return null, this is just to be safe
            if (pAttrs == IntPtr.Zero)
            {
                throw Error.CannotRetrieveTypeInformation();
            }

            try
            {
                return (ComTypes.TYPEATTR)Marshal.PtrToStructure(pAttrs, typeof(ComTypes.TYPEATTR));
            }
            finally
            {
                typeInfo.ReleaseTypeAttr(pAttrs);
            }
        }

        internal static ComTypes.TYPELIBATTR GetTypeAttrForTypeLib(ComTypes.ITypeLib typeLib)
        {
            IntPtr pAttrs;
            typeLib.GetLibAttr(out pAttrs);

            // GetTypeAttr should never return null, this is just to be safe
            if (pAttrs == IntPtr.Zero)
            {
                throw Error.CannotRetrieveTypeInformation();
            }

            try
            {
                return (ComTypes.TYPELIBATTR)Marshal.PtrToStructure(pAttrs, typeof(ComTypes.TYPELIBATTR));
            }
            finally
            {
                typeLib.ReleaseTLibAttr(pAttrs);
            }
        }

        public static BoundDispEvent CreateComEvent(object rcw, Guid sourceIid, int dispid)
        {
            return new BoundDispEvent(rcw, sourceIid, dispid);
        }

        public static DispCallable CreateDispCallable(IDispatchComObject dispatch, ComMethodDesc method)
        {
            return new DispCallable(dispatch, method.Name, method.DispId);
        }
    }

    /// <summary>
    /// This class contains methods that either cannot be expressed in C#, or which require writing unsafe code.
    /// Callers of these methods need to use them extremely carefully as incorrect use could cause GC-holes
    /// and other problems.
    /// </summary>
    ///
    internal static class UnsafeMethods
    {
        #region public members

        public static unsafe IntPtr ConvertInt32ByrefToPtr(ref int value) { return (IntPtr)System.Runtime.CompilerServices.Unsafe.AsPointer(ref value); }
        public static unsafe IntPtr ConvertVariantByrefToPtr(ref Variant value) { return (IntPtr)System.Runtime.CompilerServices.Unsafe.AsPointer(ref value); }

        internal static Variant GetVariantForObject(object obj)
        {
            Variant variant = default;
            if (obj == null)
            {
                return variant;
            }
            InitVariantForObject(obj, ref variant);
            return variant;
        }

        internal static void InitVariantForObject(object obj, ref Variant variant)
        {
            Debug.Assert(obj != null);

            // GetNativeVariantForObject is very expensive for values that marshal as VT_DISPATCH
            // also is is extremely common scenario when object at hand is an RCW.
            // Therefore we are going to test for IDispatch before defaulting to GetNativeVariantForObject.
            if (obj is IDispatch)
            {
                variant.AsDispatch = obj;
                return;
            }

            Marshal.GetNativeVariantForObject(obj, ConvertVariantByrefToPtr(ref variant));
        }

        // This method is intended for use through reflection and should not be used directly
        public static object GetObjectForVariant(Variant variant)
        {
            IntPtr ptr = UnsafeMethods.ConvertVariantByrefToPtr(ref variant);
            return Marshal.GetObjectForNativeVariant(ptr);
        }

        // This method is intended for use through reflection and should only be used directly by IUnknownReleaseNotZero
        public static int IUnknownRelease(IntPtr interfacePointer)
        {
            return s_iUnknownRelease(interfacePointer);
        }

        // This method is intended for use through reflection and should not be used directly
        public static void IUnknownReleaseNotZero(IntPtr interfacePointer)
        {
            if (interfacePointer != IntPtr.Zero)
            {
                IUnknownRelease(interfacePointer);
            }
        }

        // This method is intended for use through reflection and should not be used directly
        public static int IDispatchInvoke(
            IntPtr dispatchPointer,
            int memberDispId,
            ComTypes.INVOKEKIND flags,
            ref ComTypes.DISPPARAMS dispParams,
            out Variant result,
            out ExcepInfo excepInfo,
            out uint argErr)
        {
            int hresult = s_iDispatchInvoke(
                dispatchPointer,
                memberDispId,
                flags,
                ref dispParams,
                out result,
                out excepInfo,
                out argErr
            );

            if (hresult == ComHresults.DISP_E_MEMBERNOTFOUND
                && (flags & ComTypes.INVOKEKIND.INVOKE_FUNC) != 0
                && (flags & (ComTypes.INVOKEKIND.INVOKE_PROPERTYPUT | ComTypes.INVOKEKIND.INVOKE_PROPERTYPUTREF)) == 0)
            {
                // Re-invoke with no result argument to accomodate Word
                hresult = _IDispatchInvokeNoResult(
                    dispatchPointer,
                    memberDispId,
                    ComTypes.INVOKEKIND.INVOKE_FUNC,
                    ref dispParams,
                    out result,
                    out excepInfo,
                    out argErr);
            }
            return hresult;
        }

        // This method is intended for use through reflection and should not be used directly
        public static IntPtr GetIdsOfNamedParameters(IDispatch dispatch, string[] names, int methodDispId, out GCHandle pinningHandle)
        {
            pinningHandle = GCHandle.Alloc(null, GCHandleType.Pinned);
            int[] dispIds = new int[names.Length];
            Guid empty = Guid.Empty;
            int hresult = dispatch.TryGetIDsOfNames(ref empty, names, (uint)names.Length, 0, dispIds);
            if (hresult < 0)
            {
                Marshal.ThrowExceptionForHR(hresult);
            }

            if (methodDispId != dispIds[0])
            {
                throw Error.GetIDsOfNamesInvalid(names[0]);
            }

            int[] keywordArgDispIds = dispIds.RemoveFirst(); // Remove the dispId of the method name

            pinningHandle.Target = keywordArgDispIds;
            return Marshal.UnsafeAddrOfPinnedArrayElement(keywordArgDispIds, 0);
        }

        #endregion

        #region non-public members

        private static void EmitLoadArg(ILGenerator il, int index)
        {
            Requires.Condition(index >= 0, nameof(index));
            il.Emit(OpCodes.Ldarg, index);
        }

        private static readonly object s_lock = new object();
        private static ModuleBuilder s_dynamicModule;

        internal static ModuleBuilder DynamicModule
        {
            get
            {
                if (s_dynamicModule != null)
                {
                    return s_dynamicModule;
                }
                lock (s_lock)
                {
                    if (s_dynamicModule == null)
                    {
                        string name = typeof(VariantArray).Namespace + ".DynamicAssembly";
                        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName(name), AssemblyBuilderAccess.Run);
                        s_dynamicModule = assembly.DefineDynamicModule(name);
                    }
                    return s_dynamicModule;
                }
            }
        }

        /// <summary>
        /// We will emit an indirect call to an unmanaged function pointer from the vtable of the given interface pointer.
        /// This approach can take only ~300 instructions on x86 compared with ~900 for Marshal.Release. We are relying on
        /// the JIT-compiler to do pinvoke-stub-inlining and calling the pinvoke target directly.
        /// </summary>
        private delegate int IUnknownReleaseDelegate(IntPtr interfacePointer);
        private static readonly IUnknownReleaseDelegate s_iUnknownRelease = Create_IUnknownRelease();

        private static IUnknownReleaseDelegate Create_IUnknownRelease()
        {
            DynamicMethod dm = new DynamicMethod("IUnknownRelease", typeof(int), new Type[] { typeof(IntPtr) }, DynamicModule);

            ILGenerator method = dm.GetILGenerator();

            // return functionPtr(...)

            method.Emit(OpCodes.Ldarg_0);

            // functionPtr = *(IntPtr*)(*(interfacePointer) + VTABLE_OFFSET)
            int iunknownReleaseOffset = ((int)IDispatchMethodIndices.IUnknown_Release) * Marshal.SizeOf(typeof(IntPtr));
            method.Emit(OpCodes.Ldarg_0);
            method.Emit(OpCodes.Ldind_I);
            method.Emit(OpCodes.Ldc_I4, iunknownReleaseOffset);
            method.Emit(OpCodes.Add);
            method.Emit(OpCodes.Ldind_I);

            method.EmitCalli(OpCodes.Calli, CallingConvention.Winapi, typeof(int), new[] { typeof(IntPtr) });

            method.Emit(OpCodes.Ret);

            return dm.CreateDelegate<IUnknownReleaseDelegate>();
        }

        internal static readonly IntPtr s_nullInterfaceId = GetNullInterfaceId();

        private static IntPtr GetNullInterfaceId()
        {
            int size = Marshal.SizeOf(Guid.Empty);
            IntPtr ptr = Marshal.AllocHGlobal(size);
            for (int i = 0; i < size; i++)
            {
                Marshal.WriteByte(ptr, i, 0);
            }
            return ptr;
        }

        /// <summary>
        /// We will emit an indirect call to an unmanaged function pointer from the vtable of the given IDispatch interface pointer.
        /// It is not possible to express this in C#. Using an indirect pinvoke call allows us to do our own marshalling.
        /// We can allocate the Variant arguments cheaply on the stack. We are relying on the JIT-compiler to do
        /// pinvoke-stub-inlining and calling the pinvoke target directly.
        /// The alternative of calling via a managed interface declaration of IDispatch would have a performance
        /// penalty of going through a CLR stub that would have to re-push the arguments on the stack, etc.
        /// Marshal.GetDelegateForFunctionPointer could be used here, but its too expensive (~2000 instructions on x86).
        /// </summary>
        private delegate int IDispatchInvokeDelegate(
            IntPtr dispatchPointer,
            int memberDispId,
            ComTypes.INVOKEKIND flags,
            ref ComTypes.DISPPARAMS dispParams,
            out Variant result,
            out ExcepInfo excepInfo,
            out uint argErr
        );

        private static readonly IDispatchInvokeDelegate s_iDispatchInvoke = Create_IDispatchInvoke(true);
        private static IDispatchInvokeDelegate s_iDispatchInvokeNoResultImpl;

        private static IDispatchInvokeDelegate _IDispatchInvokeNoResult
        {
            get
            {
                if (s_iDispatchInvokeNoResultImpl == null)
                {
                    lock (s_iDispatchInvoke)
                    {
                        if (s_iDispatchInvokeNoResultImpl == null)
                        {
                            s_iDispatchInvokeNoResultImpl = Create_IDispatchInvoke(false);
                        }
                    }
                }
                return s_iDispatchInvokeNoResultImpl;
            }
        }

        private static IDispatchInvokeDelegate Create_IDispatchInvoke(bool returnResult)
        {
            const int dispatchPointerIndex = 0;
            const int memberDispIdIndex = 1;
            const int flagsIndex = 2;
            const int dispParamsIndex = 3;
            const int resultIndex = 4;
            const int exceptInfoIndex = 5;
            const int argErrIndex = 6;
            Debug.Assert(argErrIndex + 1 == typeof(IDispatchInvokeDelegate).GetMethod(nameof(IDispatchInvokeDelegate.Invoke)).GetParameters().Length);

            Type[] paramTypes = new Type[argErrIndex + 1];
            paramTypes[dispatchPointerIndex] = typeof(IntPtr);
            paramTypes[memberDispIdIndex] = typeof(int);
            paramTypes[flagsIndex] = typeof(ComTypes.INVOKEKIND);
            paramTypes[dispParamsIndex] = typeof(ComTypes.DISPPARAMS).MakeByRefType();
            paramTypes[resultIndex] = typeof(Variant).MakeByRefType();
            paramTypes[exceptInfoIndex] = typeof(ExcepInfo).MakeByRefType();
            paramTypes[argErrIndex] = typeof(uint).MakeByRefType();

            // Define the dynamic method in our assembly so we skip verification
            DynamicMethod dm = new DynamicMethod("IDispatchInvoke", typeof(int), paramTypes, DynamicModule);
            ILGenerator method = dm.GetILGenerator();

            // return functionPtr(...)

            EmitLoadArg(method, dispatchPointerIndex);
            EmitLoadArg(method, memberDispIdIndex);

            // burn the address of our empty IID in directly.  This is never freed, relocated, etc...
            // Note passing this as a Guid directly results in a ~30% perf hit for IDispatch invokes so
            // we also pass it directly as an IntPtr instead.
            if (IntPtr.Size == 4)
            {
                method.Emit(OpCodes.Ldc_I4, UnsafeMethods.s_nullInterfaceId.ToInt32()); // riid
            }
            else
            {
                method.Emit(OpCodes.Ldc_I8, UnsafeMethods.s_nullInterfaceId.ToInt64()); // riid
            }
            method.Emit(OpCodes.Conv_I);

            method.Emit(OpCodes.Ldc_I4_0); // lcid
            EmitLoadArg(method, flagsIndex);

            EmitLoadArg(method, dispParamsIndex);
            method.Emit(OpCodes.Conv_I);

            if (returnResult)
            {
                EmitLoadArg(method, resultIndex);
                method.Emit(OpCodes.Conv_I);
            }
            else
            {
                method.Emit(OpCodes.Ldsfld, typeof(IntPtr).GetField(nameof(IntPtr.Zero)));
            }
            EmitLoadArg(method, exceptInfoIndex);
            method.Emit(OpCodes.Conv_I);
            EmitLoadArg(method, argErrIndex);
            method.Emit(OpCodes.Conv_I);

            // functionPtr = *(IntPtr*)(*(dispatchPointer) + VTABLE_OFFSET)
            int idispatchInvokeOffset = ((int)IDispatchMethodIndices.IDispatch_Invoke) * Marshal.SizeOf(typeof(IntPtr));
            EmitLoadArg(method, dispatchPointerIndex);
            method.Emit(OpCodes.Ldind_I);
            method.Emit(OpCodes.Ldc_I4, idispatchInvokeOffset);
            method.Emit(OpCodes.Add);
            method.Emit(OpCodes.Ldind_I);

            Type[] invokeParamTypes = new Type[] {
                    typeof(IntPtr), // dispatchPointer
                    typeof(int),    // memberDispId
                    typeof(IntPtr), // riid
                    typeof(int),    // lcid
                    typeof(ushort), // flags
                    typeof(IntPtr), // dispParams
                    typeof(IntPtr), // result
                    typeof(IntPtr), // excepInfo
                    typeof(IntPtr), // argErr
                };
            method.EmitCalli(OpCodes.Calli, CallingConvention.Winapi, typeof(int), invokeParamTypes);

            method.Emit(OpCodes.Ret);
            return dm.CreateDelegate<IDispatchInvokeDelegate>();
        }

        #endregion
    }
}
