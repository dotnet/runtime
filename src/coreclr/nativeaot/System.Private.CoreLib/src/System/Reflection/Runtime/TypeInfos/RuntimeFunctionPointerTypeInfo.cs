// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Reflection.Runtime.General;
using System.Text;

using StructLayoutAttribute = System.Runtime.InteropServices.StructLayoutAttribute;

namespace System.Reflection.Runtime.TypeInfos
{
    //
    // The runtime's implementation of TypeInfo for function pointer types.
    //
    internal sealed partial class RuntimeFunctionPointerTypeInfo : RuntimeTypeInfo, IRuntimeMemberInfoWithNoMetadataDefinition
    {
        private RuntimeFunctionPointerTypeInfo(UnificationKey key)
        {
            _key = key;
        }

        public override bool IsTypeDefinition => false;
        public override bool IsGenericTypeDefinition => false;
        protected override bool HasElementTypeImpl() => false;
        protected override bool IsArrayImpl() => false;
        public override bool IsSZArray => false;
        public override bool IsVariableBoundArray => false;
        protected override bool IsByRefImpl() => false;
        protected override bool IsPointerImpl() => false;
        public override bool IsConstructedGenericType => false;
        public override bool IsGenericParameter => false;
        public override bool IsGenericTypeParameter => false;
        public override bool IsGenericMethodParameter => false;
        public override bool IsByRefLike => false;

        //
        // Implements IKeyedItem.PrepareKey.
        //
        // This method is the keyed item's chance to do any lazy evaluation needed to produce the key quickly.
        // Concurrent unifiers are guaranteed to invoke this method at least once and wait for it
        // to complete before invoking the Key property. The unifier lock is NOT held across the call.
        //
        // PrepareKey() must be idempodent and thread-safe. It may be invoked multiple times and concurrently.
        //
        public void PrepareKey()
        {
        }

        //
        // Implements IKeyedItem.Key.
        //
        // Produce the key. This is a high-traffic property and is called while the hash table's lock is held. Thus, it should
        // return a precomputed stored value and refrain from invoking other methods. If the keyed item wishes to
        // do lazy evaluation of the key, it should do so in the PrepareKey() method.
        //
        public UnificationKey Key
        {
            get
            {
                return _key;
            }
        }

        public override Assembly Assembly => typeof(object).Assembly;
        public override IEnumerable<CustomAttributeData> CustomAttributes => Array.Empty<CustomAttributeData>();
        public override Type[] GetFunctionPointerCallingConventions() => Type.EmptyTypes;

        public override bool ContainsGenericParameters
        {
            get
            {
                if (_key.ReturnType.ContainsGenericParameters)
                    return true;

                foreach (var p in _key.ParameterTypes)
                    if (p.ContainsGenericParameters)
                        return true;

                return false;
            }
        }
        public override string FullName => null!;
        public override bool HasSameMetadataDefinitionAs(MemberInfo other)
        {
            ArgumentNullException.ThrowIfNull(other);

            // This logic is written to match CoreCLR's behavior.
            return other is Type && other is IRuntimeMemberInfoWithNoMetadataDefinition;
        }

        public override bool IsFunctionPointer => true;
        public override bool IsUnmanagedFunctionPointer => _key.IsUnmanaged;

        public override Type[] GetFunctionPointerParameterTypes()
        {

            if (_key.ParameterTypes.Length == 0)
                return EmptyTypes;

            Type[] result = new Type[_key.ParameterTypes.Length];
            Array.Copy(_key.ParameterTypes, result, result.Length);
            return result;
        }

        public override Type GetFunctionPointerReturnType() => _key.ReturnType;

        public override string Namespace => null!;

        public override StructLayoutAttribute StructLayoutAttribute => null;
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(_key.ReturnType.ToString());
            sb.Append('(');
            RuntimeTypeInfo[] parameters = _key.ParameterTypes;
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0)
                    sb.Append(", ");
                sb.Append(parameters[i].ToString());
            }
            sb.Append(')');
            return sb.ToString();
        }

        public override int MetadataToken
        {
            get
            {
                return 0x02000000; // nil TypeDef token
            }
        }

        protected override TypeAttributes GetAttributeFlagsImpl() => TypeAttributes.Public;
        protected override int InternalGetHashCode() =>_key.GetHashCode();
        internal override Type InternalDeclaringType => null;
        public override string Name => string.Empty;
        internal override string InternalFullNameOfAssembly => string.Empty;
        internal override RuntimeTypeHandle InternalTypeHandleIfAvailable => _key.TypeHandle;

        private readonly UnificationKey _key;
    }
}
