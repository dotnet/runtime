// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using Xunit;

public class GitHub_10621
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    static int F(int x) 
    { 
        return x * x;
    }

    // An empty try with nested try finallys where
    // the inner finally cannot be cloned.
    [Fact]
    public static int TestEntryPoint()
    {
        int x = 0;
        try {
            // empty
        }
        finally {
            try {
                for (int i = 0; i < 11; i++) {
                    x += F(i);
                }
            }
            finally {

                x -= 81;

                try {
                    // empty
                }
                finally
                {
                    x -= 64; 
                    try {
                        x -= 49;
                    }
                    finally {
                        try {
                            // empty
                        }
                        finally {
                            x -= 36;
                            try {
                                x -= 25;
                            }
                            finally {
                                try {
                                    // empty
                                }
                                finally
                                {
                                    x -= 16; 
                                    try {
                                        x -= 9;
                                    }
                                    finally {
                                        try {
                                            // empty
                                        }
                                        finally {
                                            x -= 4;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }

        return x - 1;
    }
}

