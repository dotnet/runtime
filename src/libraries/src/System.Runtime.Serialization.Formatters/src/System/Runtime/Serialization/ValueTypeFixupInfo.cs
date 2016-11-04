// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Reflection;

namespace System.Runtime.Serialization
{
    internal sealed class ValueTypeFixupInfo
    {
        /// <summary>
        /// The id of the containing body.  This could be a regular
        /// object, another value type, or an array.  For obvious reasons,
        /// the containing body can never have both a FieldInfo and 
        /// an array index.
        /// </summary>
        private readonly long _containerID;

        /// <summary>
        /// The FieldInfo into the containing body.  This will only 
        /// apply if the containing body is a field info or another
        /// value type.
        /// </summary>
        private readonly FieldInfo _parentField;

        /// <summary>
        /// The array index of the index into the parent.  This will only
        /// apply if the containing body is an array.
        /// </summary>
        private readonly int[] _parentIndex;

        public ValueTypeFixupInfo(long containerID, FieldInfo member, int[] parentIndex)
        {
            // If both member and arrayIndex are null, we don't have enough information to create
            // a tunnel to do the fixup.
            if (member == null && parentIndex == null)
            {
                throw new ArgumentException(SR.Argument_MustSupplyParent);
            }

            if (containerID == 0 && member == null)
            {
                _containerID = containerID;
                _parentField = member;
                _parentIndex = parentIndex;
            }

            // If the member isn't null, we know that they supplied a MemberInfo as the parent.  This means
            // that the arrayIndex must be null because we can't have a FieldInfo into an array. 
            if (member != null)
            {
                if (parentIndex != null)
                {
                    throw new ArgumentException(SR.Argument_MemberAndArray);
                }

                if (member.FieldType.IsValueType && containerID == 0)
                {
                    throw new ArgumentException(SR.Argument_MustSupplyContainer);
                }
            }

            _containerID = containerID;
            _parentField = member;
            _parentIndex = parentIndex;
        }

        public long ContainerID => _containerID;

        public FieldInfo ParentField => _parentField;

        public int[] ParentIndex => _parentIndex;
    }
}
