// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;

namespace System.Reflection.TypeLoading.Ecma
{
    internal sealed class EcmaCustomAttributeData : RoCustomAttributeData
    {
        private readonly CustomAttributeHandle _handle;
        private readonly EcmaModule _module;

        private volatile IList<CustomAttributeTypedArgument<RoType>> _lazyFixedArguments;
        private volatile IList<CustomAttributeNamedArgument<RoType>> _lazyNamedArguments;

        internal EcmaCustomAttributeData(CustomAttributeHandle handle, EcmaModule module)
        {
            _handle = handle;
            _module = module;
            _neverAccessThisExceptThroughCustomAttributeProperty = handle.GetCustomAttribute(Reader);
        }

        public sealed override IList<CustomAttributeTypedArgument> ConstructorArguments
        {
            get
            {
                if (_lazyFixedArguments == null)
                    LoadArguments();

                return _lazyFixedArguments.ToApiForm();
            }
        }

        public sealed override IList<CustomAttributeNamedArgument> NamedArguments
        {
            get
            {
                if (_lazyNamedArguments == null)
                    LoadArguments();

                return _lazyNamedArguments.ToApiForm(AttributeType);
            }
        }

        protected sealed override Type ComputeAttributeType()
        {
            EntityHandle declaringTypeHandle = CustomAttribute.TryGetDeclaringTypeHandle(Reader);
            if (declaringTypeHandle.IsNil)
                throw new BadImageFormatException();

            return declaringTypeHandle.ResolveTypeDefRefOrSpec(_module, default);
        }

        protected sealed override ConstructorInfo ComputeConstructor()
        {
            EntityHandle ctorHandle = CustomAttribute.Constructor;
            switch (ctorHandle.Kind)
            {
                case HandleKind.MethodDefinition:
                {
                    MethodDefinitionHandle mh = (MethodDefinitionHandle)ctorHandle;
                    EcmaDefinitionType declaringType = mh.GetMethodDefinition(Reader).GetDeclaringType().ResolveTypeDef(_module);
                    return new RoDefinitionConstructor<EcmaMethodDecoder>(declaringType, new EcmaMethodDecoder(mh, _module));
                }

                case HandleKind.MemberReference:
                {
                    TypeContext typeContext = default;
                    MemberReference mr = ((MemberReferenceHandle)ctorHandle).GetMemberReference(Reader);
                    MethodSignature<RoType> sig = mr.DecodeMethodSignature(_module, typeContext);
                    Type[] parameterTypes = sig.ParameterTypes.ToArray();
                    Type declaringType = mr.Parent.ResolveTypeDefRefOrSpec(_module, typeContext);
                    const BindingFlags bf = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.ExactBinding;
                    ConstructorInfo ci = declaringType.GetConstructor(bf, null, parameterTypes, null);
                    if (ci == null)
                        throw new MissingMethodException(SR.Format(SR.MissingCustomAttributeConstructor, declaringType));
                    return ci;
                }

                // Constructors can not be generic methods so this should never occur. (Not to be confused with constructors on generic types and we do now
                // support generic attributes. But that comes in as a MemberReference, not a MethodSpec.)
                case HandleKind.MethodSpecification:
                    throw new BadImageFormatException();

                default:
                    throw new BadImageFormatException();
            }
        }

        private void LoadArguments()
        {
            CustomAttributeValue<RoType> cav = CustomAttribute.DecodeValue(_module);
            _lazyFixedArguments = cav.FixedArguments;
            _lazyNamedArguments = cav.NamedArguments;
        }

        private MetadataReader Reader => _module.Reader;
        private MetadataLoadContext Loader => _module.Loader;

        private ref readonly CustomAttribute CustomAttribute { get { Loader.DisposeCheck(); return ref _neverAccessThisExceptThroughCustomAttributeProperty; } }
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]  // Block from debugger watch windows so they don't AV the debugged process.
        private readonly CustomAttribute _neverAccessThisExceptThroughCustomAttributeProperty;
    }
}
