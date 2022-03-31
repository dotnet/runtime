// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.CompilerServices;

namespace System.Runtime.InteropServices.JavaScript
{
    public partial struct JavaScriptMarshalerArg
    {
        // intentionaly opaque internal structure

        //TODO add to ref assembly
        public unsafe IntPtr TypeHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value[0].TypeHandle;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Value[0].TypeHandle = value;
            }
        }

        public unsafe IntPtr RootRef
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value[0].RootRef;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Value[0].RootRef = value;
            }
        }

        public unsafe IntPtr JSHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value[0].JSHandle;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Value[0].JSHandle = value;
            }
        }

        public unsafe IntPtr GCHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value[0].GCHandle;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Value[0].GCHandle = value;
            }
        }

        public unsafe bool BooleanValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value[0].BooleanValue;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Value[0].BooleanValue = value;
            }
        }

        public unsafe byte ByteValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value[0].ByteValue;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Value[0].ByteValue = value;
            }
        }

        public unsafe short Int16Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value[0].Int16Value;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Value[0].Int16Value = value;
            }
        }

        public unsafe int Int32Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value[0].Int32Value;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Value[0].Int32Value = value;
            }
        }

        public unsafe long Int64Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value[0].Int64Value;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Value[0].Int64Value = value;
            }
        }

        public unsafe double DoubleValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value[0].DoubleValue;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Value[0].DoubleValue = value;
            }
        }

        public unsafe float SingleValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value[0].SingleValue;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Value[0].SingleValue = value;
            }
        }

        public unsafe IntPtr IntPtrValue
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value[0].IntPtrValue;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Value[0].IntPtrValue = value;
            }
        }

        public unsafe IntPtr ExtraBufferPtr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                return Value[0].ExtraBufferPtr;
            }
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set
            {
                Value[0].ExtraBufferPtr = value;
            }
        }
    }
}
