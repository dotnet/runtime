// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;

internal class Program
{
    private static int Main(string[] args)
    {
        return Test(null, null, null, 0, 1);
    }

    public static int Test(
    List<string> liste, List<string> unused1,
    string unused2, int unused3, long p_lFirstId)
    {
        liste = new List<string>();

        StringBuilder sbSql = new StringBuilder();

        for (int i = 0; i < 10; i++)
        {
            sbSql.Append(p_lFirstId);
            p_lFirstId++;

            foreach (string sColonne in liste)
            {
            }
        }

        System.Console.WriteLine(sbSql.ToString());
        if (sbSql.ToString() == "12345678910")
            return 100;
        return 101;
    }
}

