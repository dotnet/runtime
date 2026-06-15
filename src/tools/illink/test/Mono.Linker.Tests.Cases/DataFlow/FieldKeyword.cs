// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [ExpectedNoWarnings]
    [Kept]
    public class FieldKeyword
    {
        [Kept]
        public static void Main()
        {
            WithMethods = typeof(FieldKeyword);
            WithFields = typeof(FieldKeyword);
            _ = WithNone;
            WithNone = null;
            _ = WithFields;
            _ = WithMethods;
            _ = MismatchAssignedFromField;
            _ = MismatchAssignedFromField_FieldAnnotated;
            _ = MismatchAssignedToField;
            _ = MismatchAssignedToField_FieldAnnotated;
            MismatchAssignedFromValue = null;
            AssignNoneToMethods();
        }

        [Kept]
        [KeptBackingField]
        static Type WithNone { [Kept] get => field; [Kept] set => field = value; }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods),
            KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
        [Kept]
        [KeptBackingField]
        static Type WithMethods
        {
            [Kept]
            [ExpectedWarning("IL2078", "return value", nameof(WithMethods), "BackingField")]
            get => field;
            [Kept]
            set => field = value;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields),
            KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
        [Kept]
        [KeptBackingField]
        static Type WithFields
        {
            [Kept]
            [ExpectedWarning("IL2078", "return value", nameof(WithFields), "BackingField")]
            get => field;
            [Kept]
            set => field = value;
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields),
            KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
        [Kept]
        [KeptBackingField]
        static Type MismatchAssignedToField
        {
            [ExpectedWarning("IL2078", "return value", nameof(MismatchAssignedToField))]
            [Kept]
            get
            {
                field = WithNone;
                return field;
            }
        }

        [field: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields),
            KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
        [Kept]
        [KeptBackingField]
        static Type MismatchAssignedToField_FieldAnnotated
        {
            [ExpectedWarning("IL2074", nameof(WithNone))]
            [Kept]
            [return: KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
            [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
            get
            {
                field = WithNone;
                return field;
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields),
            KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
        [Kept]
        [KeptBackingField]
        static Type MismatchAssignedFromField
        {
            [Kept]
            [ExpectedWarning("IL2077", nameof(MismatchAssignedFromField), nameof(WithMethods))]
            [ExpectedWarning("IL2078", "return value")]
            get
            {
                WithMethods = field;
                return field;
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields),
            KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
        [field: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        [Kept]
        [KeptBackingField]
        static Type MismatchAssignedFromField_FieldAnnotated
        {
            [Kept]
            [ExpectedWarning("IL2077", nameof(MismatchAssignedFromField), nameof(WithMethods))]
            get
            {
                WithMethods = field;
                return field;
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields),
            KeptAttributeAttribute(typeof(DynamicallyAccessedMembersAttribute))]
        [Kept]
        [KeptBackingField]
        static Type MismatchAssignedFromValue
        {
            [ExpectedWarning("IL2067", nameof(WithMethods))]
            [Kept]
            set
            {
                WithMethods = value;
                field = value;
            }
        }

        [ExpectedWarning("IL2072")]
        [Kept]
        static void AssignNoneToMethods()
        {
            WithMethods = WithNone;
        }
    }
}
