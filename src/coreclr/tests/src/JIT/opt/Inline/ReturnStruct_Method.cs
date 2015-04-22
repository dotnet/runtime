// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

public struct MyStruct
{
    public string str;
    public static MyStruct MakeString_Inline(string st)
    {
        MyStruct ss;
        ss.str = st;
        return ss;
    }

}

public class ReturnStruct
{
    public static int Main()
    {
        int iret = 100;
        MyStruct st = MyStruct.MakeString_Inline("Hello!");
        Console.WriteLine("st=" + st.str);
        return iret;
    }
}


