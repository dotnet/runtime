
using System;
using System.Diagnostics.CodeAnalysis;
using Mono.Linker.Tests.Cases.Expectations.Assertions;

namespace Mono.Linker.Tests.Cases.DataFlow
{
    [SkipKeptItemsValidation]
    [ExpectedNoWarnings]
    public class FieldKeyword
    {
        public static void Main()
        {
            WithMethods = typeof(FieldKeyword);
            WithFields = typeof(FieldKeyword);
            AssignNoneToMethods();
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods)]
        static Type WithMethods { get => field; set => field = value; }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        static Type WithFields { get => field; set => field = value; }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        static Type MismatchAssignedToField
        {
            // [method: ExpectedWarning("IL2074", nameof(WithNone))]
            get
            {
                field = WithNone;
                return field;
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        static Type MismatchAssignedFromField
        {
            [method: ExpectedWarning("IL2077", nameof(MismatchAssignedFromField), nameof(WithMethods))]
            get
            {
                WithMethods = field;
                return field;
            }
        }

        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)]
        static Type MismatchAssignedFromValue
        {
            [method: ExpectedWarning("IL2067", nameof(WithMethods))]
            set
            {
                WithMethods = value;
            }
        }

        static Type WithNone { get => field; set => field = value; }

        [ExpectedWarning("IL2072")]
        static void AssignNoneToMethods()
        {
            WithMethods = WithNone;
        }
    }
}
