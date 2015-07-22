// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;


namespace System
{
    public sealed class DBNull
    {
        private DBNull()
        {
        }


        public static readonly DBNull Value = new DBNull();


        public override String ToString()
        {
            return String.Empty;
        }

        public String ToString(IFormatProvider provider)
        {
            return String.Empty;
        }
    }
}

