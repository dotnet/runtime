// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime;
using System.Diagnostics;
using Internal.Runtime.Augments;

namespace Internal.Runtime.CompilerServices
{
    public struct RuntimeSignature
    {
        private IntPtr _moduleHandle;
        private int _tokenOrOffset;
        private bool _isNativeLayoutSignature;

        [CLSCompliant(false)]
        public static RuntimeSignature CreateFromNativeLayoutSignature(TypeManagerHandle moduleHandle, uint nativeLayoutOffset)
        {
            return new RuntimeSignature
            {
                _moduleHandle = moduleHandle.GetIntPtrUNSAFE(),
                _tokenOrOffset = (int)nativeLayoutOffset,
                _isNativeLayoutSignature = true,
            };
        }

        [CLSCompliant(false)]
        public static RuntimeSignature CreateFromNativeLayoutSignature(RuntimeSignature oldSignature, uint newNativeLayoutOffset)
        {
            return new RuntimeSignature
            {
                _moduleHandle = oldSignature._moduleHandle,
                _tokenOrOffset = (int)newNativeLayoutOffset,
                _isNativeLayoutSignature = true,
            };
        }

        public static RuntimeSignature CreateFromMethodHandle(TypeManagerHandle moduleHandle, int token)
        {
            return new RuntimeSignature
            {
                _moduleHandle = moduleHandle.GetIntPtrUNSAFE(),
                _tokenOrOffset = token,
                _isNativeLayoutSignature = false,
            };
        }

        public static RuntimeSignature CreateFromMethodHandle(IntPtr moduleHandle, int token)
        {
            return new RuntimeSignature
            {
                _moduleHandle = moduleHandle,
                _tokenOrOffset = token,
                _isNativeLayoutSignature = false,
            };
        }

        [CLSCompliant(false)]
        public static RuntimeSignature CreateFromNativeLayoutSignatureForDebugger(uint nativeLayoutOffset)
        {
            // This is a RuntimeSignature object used by the debugger only,
            // the fact that the _moduleHandle is NULL signify that information.
            return new RuntimeSignature
            {
                _moduleHandle = IntPtr.Zero,
                _tokenOrOffset = (int)nativeLayoutOffset,
                _isNativeLayoutSignature = true,
            };
        }

        public bool IsNativeLayoutSignature
        {
            get
            {
                return _isNativeLayoutSignature;
            }
        }

        public int Token
        {
            get
            {
                if (_isNativeLayoutSignature)
                {
                    Debug.Assert(false);
                    return -1;
                }
                return _tokenOrOffset;
            }
        }

        [CLSCompliant(false)]
        public uint NativeLayoutOffset
        {
            get
            {
                if (!_isNativeLayoutSignature)
                {
                    Debug.Assert(false);
                    return unchecked((uint)-1);
                }
                return (uint)_tokenOrOffset;
            }
        }

        public IntPtr ModuleHandle
        {
            get
            {
                return _moduleHandle;
            }
        }

        public bool Equals(RuntimeSignature other)
        {
            if (IsNativeLayoutSignature && other.IsNativeLayoutSignature)
            {
                if ((ModuleHandle == other.ModuleHandle) && (NativeLayoutOffset == other.NativeLayoutOffset))
                    return true;
            }
            else if (!IsNativeLayoutSignature && !other.IsNativeLayoutSignature)
            {
                if ((ModuleHandle == other.ModuleHandle) && (Token == other.Token))
                    return true;
            }

            // Walk both signatures to check for equality the slow way
            return RuntimeAugments.TypeLoaderCallbacks.CompareMethodSignatures(this, other);
        }

        /// <summary>
        /// Fast equality check
        /// </summary>
        public bool StructuralEquals(RuntimeSignature other)
        {
            if (_moduleHandle != other._moduleHandle)
                return false;

            if (_tokenOrOffset != other._tokenOrOffset)
                return false;

            if (_isNativeLayoutSignature != other._isNativeLayoutSignature)
                return false;

            return true;
        }
    }
}
