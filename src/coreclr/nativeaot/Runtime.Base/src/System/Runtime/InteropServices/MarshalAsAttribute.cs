// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices
{
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Field | AttributeTargets.ReturnValue, Inherited = false)]
    public sealed partial class MarshalAsAttribute : Attribute
    {
        public MarshalAsAttribute(UnmanagedType unmanagedType)
        {
            Value = unmanagedType;
        }
        public MarshalAsAttribute(short unmanagedType)
        {
            Value = (UnmanagedType)unmanagedType;
        }

        public UnmanagedType Value { get; }

        // Fields used with SubType = ByValArray and LPArray.
        // Array size =  parameter(PI) * PM + C
        public UnmanagedType ArraySubType;
        public short SizeParamIndex;           // param index PI
        public int SizeConst;                // constant C
    }
}
