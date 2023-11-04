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

        //
        // Implements IKeyedItem.Key.
        //
        // Produce the key. This is a high-traffic property and is called while the hash table's lock is held. Thus, it should
        // return a precomputed stored value and refrain from invoking other methods.
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
            return other is RuntimeType runtimeType && runtimeType.GetRuntimeTypeInfo() is IRuntimeMemberInfoWithNoMetadataDefinition;
        }

        public override bool IsFunctionPointer => true;
        public override bool IsUnmanagedFunctionPointer => _key.IsUnmanaged;

        public override Type[] GetFunctionPointerParameterTypes()
        {
            if (_key.ParameterTypes.Length == 0)
                return Type.EmptyTypes;

            Type[] result = new Type[_key.ParameterTypes.Length];
            Array.Copy(_key.ParameterTypes, result, result.Length);
            return result;
        }

        public override Type GetFunctionPointerReturnType() => _key.ReturnType.ToType();

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

        public override TypeAttributes Attributes => TypeAttributes.Public;
        public override int GetHashCode() => _key.GetHashCode();
        internal override RuntimeTypeInfo InternalDeclaringType => null;
        public override string Name => string.Empty;
        internal override string InternalFullNameOfAssembly => string.Empty;
        internal override RuntimeTypeHandle InternalTypeHandleIfAvailable => _key.TypeHandle;

        private readonly UnificationKey _key;
    }
}
