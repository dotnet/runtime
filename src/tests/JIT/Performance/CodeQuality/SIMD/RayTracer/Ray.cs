// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

internal struct Ray
{
    public Vector Start;
    public Vector Dir;

    public Ray(Vector start, Vector dir) { Start = start; Dir = dir; }
}

