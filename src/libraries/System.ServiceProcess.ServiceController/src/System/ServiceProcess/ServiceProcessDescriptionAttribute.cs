//------------------------------------------------------------------------------
// <copyright file="ServiceProcessDescriptionAttribute.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>                                                                
//------------------------------------------------------------------------------

namespace System.ServiceProcess {


    using System;
    using System.ComponentModel;   

    /// <include file='doc\ServiceProcessDescriptionAttribute.uex' path='docs/doc[@for="ServiceProcessDescriptionAttribute"]/*' />
    /// <devdoc>
    ///     DescriptionAttribute marks a property, event, or extender with a
    ///     description. Visual designers can display this description when referencing
    ///     the member.
    /// </devdoc>
    [AttributeUsage(AttributeTargets.All)]
    public class ServiceProcessDescriptionAttribute : DescriptionAttribute
    {
        private bool replaced = false;

        /// <include file='doc\ServiceProcessDescriptionAttribute.uex' path='docs/doc[@for="ServiceProcessDescriptionAttribute.ServiceProcessDescriptionAttribute"]/*' />
        /// <devdoc>
        ///     Constructs a new sys description.
        /// </devdoc>
        public ServiceProcessDescriptionAttribute(string description) : base(description) {
        }

        /// <include file='doc\ServiceProcessDescriptionAttribute.uex' path='docs/doc[@for="ServiceProcessDescriptionAttribute.Description"]/*' />
        /// <devdoc>
        ///     Retrieves the description text.
        /// </devdoc>
        public override string Description {
            get {
                if (!replaced) {
                    replaced = true;
                    DescriptionValue = SR.Format(base.Description);
                }
                return base.Description;
            }
        }
    }
}
