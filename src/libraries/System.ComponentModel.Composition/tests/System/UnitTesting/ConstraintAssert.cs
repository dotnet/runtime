// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Primitives;
using System.Linq;
using System.Linq.Expressions;
using Xunit;

namespace System.UnitTesting
{
    internal static class ConstraintAssert
    {
        public static void Contains(Expression<Func<ExportDefinition, bool>> constraint, string contractName)
        {
            Contains(constraint, contractName, Enumerable.Empty<KeyValuePair<string, Type>>());
        }

        public static void Contains(Expression<Func<ExportDefinition, bool>> constraint, string contractName, IEnumerable<KeyValuePair<string, Type>> requiredMetadata)
        {
            string actualContractName;
            IEnumerable<KeyValuePair<string, Type>> actualRequiredMetadata;
            bool success = TryParseConstraint(constraint, out actualContractName, out actualRequiredMetadata);

            Assert.True(success);
            Assert.Equal(contractName, actualContractName);
            EnumerableAssert.AreEqual(requiredMetadata, actualRequiredMetadata);
        }

        private static bool TryParseConstraint(Expression<Func<ExportDefinition, bool>> constraint, out string contractName, out IEnumerable<KeyValuePair<string, Type>> requiredMetadata)
        {
            return ConstraintParser.TryParseConstraint(constraint, out contractName, out requiredMetadata);
        }
    }
}
