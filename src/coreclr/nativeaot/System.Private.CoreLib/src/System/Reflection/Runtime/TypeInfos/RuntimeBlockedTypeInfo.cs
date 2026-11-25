// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using Internal.Runtime.Augments;

namespace System.Reflection.Runtime.TypeInfos
{
    //
    // TypeInfos that represent types that don't have reflection metadata, because the compiler
    // was asked not to generate any (the type is not public, in an assembly annotated with DisablePrivateReflectionAttribute).
    //
    // The name of the type is a placeholder. Everything else that needs the metadata of the type fails with a
    // NotSupportedException. What can be answered from the MethodTable (value type, interface, ...) is still available.
    //
    internal sealed class RuntimeBlockedTypeInfo : RuntimeTypeDefinitionTypeInfo
    {
        private static readonly ConcurrentDictionary<RuntimeTypeHandle, RuntimeBlockedTypeInfo> s_blockedTypes = new();

        private readonly RuntimeTypeHandle _typeHandle;

        private RuntimeBlockedTypeInfo(RuntimeTypeHandle typeHandle)
        {
            _typeHandle = typeHandle;
        }

        internal static RuntimeBlockedTypeInfo GetRuntimeBlockedTypeInfo(RuntimeTypeHandle typeHandle)
        {
            return s_blockedTypes.GetOrAdd(typeHandle, static handle => new RuntimeBlockedTypeInfo(handle));
        }

        private string PlaceholderName => $"<PrivateType 0x{_typeHandle.GetHashCode():X8}>";

        private static NotSupportedException CreateBlockedException() => new NotSupportedException(SR.Reflection_TypeBlocked);

        public sealed override Assembly Assembly => throw CreateBlockedException();

        public sealed override bool ContainsGenericParameters => false;

        public sealed override string FullName => PlaceholderName;

        public sealed override string Name => PlaceholderName;

        public sealed override string Namespace => null!;

        public sealed override string ToString() => PlaceholderName;

        public sealed override Guid GUID => throw CreateBlockedException();

        public sealed override int MetadataToken => throw new InvalidOperationException(SR.NoMetadataTokenAvailable);

        public sealed override bool IsByRefLike => RuntimeAugments.IsByRefLike(_typeHandle);

        public sealed override StructLayoutAttribute StructLayoutAttribute => throw CreateBlockedException();

        // The MethodTable knows what kind of type it is, and (as flags that only the types that have no metadata because they are stripped have) if it is abstract or sealed.
        // The rest of the attributes (visibility of a nested type, layout, ...) are not known.
        public sealed override TypeAttributes Attributes
        {
            get
            {
                TypeAttributes attributes = TypeAttributes.NotPublic;
                if (RuntimeAugments.IsInterface(_typeHandle))
                    attributes |= TypeAttributes.Interface | TypeAttributes.Abstract;
                else
                    attributes |= TypeAttributes.Class;
                if (RuntimeAugments.IsValueType(_typeHandle) || RuntimeAugments.IsStrippedSealed(_typeHandle))
                    attributes |= TypeAttributes.Sealed;
                if (RuntimeAugments.IsStrippedAbstract(_typeHandle))
                    attributes |= TypeAttributes.Abstract;
                return attributes;
            }
        }

        public sealed override int GetHashCode() => _typeHandle.GetHashCode();

        internal sealed override RuntimeTypeInfo InternalDeclaringType => null;

        internal sealed override string InternalFullNameOfAssembly => throw CreateBlockedException();

        internal sealed override RuntimeTypeHandle InternalTypeHandleIfAvailable => _typeHandle;
    }
}
