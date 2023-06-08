// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using Xunit;
public class _74773 {
    static string s_string_16 = "Q57IY";
    bool bool_23 = false;
    string string_33 = "";
    static int s_loopInvariant = 1;
    public string LeafMethod10() {
        unchecked {
            return s_string_16 = string_33;
        }
    }

    internal void Method3() {
        unchecked {
            int __loopvar3 = s_loopInvariant;
            do {
                if (__loopvar3 > 15 + 4)
                    break;
                ;
            }
            while (bool_23 && LeafMethod10() == string_33);
        }
    }

    [Fact]
    public static int TestEntryPoint() {
        new _74773().Method3();
        return 100;
    }
}
