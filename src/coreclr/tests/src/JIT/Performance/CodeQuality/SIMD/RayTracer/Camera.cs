// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

internal class Camera
{
    public Vector Pos;
    public Vector Forward;
    public Vector Up;
    public Vector Right;

    public Camera(Vector pos, Vector forward, Vector up, Vector right) { Pos = pos; Forward = forward; Up = up; Right = right; }

    public static Camera Create(Vector pos, Vector lookAt)
    {
        Vector forward = Vector.Norm(Vector.Minus(lookAt, pos));
        Vector down = new Vector(0, -1, 0);
        Vector right = Vector.Times(1.5F, Vector.Norm(Vector.Cross(forward, down)));
        Vector up = Vector.Times(1.5F, Vector.Norm(Vector.Cross(forward, right)));

        return new Camera(pos, forward, up, right);
    }
}

