// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Diagnostics;

using Internal.Runtime;
using Internal.Runtime.Augments;

namespace System
{
    [StructLayout(LayoutKind.Sequential)]
    public unsafe struct RuntimeTypeHandle : IEquatable<RuntimeTypeHandle>, ISerializable
    {
        //
        // Caution: There can be and are multiple MethodTable for the "same" type (e.g. int[]). That means
        // you can't use the raw IntPtr value for comparisons.
        //

        internal RuntimeTypeHandle(EETypePtr pEEType)
            : this(pEEType.RawValue)
        {
        }

        private RuntimeTypeHandle(IntPtr value)
        {
            _value = value;
        }

        public override bool Equals(object? obj)
        {
            if (obj is RuntimeTypeHandle)
            {
                RuntimeTypeHandle handle = (RuntimeTypeHandle)obj;
                return Equals(handle);
            }
            return false;
        }

        public override int GetHashCode()
        {
            if (IsNull)
                return 0;

            return this.ToEETypePtr().GetHashCode();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(RuntimeTypeHandle handle)
        {
            if (_value == handle._value)
            {
                return true;
            }
            else if (this.IsNull || handle.IsNull)
            {
                return false;
            }
            else
            {
                return RuntimeImports.AreTypesEquivalent(this.ToEETypePtr(), handle.ToEETypePtr());
            }
        }

        public static RuntimeTypeHandle FromIntPtr(IntPtr value) => new RuntimeTypeHandle(value);

        public static IntPtr ToIntPtr(RuntimeTypeHandle value) => value.Value;

        public static bool operator ==(object? left, RuntimeTypeHandle right)
        {
            if (left is RuntimeTypeHandle)
                return right.Equals((RuntimeTypeHandle)left);
            return false;
        }

        public static bool operator ==(RuntimeTypeHandle left, object? right)
        {
            if (right is RuntimeTypeHandle)
                return left.Equals((RuntimeTypeHandle)right);
            return false;
        }

        public static bool operator !=(object? left, RuntimeTypeHandle right)
        {
            if (left is RuntimeTypeHandle)
                return !right.Equals((RuntimeTypeHandle)left);
            return true;
        }

        public static bool operator !=(RuntimeTypeHandle left, object? right)
        {
            if (right is RuntimeTypeHandle)
                return !left.Equals((RuntimeTypeHandle)right);
            return true;
        }

        public IntPtr Value => _value;

        public ModuleHandle GetModuleHandle()
        {
            Type? type = Type.GetTypeFromHandle(this);
            if (type == null)
                return default(ModuleHandle);

            return type.Module.ModuleHandle;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            throw new PlatformNotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal EETypePtr ToEETypePtr()
        {
            return new EETypePtr(_value);
        }

        internal bool IsNull
        {
            get
            {
                return _value == new IntPtr(0);
            }
        }

        // Last resort string for Type.ToString() when no metadata around.
        internal string LastResortToString
        {
            get
            {
                string s;
                EETypePtr eeType = this.ToEETypePtr();
                IntPtr rawEEType = eeType.RawValue;
                IntPtr moduleBase = RuntimeImports.RhGetOSModuleFromEEType(rawEEType);
                if (moduleBase != IntPtr.Zero)
                {
                    uint rva = (uint)(rawEEType.ToInt64() - moduleBase.ToInt64());
                    s = "EETypeRva:0x" + rva.LowLevelToString();
                }
                else
                {
                    s = "EETypePointer:0x" + rawEEType.LowLevelToString();
                }

                ReflectionExecutionDomainCallbacks callbacks = RuntimeAugments.CallbacksIfAvailable;
                if (callbacks != null)
                {
                    string penultimateLastResortString = callbacks.GetBetterDiagnosticInfoIfAvailable(this);
                    if (penultimateLastResortString != null)
                        s += "(" + penultimateLastResortString + ")";
                }
                return s;
            }
        }

        internal IntPtr RawValue
        {
            get
            {
                return _value;
            }
        }

        private IntPtr _value;
    }
}
