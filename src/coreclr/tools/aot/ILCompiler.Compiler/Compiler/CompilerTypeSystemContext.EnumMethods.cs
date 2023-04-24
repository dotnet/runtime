// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Internal.TypeSystem;
using Internal.IL.Stubs;

using Debug = System.Diagnostics.Debug;

namespace ILCompiler
{
    public partial class CompilerTypeSystemContext
    {
        private sealed class EnumInfo
        {
            public TypeDesc Type => EqualsMethod.OwningType;

            public MethodDesc EqualsMethod { get; }

            public MethodDesc GetHashCodeMethod { get; }

            public EnumInfo(TypeDesc enumType)
            {
                Debug.Assert(enumType.IsEnum && enumType.IsTypeDefinition);
                EqualsMethod = new EnumEqualsThunk(enumType);
                GetHashCodeMethod = new EnumGetHashCodeThunk(enumType);
            }
        }

        private sealed class EnumInfoHashtable : LockFreeReaderHashtable<TypeDesc, EnumInfo>
        {
            protected override int GetKeyHashCode(TypeDesc key) => key.GetHashCode();
            protected override int GetValueHashCode(EnumInfo value) => value.Type.GetHashCode();
            protected override bool CompareKeyToValue(TypeDesc key, EnumInfo value) => key == value.Type;
            protected override bool CompareValueToValue(EnumInfo v1, EnumInfo v2) => v1.Type == v2.Type;

            protected override EnumInfo CreateValueFromKey(TypeDesc key)
            {
                return new EnumInfo(key);
            }
        }

        private EnumInfoHashtable _enumInfoHashtable = new EnumInfoHashtable();

        public MethodDesc TryResolveConstrainedEnumMethod(TypeDesc enumType, MethodDesc virtualMethod)
        {
            Debug.Assert(enumType.IsEnum);

            if (!virtualMethod.OwningType.IsObject)
                return null;

            // Also handle the odd case of generic enums

            TypeDesc enumTypeDefinition = enumType.GetTypeDefinition();
            EnumInfo info = _enumInfoHashtable.GetOrCreateValue(enumTypeDefinition);

            MethodDesc resolvedMethod;
            if (virtualMethod.Name == "Equals")
                resolvedMethod = info.EqualsMethod;
            else if (virtualMethod.Name == "GetHashCode")
                resolvedMethod = info.GetHashCodeMethod;
            else
                return null;

            if (enumType != enumTypeDefinition)
                return GetMethodForInstantiatedType(resolvedMethod, (InstantiatedType)enumType);

            return resolvedMethod;
        }

        protected virtual IEnumerable<MethodDesc> GetAllMethodsForEnum(TypeDesc enumType, bool virtualOnly)
        {
            if (virtualOnly)
            {
                // We devirtualize these, but they're not actually virtual. We don't want standalone method bodies
                // referenced from vtables. The base implementation on System.Enum is perflectly adequate.
                yield break;
            }

            _objectEqualsMethod ??= GetWellKnownType(WellKnownType.Object).GetMethod("Equals", null);

            // If the classlib doesn't have Object.Equals, we don't need this.
            if (_objectEqualsMethod == null)
                yield break;

            TypeDesc enumTypeDefinition = enumType.GetTypeDefinition();
            EnumInfo info = _enumInfoHashtable.GetOrCreateValue(enumTypeDefinition);

            if (enumType != enumTypeDefinition)
            {
                yield return GetMethodForInstantiatedType(info.GetHashCodeMethod, (InstantiatedType)enumType);
                yield return GetMethodForInstantiatedType(info.EqualsMethod, (InstantiatedType)enumType);
            }
            else
            {
                yield return info.GetHashCodeMethod;
                yield return info.EqualsMethod;
            }
        }
    }
}
