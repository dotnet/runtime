// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

internal struct Ray
{
    public Vector Start;
    public Vector Dir;

    public Ray(Vector start, Vector dir) { Start = start; Dir = dir; }
}

