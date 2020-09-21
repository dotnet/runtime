// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    internal struct Currency
    {
        internal long m_value;

        // Constructs a Currency from a Decimal value.
        //
        public Currency(decimal value)
        {
            m_value = decimal.ToOACurrency(value);
        }
    }

    public partial struct Decimal
    {
        // Constructs a Decimal from a Currency value.
        //
        internal Decimal(Currency value)
        {
            this = FromOACurrency(value.m_value);
        }
    }
}
