// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace System.Net.Http.Headers
{
    internal class Int64NumberHeaderParser : BaseHeaderParser
    {
        // Note that we don't need a custom comparer even though we have a value type that gets boxed (comparing two
        // equal boxed value types returns 'false' since the object instances used for boxing the two values are 
        // different). The reason is that the comparer is only used by HttpHeaders when comparing values in a collection.
        // Value types are never used in collections (in fact HttpHeaderValueCollection expects T to be a reference
        // type).

        internal static readonly Int64NumberHeaderParser Parser = new Int64NumberHeaderParser();

        private Int64NumberHeaderParser()
            : base(false)
        {
        }

        public override string ToString(object value)
        {
            Debug.Assert(value is long);

            return ((long)value).ToString(NumberFormatInfo.InvariantInfo);
        }

        protected override int GetParsedValueLength(string value, int startIndex, object storeValue,
            out object parsedValue)
        {
            parsedValue = null;

            int numberLength = HttpRuleParser.GetNumberLength(value, startIndex, false);

            if ((numberLength == 0) || (numberLength > HttpRuleParser.MaxInt64Digits))
            {
                return 0;
            }

            long result = 0;
            if (!HeaderUtilities.TryParseInt64(value.Substring(startIndex, numberLength), out result))
            {
                return 0;
            }

            parsedValue = result;
            return numberLength;
        }
    }
}
