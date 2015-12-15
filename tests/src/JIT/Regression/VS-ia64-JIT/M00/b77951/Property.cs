// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

