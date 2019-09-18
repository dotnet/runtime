// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices
{
	partial class Marshal
	{
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static IntPtr AllocCoTaskMem (int cb);
		
		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static IntPtr AllocHGlobal (IntPtr cb);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static void FreeBSTR (IntPtr ptr);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static void FreeCoTaskMem (IntPtr ptr);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static void FreeHGlobal (IntPtr hglobal);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public static extern int GetLastWin32Error ();

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static void DestroyStructure (IntPtr ptr, Type structuretype);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static IntPtr OffsetOf (Type t, string fieldName);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static string PtrToStringBSTR (IntPtr ptr);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static IntPtr ReAllocCoTaskMem (IntPtr pv, int cb);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		public extern static IntPtr ReAllocHGlobal (IntPtr pv, IntPtr cb);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		public extern static void StructureToPtr (object structure, IntPtr ptr, bool fDeleteOld);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		internal extern static int GetArrayElementSize (Type type);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern unsafe static IntPtr BufferToBSTR (char* ptr, int slen);

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static bool IsPinnableType (Type type);

		internal static bool IsPinnable (object obj)
		{
			if (obj == null || obj is string)
				return true;
			return IsPinnableType (obj.GetType ());
			//Type type = obj.GetType ();
			//return !type.IsValueType || RuntimeTypeHandle.HasReferences (type as RuntimeType);
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		internal static extern void SetLastWin32Error (int error);

		static Exception GetExceptionForHRInternal (int errorCode, IntPtr errorInfo)
		{
			switch (errorCode) {
			case HResults.S_OK:
			case HResults.S_FALSE:
				return null;
			case HResults.COR_E_AMBIGUOUSMATCH:
				return new System.Reflection.AmbiguousMatchException ();
			case HResults.COR_E_APPLICATION:
				return new System.ApplicationException ();
			case HResults.COR_E_ARGUMENT:
				return new System.ArgumentException ();
			case HResults.COR_E_ARGUMENTOUTOFRANGE:
				return new System.ArgumentOutOfRangeException ();
			case HResults.COR_E_ARITHMETIC:
				return new System.ArithmeticException ();
			case HResults.COR_E_ARRAYTYPEMISMATCH:
				return new System.ArrayTypeMismatchException ();
			case HResults.COR_E_BADEXEFORMAT:
				return new System.BadImageFormatException ();
			case HResults.COR_E_BADIMAGEFORMAT:
				return new System.BadImageFormatException ();
				//case HResults.COR_E_CODECONTRACTFAILED:
				//return new System.Diagnostics.Contracts.ContractException ();
				//case HResults.COR_E_COMEMULATE:
			case HResults.COR_E_CUSTOMATTRIBUTEFORMAT:
				return new System.Reflection.CustomAttributeFormatException ();
			case HResults.COR_E_DATAMISALIGNED:
				return new System.DataMisalignedException ();
			case HResults.COR_E_DIRECTORYNOTFOUND:
				return new System.IO.DirectoryNotFoundException ();
			case HResults.COR_E_DIVIDEBYZERO:
				return new System.DivideByZeroException ();
			case HResults.COR_E_DLLNOTFOUND:
				return new System.DllNotFoundException ();
			case HResults.COR_E_DUPLICATEWAITOBJECT:
				return new System.DuplicateWaitObjectException ();
			case HResults.COR_E_ENDOFSTREAM:
				return new System.IO.EndOfStreamException ();
			case HResults.COR_E_ENTRYPOINTNOTFOUND:
				return new System.EntryPointNotFoundException ();
			case HResults.COR_E_EXCEPTION:
				return new System.Exception ();
			case HResults.COR_E_EXECUTIONENGINE:
				return new System.ExecutionEngineException ();
			case HResults.COR_E_FIELDACCESS:
				return new System.FieldAccessException ();
			case HResults.COR_E_FILELOAD:
				return new System.IO.FileLoadException ();
			case HResults.COR_E_FILENOTFOUND:
				return new System.IO.FileNotFoundException ();
			case HResults.COR_E_FORMAT:
				return new System.FormatException ();
			case HResults.COR_E_INDEXOUTOFRANGE:
				return new System.IndexOutOfRangeException ();
			case HResults.COR_E_INSUFFICIENTEXECUTIONSTACK:
				return new System.InsufficientExecutionStackException ();
			case HResults.COR_E_INVALIDCAST:
				return new System.InvalidCastException ();
			case HResults.COR_E_INVALIDFILTERCRITERIA:
				return new System.Reflection.InvalidFilterCriteriaException ();
			case HResults.COR_E_INVALIDOLEVARIANTTYPE:
				return new System.Runtime.InteropServices.InvalidOleVariantTypeException ();
			case HResults.COR_E_INVALIDOPERATION:
				return new System.InvalidOperationException ();
			case HResults.COR_E_INVALIDPROGRAM:
				return new System.InvalidProgramException ();
			case HResults.COR_E_IO:
				return new System.IO.IOException ();
			case HResults.COR_E_MARSHALDIRECTIVE:
				return new System.Runtime.InteropServices.MarshalDirectiveException ();
			case HResults.COR_E_MEMBERACCESS:
				return new System.MemberAccessException ();
			case HResults.COR_E_METHODACCESS:
				return new System.MethodAccessException ();
			case HResults.COR_E_MISSINGFIELD:
				return new System.MissingFieldException ();
			case HResults.COR_E_MISSINGMANIFESTRESOURCE:
				return new System.Resources.MissingManifestResourceException ();
			case HResults.COR_E_MISSINGMEMBER:
				return new System.MissingMemberException ();
			case HResults.COR_E_MISSINGMETHOD:
				return new System.MissingMethodException ();
			case HResults.COR_E_MULTICASTNOTSUPPORTED:
				return new System.MulticastNotSupportedException ();
			case HResults.COR_E_NOTFINITENUMBER:
				return new System.NotFiniteNumberException ();
			case HResults.COR_E_NOTSUPPORTED:
				return new System.NotSupportedException ();
			case HResults.COR_E_NULLREFERENCE:
				return new System.NullReferenceException ();
			case HResults.COR_E_OBJECTDISPOSED:
				return new System.ObjectDisposedException ("");
			case HResults.COR_E_OPERATIONCANCELED:
				return new System.OperationCanceledException ();
			case HResults.COR_E_OUTOFMEMORY:
				return new System.OutOfMemoryException ();
			case HResults.COR_E_OVERFLOW:
				return new System.OverflowException ();
			case HResults.COR_E_PATHTOOLONG:
				return new System.IO.PathTooLongException ();
			case HResults.COR_E_PLATFORMNOTSUPPORTED:
				return new System.PlatformNotSupportedException ();
			case HResults.COR_E_RANK:
				return new System.RankException ();
			case HResults.COR_E_REFLECTIONTYPELOAD:
				return new System.MissingMethodException ();
			case HResults.COR_E_RUNTIMEWRAPPED:
				return new System.MissingMethodException ();
			case HResults.COR_E_SECURITY:
				return new System.Security.SecurityException ();
			case HResults.COR_E_SERIALIZATION:
				return new System.Runtime.Serialization.SerializationException ();
			case HResults.COR_E_STACKOVERFLOW:
				return new System.StackOverflowException ();
			case HResults.COR_E_SYNCHRONIZATIONLOCK:
				return new System.Threading.SynchronizationLockException ();
			case HResults.COR_E_SYSTEM:
				return new System.SystemException ();
			case HResults.COR_E_TARGET:
				return new System.Reflection.TargetException ();
			case HResults.COR_E_TARGETINVOCATION:
				return new System.MissingMethodException ();
			case HResults.COR_E_TARGETPARAMCOUNT:
				return new System.Reflection.TargetParameterCountException ();
			case HResults.COR_E_THREADABORTED:
				return new System.Threading.ThreadAbortException ();
			case HResults.COR_E_THREADINTERRUPTED:
				return new System.Threading.ThreadInterruptedException ();
			case HResults.COR_E_THREADSTART:
				return new System.Threading.ThreadStartException ();
			case HResults.COR_E_THREADSTATE:
				return new System.Threading.ThreadStateException ();
			case HResults.COR_E_TYPEACCESS:
				return new System.TypeAccessException ();
			case HResults.COR_E_TYPEINITIALIZATION:
				return new System.TypeInitializationException ("");
			case HResults.COR_E_TYPELOAD:
				return new System.TypeLoadException ();
			case HResults.COR_E_TYPEUNLOADED:
				return new System.TypeUnloadedException ();
			case HResults.COR_E_UNAUTHORIZEDACCESS:
				return new System.UnauthorizedAccessException ();
				//case HResults.COR_E_UNSUPPORTEDFORMAT:
			case HResults.COR_E_VERIFICATION:
				return new System.Security.VerificationException ();
				//case HResults.E_INVALIDARG:
			case HResults.E_NOTIMPL:
				return new System.NotImplementedException ();
				//case HResults.E_POINTER:
			case HResults.RO_E_CLOSED:
				return new System.ObjectDisposedException ("");
			case HResults.COR_E_ABANDONEDMUTEX:
			case HResults.COR_E_AMBIGUOUSIMPLEMENTATION:
			case HResults.COR_E_CANNOTUNLOADAPPDOMAIN:
			case HResults.COR_E_CONTEXTMARSHAL:
			case HResults.COR_E_HOSTPROTECTION:
			case HResults.COR_E_INSUFFICIENTMEMORY:
			case HResults.COR_E_INVALIDCOMOBJECT:
			case HResults.COR_E_KEYNOTFOUND:
			case HResults.COR_E_MISSINGSATELLITEASSEMBLY:
			case HResults.COR_E_SAFEARRAYRANKMISMATCH:
			case HResults.COR_E_SAFEARRAYTYPEMISMATCH:
			case HResults.COR_E_SAFEHANDLEMISSINGATTRIBUTE:
			case HResults.COR_E_SEMAPHOREFULL:
			case HResults.COR_E_THREADSTOP:
			case HResults.COR_E_TIMEOUT:
			case HResults.COR_E_WAITHANDLECANNOTBEOPENED:
			case HResults.DISP_E_OVERFLOW:
			case HResults.E_BOUNDS:
			case HResults.E_CHANGED_STATE:
			case HResults.E_FAIL:
			case HResults.E_HANDLE:
			case HResults.ERROR_MRM_MAP_NOT_FOUND:
			case HResults.TYPE_E_TYPEMISMATCH:
			case HResults.CO_E_NOTINITIALIZED:
			case HResults.RPC_E_CHANGED_MODE:
				return new COMException ("", errorCode);

			case HResults.STG_E_PATHNOTFOUND:
			case HResults.CTL_E_PATHNOTFOUND: {
				return new System.IO.DirectoryNotFoundException {
					HResult = errorCode
				};
			}
			case HResults.FUSION_E_INVALID_PRIVATE_ASM_LOCATION:
			case HResults.FUSION_E_SIGNATURE_CHECK_FAILED:
			case HResults.FUSION_E_LOADFROM_BLOCKED:
			case HResults.FUSION_E_CACHEFILE_FAILED:
			case HResults.FUSION_E_ASM_MODULE_MISSING:
			case HResults.FUSION_E_INVALID_NAME:
			case HResults.FUSION_E_PRIVATE_ASM_DISALLOWED:
			case HResults.FUSION_E_HOST_GAC_ASM_MISMATCH:
			case HResults.COR_E_MODULE_HASH_CHECK_FAILED:
			case HResults.FUSION_E_REF_DEF_MISMATCH:
			case HResults.SECURITY_E_INCOMPATIBLE_SHARE:
			case HResults.SECURITY_E_INCOMPATIBLE_EVIDENCE:
			case HResults.SECURITY_E_UNVERIFIABLE:
			case HResults.COR_E_FIXUPSINEXE:
			case HResults.ERROR_TOO_MANY_OPEN_FILES:
			case HResults.ERROR_SHARING_VIOLATION:
			case HResults.ERROR_LOCK_VIOLATION:
			case HResults.ERROR_OPEN_FAILED:
			case HResults.ERROR_DISK_CORRUPT:
			case HResults.ERROR_UNRECOGNIZED_VOLUME:
			case HResults.ERROR_DLL_INIT_FAILED:
			case HResults.FUSION_E_CODE_DOWNLOAD_DISABLED:
			case HResults.CORSEC_E_MISSING_STRONGNAME:
			case HResults.MSEE_E_ASSEMBLYLOADINPROGRESS:
			case HResults.ERROR_FILE_INVALID: {
				return new System.IO.FileLoadException {
					HResult = errorCode
				};
			}
			case HResults.CTL_E_FILENOTFOUND: {
				return new System.IO.FileNotFoundException {
					HResult = errorCode
				};
			}
			default:
				return new COMException ("", errorCode);
			}
		}

		static void PrelinkCore (MethodInfo m)
		{
			if (!(m is RuntimeMethodInfo))
			{
				throw new ArgumentException (SR.Argument_MustBeRuntimeMethodInfo, nameof(m));
			}

			PrelinkInternal (m);
		}

		[MethodImplAttribute(MethodImplOptions.InternalCall)]
		extern static void PtrToStructureInternal (IntPtr ptr, object structure, bool allowValueClasses);

		static void PtrToStructureHelper (IntPtr ptr, object structure, bool allowValueClasses)
		{
			if (structure == null)
				throw new ArgumentNullException (nameof (structure));
			PtrToStructureInternal (ptr, structure, allowValueClasses);
		}

		static object PtrToStructureHelper (IntPtr ptr, Type structureType)
		{
			var obj = Activator.CreateInstance (structureType);
			PtrToStructureHelper (ptr, obj, true);
			return obj;
		}

		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern Delegate GetDelegateForFunctionPointerInternal (IntPtr ptr, Type t);

		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern IntPtr GetFunctionPointerForDelegateInternal (Delegate d);

		[MethodImplAttribute (MethodImplOptions.InternalCall)]
		extern static void PrelinkInternal (MethodInfo m);

		[MethodImpl (MethodImplOptions.InternalCall)]
		static extern int SizeOfHelper (Type t, bool throwIfNotMarshalable);

		public static IntPtr GetExceptionPointers ()
		{
			throw new PlatformNotSupportedException ();
		}

		internal unsafe static IntPtr AllocBSTR (int length)
		{
			var res = BufferToBSTR ((char*)IntPtr.Zero, length);
			if (res == IntPtr.Zero)
				throw new OutOfMemoryException ();
			return res;
		}

		public unsafe static IntPtr StringToBSTR (string s)
		{
			if (s == null)
				return IntPtr.Zero;
			fixed (char* fixed_s = s)
				return BufferToBSTR (fixed_s, s.Length);
		}

		#region PlatformNotSupported

		public static int GetExceptionCode()
		{
			// Obsolete
			throw new PlatformNotSupportedException ();
		}

		public static byte ReadByte(Object ptr, int ofs)
		{
			// Obsolete
			throw new PlatformNotSupportedException ();
		}

		public static short ReadInt16(Object ptr, int ofs)
		{
			// Obsolete
			throw new PlatformNotSupportedException ();
		}

		public static int ReadInt32(Object ptr, int ofs)
		{
			// Obsolete
			throw new PlatformNotSupportedException ();
		}

		public static long ReadInt64(Object ptr, int ofs)
		{
			// Obsolete
			throw new PlatformNotSupportedException ();
		}

		public static void WriteByte(Object ptr, int ofs, byte val)
		{
			// Obsolete
			throw new PlatformNotSupportedException ();
		}

		public static void WriteInt16(Object ptr, int ofs, short val)
		{
			// Obsolete
			throw new PlatformNotSupportedException ();
		}

		public static void WriteInt32(Object ptr, int ofs, int val)
		{
			// Obsolete
			throw new PlatformNotSupportedException ();
		}

		public static void WriteInt64(Object ptr, int ofs, long val)
		{
			// Obsolete
			throw new PlatformNotSupportedException ();
		}

		#endregion
	}
}

namespace System
{
    internal static partial class HResults
	{
        // DirectoryNotFoundException
         public const int STG_E_PATHNOTFOUND = unchecked((int)0x80030003);
        public const int CTL_E_PATHNOTFOUND = unchecked((int)0x800A004C);

        // FileNotFoundException
         public const int CTL_E_FILENOTFOUND = unchecked((int)0x800A0035);

        public const int FUSION_E_INVALID_PRIVATE_ASM_LOCATION = unchecked((int)0x80131041);
        public const int FUSION_E_SIGNATURE_CHECK_FAILED = unchecked((int)0x80131045);
        public const int FUSION_E_LOADFROM_BLOCKED = unchecked((int)0x80131051);
        public const int FUSION_E_CACHEFILE_FAILED = unchecked((int)0x80131052);
        public const int FUSION_E_ASM_MODULE_MISSING = unchecked((int)0x80131042);
        public const int FUSION_E_INVALID_NAME = unchecked((int)0x80131047);
        public const int FUSION_E_PRIVATE_ASM_DISALLOWED = unchecked((int)0x80131044);
        public const int FUSION_E_HOST_GAC_ASM_MISMATCH = unchecked((int)0x80131050);
        public const int COR_E_MODULE_HASH_CHECK_FAILED = unchecked((int)0x80131039);
        public const int FUSION_E_REF_DEF_MISMATCH = unchecked((int)0x80131040);
        public const int SECURITY_E_INCOMPATIBLE_SHARE = unchecked((int)0x80131401);
        public const int SECURITY_E_INCOMPATIBLE_EVIDENCE = unchecked((int)0x80131403);
        public const int SECURITY_E_UNVERIFIABLE = unchecked((int)0x80131402);
        public const int COR_E_FIXUPSINEXE = unchecked((int)0x80131019);
        public const int ERROR_TOO_MANY_OPEN_FILES = unchecked((int)0x80070004);
        public const int ERROR_SHARING_VIOLATION = unchecked((int)0x80070020);
        public const int ERROR_LOCK_VIOLATION = unchecked((int)0x80070021);
        public const int ERROR_OPEN_FAILED = unchecked((int)0x8007006E);
        public const int ERROR_DISK_CORRUPT = unchecked((int)0x80070571);
        public const int ERROR_UNRECOGNIZED_VOLUME = unchecked((int)0x800703ED);
        public const int ERROR_DLL_INIT_FAILED = unchecked((int)0x8007045A);
        public const int FUSION_E_CODE_DOWNLOAD_DISABLED = unchecked((int)0x80131048);
        public const int CORSEC_E_MISSING_STRONGNAME = unchecked((int)0x8013141b);
        public const int MSEE_E_ASSEMBLYLOADINPROGRESS = unchecked((int)0x80131016);
        public const int ERROR_FILE_INVALID = unchecked((int)0x800703EE);
	}
}
