// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;

using Internal.JitInterface;
using Internal.TypeSystem;
using Internal.TypeSystem.Ecma;

namespace ILCompiler.DependencyAnalysis.ReadyToRun
{
    public class SignatureContext
    {
        /// <summary>
        /// Context module used for the signature. Whenever a part of the signature
        /// needs to encode an entity external to the context module, it muse use
        /// an ELEMENT_TYPE_MODULE_ZAPSIG module override.
        /// </summary>
        public readonly IEcmaModule GlobalContext;

        /// <summary>
        /// Local context changes during recursive descent while encoding the signature.
        /// </summary>
        public readonly IEcmaModule LocalContext;

        /// <summary>
        /// Resolver used to back-translate types and fields to tokens.
        /// </summary>
        public readonly ModuleTokenResolver Resolver;

        public SignatureContext OuterContext => new SignatureContext(GlobalContext, Resolver);

        public SignatureContext(IEcmaModule context, ModuleTokenResolver resolver)
        {
            GlobalContext = context;
            LocalContext = context;
            Resolver = resolver;
        }

        private SignatureContext(IEcmaModule globalContext, IEcmaModule localContext, ModuleTokenResolver resolver)
        {
            GlobalContext = globalContext;
            LocalContext = localContext;
            Resolver = resolver;
        }

        public SignatureContext InnerContext(IEcmaModule innerContext)
        {
            return new SignatureContext(GlobalContext, innerContext, Resolver);
        }

        public IEcmaModule GetTargetModule(TypeDesc type)
        {
            if (type.IsPrimitive || type.IsString || type.IsObject || type.IsWellKnownType(WellKnownType.TypedReference))
            {
                return LocalContext;
            }
            if (type.GetTypeDefinition() is EcmaType ecmaType)
            {
                return GetModuleTokenForType(ecmaType).Module;
            }
            return LocalContext;
        }

        public IEcmaModule GetTargetModule(MethodDesc method)
        {
            return GetModuleTokenForMethod(method).Module;
        }

        public ModuleToken GetModuleTokenForType(EcmaType type, bool throwIfNotFound = true)
        {
            return Resolver.GetModuleTokenForType(type, allowDynamicallyCreatedReference:true, throwIfNotFound: throwIfNotFound);
        }

        public ModuleToken GetModuleTokenForMethod(MethodDesc method)
        {
            return Resolver.GetModuleTokenForMethod(method, throwIfNotFound: false, allowDynamicallyCreatedReference: false);
        }

        public bool Equals(SignatureContext other)
        {
            return GlobalContext == other.GlobalContext
                && LocalContext == other.LocalContext;
        }

        public override bool Equals(object obj)
        {
            return obj is SignatureContext other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (GlobalContext?.GetHashCode() ?? 0) ^ ((LocalContext?.GetHashCode() ?? 0) * 31);
        }

        public int CompareTo(SignatureContext other, TypeSystemComparer comparer)
        {
            if (GlobalContext == null || other.GlobalContext == null)
            {
                return (GlobalContext != null ? 1 : other.GlobalContext != null ? -1 : 0);
            }

            int result = GlobalContext.CompareTo(other.GlobalContext);
            if (result != 0)
                return result;

            if (LocalContext == null || other.LocalContext == null)
            {
                return (LocalContext != null ? 1 : other.LocalContext != null ? -1 : 0);
            }

            return LocalContext.CompareTo(other.LocalContext);
        }

        public override string ToString()
        {
            return (GlobalContext != null ? GlobalContext.Assembly.GetName().Name : "<Composite>");
        }
    }
}
