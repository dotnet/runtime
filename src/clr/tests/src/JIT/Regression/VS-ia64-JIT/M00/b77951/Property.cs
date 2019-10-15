// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

public class Property
{
    public const int NULLDATA = 999999;
    private int m_data = NULLDATA;

    public Property()
    {
        m_data = NULLDATA;
    }

    public Property(int value)
    {
        m_data = value;
    }

    public int Item
    {
        get
        {
            return m_data;
        }

        set
        {
            m_data = value;
        }
    }
}

