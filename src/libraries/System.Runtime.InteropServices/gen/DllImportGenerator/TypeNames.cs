using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Microsoft.Interop
{
    static class TypeNames
    {
        public const string GeneratedDllImportAttribute = "System.Runtime.InteropServices.GeneratedDllImportAttribute";

        public const string GeneratedMarshallingAttribute = "System.Runtime.InteropServices.GeneratedMarshallingAttribute";

        public const string BlittableTypeAttribute = "System.Runtime.InteropServices.BlittableTypeAttribute";

        public const string NativeMarshallingAttribute = "System.Runtime.InteropServices.NativeMarshallingAttribute";

        public const string MarshalUsingAttribute = "System.Runtime.InteropServices.MarshalUsingAttribute";

        public const string GenericContiguousCollectionMarshallerAttribute = "System.Runtime.InteropServices.GenericContiguousCollectionMarshallerAttribute";

        public const string LCIDConversionAttribute = "System.Runtime.InteropServices.LCIDConversionAttribute";

        public const string SuppressGCTransitionAttribute = "System.Runtime.InteropServices.SuppressGCTransitionAttribute";

        public const string UnmanagedCallConvAttribute = "System.Runtime.InteropServices.UnmanagedCallConvAttribute";

        public const string System_Span_Metadata = "System.Span`1";
        public const string System_Span = "System.Span";

        public const string System_Activator = "System.Activator";

        public const string System_Runtime_InteropServices_StructLayoutAttribute = "System.Runtime.InteropServices.StructLayoutAttribute";

        public const string System_Runtime_InteropServices_MarshalAsAttribute = "System.Runtime.InteropServices.MarshalAsAttribute";

        public const string System_Runtime_InteropServices_Marshal = "System.Runtime.InteropServices.Marshal";

        private const string System_Runtime_InteropServices_MarshalEx = "System.Runtime.InteropServices.MarshalEx";

        public static string MarshalEx(AnalyzerConfigOptions options)
        {
            return options.UseMarshalType() ? System_Runtime_InteropServices_Marshal : System_Runtime_InteropServices_MarshalEx;
        }

        public const string System_Runtime_InteropServices_GeneratedMarshalling_ArrayMarshaller_Metadata = "System.Runtime.InteropServices.GeneratedMarshalling.ArrayMarshaller`1";

        public const string System_Runtime_InteropServices_GeneratedMarshalling_PtrArrayMarshaller_Metadata = "System.Runtime.InteropServices.GeneratedMarshalling.PtrArrayMarshaller`1";

        public const string System_Runtime_InteropServices_MemoryMarshal = "System.Runtime.InteropServices.MemoryMarshal";

        public const string System_Runtime_InteropServices_SafeHandle = "System.Runtime.InteropServices.SafeHandle";

        public const string System_Runtime_InteropServices_OutAttribute = "System.Runtime.InteropServices.OutAttribute";

        public const string System_Runtime_InteropServices_InAttribute = "System.Runtime.InteropServices.InAttribute";

        public const string System_Runtime_CompilerServices_SkipLocalsInitAttribute = "System.Runtime.CompilerServices.SkipLocalsInitAttribute";

        private const string System_Runtime_CompilerServices_Unsafe = "System.Runtime.CompilerServices.Unsafe";

        private const string Internal_Runtime_CompilerServices_Unsafe = "Internal.Runtime.CompilerServices.Unsafe";

        public static string Unsafe(AnalyzerConfigOptions options)
        {
            return options.UseInternalUnsafeType() ? Internal_Runtime_CompilerServices_Unsafe : System_Runtime_CompilerServices_Unsafe;
        }

        public const string System_Type = "System.Type";
    }
}
