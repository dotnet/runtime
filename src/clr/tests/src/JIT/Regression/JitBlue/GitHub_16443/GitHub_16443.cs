// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Numerics;

class Program
{
    struct vec2
    {
        public Vector2 value;
        public vec2(float x, float y) => value = new Vector2(x, y);
    }

    static int Main()
    {
        var a = new vec2(0.42f, 0.24f);
        var b = new vec2(0.42f, 0.24f);
        return a.value.Equals(b.value) ? 100 : 1;
    }
}
