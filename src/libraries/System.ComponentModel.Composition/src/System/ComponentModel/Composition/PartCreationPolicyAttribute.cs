// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel.Composition
{
    /// <summary>
    ///     Specifies <see cref="CreationPolicy"/> for a given <see cref="ComposablePart" />.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public sealed class PartCreationPolicyAttribute : Attribute
    {
        internal static PartCreationPolicyAttribute Default = new PartCreationPolicyAttribute(CreationPolicy.Any);
        internal static PartCreationPolicyAttribute Shared = new PartCreationPolicyAttribute(CreationPolicy.Shared);

        /// <summary>
        ///     Initializes a new instance of the <see cref="PartCreationPolicyAttribute"/> class.
        /// </summary>
        public PartCreationPolicyAttribute(CreationPolicy creationPolicy)
        {
            CreationPolicy = creationPolicy;
        }

        /// <summary>
        ///     Gets or sets a value indicating the creation policy of the attributed part.
        /// </summary>
        /// <value>
        ///     One of the <see cref="CreationPolicy"/> values indicating the creation policy of the 
        ///     attributed part. The default is 
        ///     <see cref="System.ComponentModel.Composition.CreationPolicy.Any"/>.
        /// </value>
        public CreationPolicy CreationPolicy { get; private set; }
    }
}
