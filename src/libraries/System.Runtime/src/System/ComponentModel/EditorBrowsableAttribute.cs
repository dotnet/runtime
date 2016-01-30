// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Constructor | AttributeTargets.Method | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Delegate | AttributeTargets.Interface)]
    public sealed class EditorBrowsableAttribute : Attribute
    {
        private EditorBrowsableState browsableState;


        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public EditorBrowsableAttribute(EditorBrowsableState state)
        {
            browsableState = state;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public EditorBrowsableState State
        {
            get { return browsableState; }
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            EditorBrowsableAttribute other = obj as EditorBrowsableAttribute;

            return (other != null) && other.browsableState == browsableState;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }

    /// <devdoc>
    ///    <para>[To be supplied.]</para>
    /// </devdoc>
    public enum EditorBrowsableState
    {
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Always,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Never,
        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        Advanced
    }
}
