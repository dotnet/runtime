// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
namespace System
{
    internal struct ParamsArray
    {
        // Sentinel fixed-length arrays eliminate the need for a "count" field keeping this
        // struct down to just 4 fields. These are only used for their "Length" property,
        // that is, their elements are never set or referenced.
        private static readonly object[] oneArgArray = new object[1];
        private static readonly object[] twoArgArray = new object[2];
        private static readonly object[] threeArgArray = new object[3];

        private readonly object arg0;
        private readonly object arg1;
        private readonly object arg2;

        // After construction, the first three elements of this array will never be accessed
        // because the indexer will retrieve those values from arg0, arg1, and arg2.
        private readonly object[] args;

        public ParamsArray(object arg0)
        {
            this.arg0 = arg0;
            this.arg1 = null;
            this.arg2 = null;

            // Always assign this.args to make use of its "Length" property
            this.args = oneArgArray;
        }

        public ParamsArray(object arg0, object arg1)
        {
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = null;

            // Always assign this.args to make use of its "Length" property
            this.args = twoArgArray;
        }

        public ParamsArray(object arg0, object arg1, object arg2)
        {
            this.arg0 = arg0;
            this.arg1 = arg1;
            this.arg2 = arg2;

            // Always assign this.args to make use of its "Length" property
            this.args = threeArgArray;
        }

        public ParamsArray(object[] args)
        {
            int len = args.Length;
            this.arg0 = len > 0 ? args[0] : null;
            this.arg1 = len > 1 ? args[1] : null;
            this.arg2 = len > 2 ? args[2] : null;
            this.args = args;
        }

        public int Length
        {
            get { return this.args.Length; }
        }

        public object this[int index]
        {
            get { return index == 0 ? this.arg0 : GetAtSlow(index); }
        }

        private object GetAtSlow(int index)
        {
            if (index == 1)
                return this.arg1;
            if (index == 2)
                return this.arg2;
            return this.args[index];
        }
    }
}
