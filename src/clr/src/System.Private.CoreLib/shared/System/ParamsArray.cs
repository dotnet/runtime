// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System
{
    internal readonly struct ParamsArray
    {
        // Sentinel fixed-length arrays eliminate the need for a "count" field keeping this
        // struct down to just 4 fields. These are only used for their "Length" property,
        // that is, their elements are never set or referenced.
        private static readonly object[] s_oneArgArray = new object[1];
        private static readonly object[] s_twoArgArray = new object[2];
        private static readonly object[] s_threeArgArray = new object[3];

        private readonly object _arg0;
        private readonly object _arg1;
        private readonly object _arg2;

        // After construction, the first three elements of this array will never be accessed
        // because the indexer will retrieve those values from arg0, arg1, and arg2.
        private readonly object[] _args;

        public ParamsArray(object arg0)
        {
            _arg0 = arg0;
            _arg1 = null;
            _arg2 = null;

            // Always assign this.args to make use of its "Length" property
            _args = s_oneArgArray;
        }

        public ParamsArray(object arg0, object arg1)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = null;

            // Always assign this.args to make use of its "Length" property
            _args = s_twoArgArray;
        }

        public ParamsArray(object arg0, object arg1, object arg2)
        {
            _arg0 = arg0;
            _arg1 = arg1;
            _arg2 = arg2;

            // Always assign this.args to make use of its "Length" property
            _args = s_threeArgArray;
        }

        public ParamsArray(object[] args)
        {
            int len = args.Length;
            _arg0 = len > 0 ? args[0] : null;
            _arg1 = len > 1 ? args[1] : null;
            _arg2 = len > 2 ? args[2] : null;
            _args = args;
        }

        public int Length
        {
            get { return _args.Length; }
        }

        public object this[int index]
        {
            get { return index == 0 ? _arg0 : GetAtSlow(index); }
        }

        private object GetAtSlow(int index)
        {
            if (index == 1)
                return _arg1;
            if (index == 2)
                return _arg2;
            return _args[index];
        }
    }
}
