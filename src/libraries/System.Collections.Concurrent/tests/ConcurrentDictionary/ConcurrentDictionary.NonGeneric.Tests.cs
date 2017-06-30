// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Tests;
using System.Collections.Generic;

namespace System.Collections.Concurrent.Tests
{
    public class ConcurrentDictionary_NonGeneric_Tests : IDictionary_NonGeneric_Tests
    {
        #region IDictionary<TKey, TValue Helper Methods

        protected override IDictionary NonGenericIDictionaryFactory()
        {
            return new ConcurrentDictionary<string, string>();
        }

        /// <summary>
        /// Creates an object that is dependent on the seed given. The object may be either
        /// a value type or a reference type, chosen based on the value of the seed.
        /// </summary>
        protected override object CreateTKey(int seed)
        {
            int stringLength = seed % 10 + 5;
            Random rand = new Random(seed);
            byte[] bytes = new byte[stringLength];
            rand.NextBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        /// <summary>
        /// Creates an object that is dependent on the seed given. The object may be either
        /// a value type or a reference type, chosen based on the value of the seed.
        /// </summary>
        protected override object CreateTValue(int seed) => CreateTKey(seed);

        protected override IEnumerable<ModifyEnumerable> ModifyEnumerables => new List<ModifyEnumerable>();

        protected override bool Enumerator_Current_UndefinedOperation_Throws => false;

        protected override bool IDictionary_NonGeneric_Keys_Values_ModifyingTheDictionaryUpdatesTheCollection => false;

        protected override bool ICollection_NonGeneric_SupportsSyncRoot => false;

        protected override bool IDictionary_NonGeneric_Keys_Values_ParentDictionaryModifiedInvalidates => false;

        protected override bool ResetImplemented => false;

        protected override bool IDictionary_NonGeneric_Keys_Values_Enumeration_ResetImplemented => true;

        protected override bool SupportsSerialization => false;

        protected override Type ICollection_NonGeneric_CopyTo_ArrayOfIncorrectReferenceType_ThrowType => typeof(ArrayTypeMismatchException);

        #endregion
    }
}
