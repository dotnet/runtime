// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


using System;

using Internal.Runtime.CompilerServices;

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public partial class MethodDesc
    {
        private IntPtr _functionPointer;

        public void SetFunctionPointer(IntPtr functionPointer)
        {
            Debug.Assert(_functionPointer == IntPtr.Zero || _functionPointer == functionPointer);
            _functionPointer = functionPointer;
        }

        /// <summary>
        /// Pointer to function's code. May be IntPtr.Zero
        /// </summary>
        public IntPtr FunctionPointer
        {
            get
            {
                return _functionPointer;
            }
        }

        public abstract MethodNameAndSignature NameAndSignature { get; }

        private bool? _isNonSharableCache;
        public virtual bool IsNonSharableMethod
        {
            get
            {
                if (!_isNonSharableCache.HasValue)
                {
                    _isNonSharableCache = ComputeIsNonSharableMethod();
                }
                return _isNonSharableCache.Value;
            }
        }

        protected virtual bool ComputeIsNonSharableMethod()
        {
            return !OwningType.IsCanonicalSubtype(CanonicalFormKind.Any) &&
                        OwningType == (OwningType.ConvertToCanonForm(CanonicalFormKind.Specific) as DefType);
        }

        public virtual bool UnboxingStub
        {
            get
            {
                return false;
            }
        }
    }
}
