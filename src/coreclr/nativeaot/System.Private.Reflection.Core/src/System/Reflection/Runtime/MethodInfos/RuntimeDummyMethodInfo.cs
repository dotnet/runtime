// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection.Runtime.ParameterInfos;
using System.Reflection.Runtime.TypeInfos;
using Internal.Reflection.Core.Execution;

namespace System.Reflection.Runtime.MethodInfos
{
    //
    // Singleton MethodInfo used as a sentinel for _lazy* latches where we can't use "null" as a sentinel.
    //
    internal sealed class RuntimeDummyMethodInfo : RuntimeNamedMethodInfo
    {
        private RuntimeDummyMethodInfo() { }

        public sealed override bool Equals(object obj) => object.ReferenceEquals(this, obj);
        public sealed override int GetHashCode() => 1;
        public sealed override string ToString() => string.Empty;

        public sealed override MethodInfo GetGenericMethodDefinition() { throw NotImplemented.ByDesign; }
        [RequiresDynamicCode("The native code for this instantiation might not be available at runtime.")]
        [RequiresUnreferencedCode("If some of the generic arguments are annotated (either with DynamicallyAccessedMembersAttribute, or generic constraints), trimming can't validate that the requirements of those annotations are met.")]
        public sealed override MethodInfo MakeGenericMethod(params Type[] typeArguments) { throw NotImplemented.ByDesign; }
        public sealed override MethodAttributes Attributes { get { throw NotImplemented.ByDesign; } }
        public sealed override Type ReflectedType { get { throw NotImplemented.ByDesign; } }
        public sealed override CallingConventions CallingConvention { get { throw NotImplemented.ByDesign; } }
        public sealed override IEnumerable<CustomAttributeData> CustomAttributes { get { throw NotImplemented.ByDesign; } }
        public sealed override bool IsConstructedGenericMethod { get { throw NotImplemented.ByDesign; } }
        public sealed override bool IsGenericMethod { get { throw NotImplemented.ByDesign; } }
        public sealed override bool IsGenericMethodDefinition { get { throw NotImplemented.ByDesign; } }
        public sealed override int GenericParameterCount { get { throw NotImplemented.ByDesign; } }
        public sealed override bool HasSameMetadataDefinitionAs(MemberInfo other) { throw NotImplemented.ByDesign; }
        public sealed override MethodImplAttributes MethodImplementationFlags { get { throw NotImplemented.ByDesign; } }
        public sealed override Module Module { get { throw NotImplemented.ByDesign; } }
        public sealed override MethodBase MetadataDefinitionMethod { get { throw NotImplemented.ByDesign; } }
        public sealed override int MetadataToken { get { throw NotImplemented.ByDesign; } }
        public sealed override RuntimeMethodHandle MethodHandle { get { throw NotImplemented.ByDesign; } }
        protected sealed override MethodInvoker UncachedMethodInvoker { get { throw NotImplemented.ByDesign; } }
        internal sealed override RuntimeParameterInfo[] GetRuntimeParameters(RuntimeMethodInfo contextMethod, out RuntimeParameterInfo returnParameter) { throw NotImplemented.ByDesign; }
        internal sealed override RuntimeTypeInfo RuntimeDeclaringType { get { throw NotImplemented.ByDesign; } }
        internal sealed override string RuntimeName { get { throw NotImplemented.ByDesign; } }
        internal sealed override RuntimeTypeInfo[] RuntimeGenericArgumentsOrParameters { get { throw NotImplemented.ByDesign; } }

        protected internal sealed override string ComputeToString(RuntimeMethodInfo contextMethod) { throw NotImplemented.ByDesign; }
        internal sealed override MethodInvoker GetUncachedMethodInvoker(RuntimeTypeInfo[] methodArguments, MemberInfo exceptionPertainant) { throw NotImplemented.ByDesign; }
        internal sealed override RuntimeMethodHandle GetRuntimeMethodHandle(Type[] genericArgs) { throw NotImplemented.ByDesign; }
        internal sealed override RuntimeMethodInfo WithReflectedTypeSetToDeclaringType { get { throw NotImplemented.ByDesign; } }
        public static readonly RuntimeDummyMethodInfo Instance = new RuntimeDummyMethodInfo();
    }
}
