// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace System.Reflection.TypeLoading
{
    internal sealed class RoPseudoCustomAttributeData : RoCustomAttributeData
    {
        private readonly ConstructorInfo _constructor;
        private readonly Func<CustomAttributeArguments> _argumentsPromise;

        private volatile IList<CustomAttributeTypedArgument> _lazyFixedArguments;
        private volatile IList<CustomAttributeNamedArgument> _lazyNamedArguments;

        //
        // For complex custom attributes, use this overload to defer the work of constructing the argument lists until needed.
        //
        internal RoPseudoCustomAttributeData(ConstructorInfo constructor, Func<CustomAttributeArguments> argumentsPromise)
        {
            CustomAttributeArguments ca = argumentsPromise();

            _constructor = constructor;
            _argumentsPromise = argumentsPromise;
        }

        internal RoPseudoCustomAttributeData(ConstructorInfo constructor, IList<CustomAttributeTypedArgument> fixedArguments = null, IList<CustomAttributeNamedArgument> namedArguments = null)
        {
            _constructor = constructor;
            _lazyFixedArguments = fixedArguments ?? Array.Empty<CustomAttributeTypedArgument>();
            _lazyNamedArguments = namedArguments ?? Array.Empty<CustomAttributeNamedArgument>();
        }

        public sealed override IList<CustomAttributeTypedArgument> ConstructorArguments => GetLatchedFixedArguments().CloneForApiReturn();
        public sealed override IList<CustomAttributeNamedArgument> NamedArguments => GetLatchedNamedArguments().CloneForApiReturn();

        private IList<CustomAttributeTypedArgument> GetLatchedFixedArguments() => _lazyFixedArguments ?? LazilyComputeArguments().FixedArguments;
        private IList<CustomAttributeNamedArgument> GetLatchedNamedArguments() => _lazyNamedArguments ?? LazilyComputeArguments().NamedArguments;

        protected sealed override Type ComputeAttributeType() => _constructor.DeclaringType;
        protected sealed override ConstructorInfo ComputeConstructor() => _constructor;

        private CustomAttributeArguments LazilyComputeArguments()
        {
            CustomAttributeArguments ca = _argumentsPromise();
            _lazyFixedArguments = ca.FixedArguments;
            _lazyNamedArguments = ca.NamedArguments;
            return ca;
        }
    }
}
