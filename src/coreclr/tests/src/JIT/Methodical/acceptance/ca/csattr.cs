// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

public enum SomeStuff { e1 = 1, e17 = 17 }

[System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Struct, AllowMultiple = true)]
public class Attr : System.Attribute
{
    public Attr() { name = "default"; num = 0; }

    public Attr(int[] up0)
    {
        name = "intgcarray";
        unnamed_list = up0[0].ToString();
    }
    public Attr(System.Type[] up0)
    {
        name = "typegcarray";
        unnamed_list = up0[0].ToString();
    }
    public Attr(int up0)
    {
        name = "int";
        unnamed_list = up0.ToString();
    }
    public Attr(int up0, float up1)
    {
        name = "int_float";
        unnamed_list = up0.ToString() + "\t|" + String.Format("{0:N2}", up1);
    }
    public Attr(SomeStuff up0)
    {
        name = "enum";
        unnamed_list = up0.ToString();
    }
    public Attr(String up0)
    {
        name = "String";
        unnamed_list = up0;
    }
    public Attr(int[] up0, System.Type[] up1, int up2, float up3, SomeStuff up4, String up5)
    {
        name = "multiple1";
        unnamed_list = up0[0].ToString() + "\t|" +
                up1[0].ToString() + "\t|" +
                up2.ToString() + "\t|" +
                String.Format("{0:N2}", up3) + "\t|" +
                up4.ToString() + "\t|" +
                up5;
    }
    public Attr(int[] up0, ulong up1, int up2, double up3, SomeStuff up4, String up5)
    {
        name = "multiple2";
        unnamed_list = up0[0].ToString() + "\t|" +
                up1.ToString() + "\t|" +
                up2.ToString() + "\t|" +
                String.Format("{0:N2}", up3) + "\t|" +
                up4.ToString() + "\t|" +
                up5;
    }

    public int[] np0;
    public String[] np1;
    public System.Type np2;
    public double np3;
    public String name;
    public int num;
    public String unnamed_list;
}