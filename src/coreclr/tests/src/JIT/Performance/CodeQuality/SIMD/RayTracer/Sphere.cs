// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

using System;

internal class Sphere : SceneObject
{
    public Vector Center;
    public float Radius;

    public Sphere(Vector center, double radius, Surface surface) : base(surface) { Center = center; Radius = (float)radius; }

    public override ISect Intersect(Ray ray)
    {
        Vector eo = Vector.Minus(Center, ray.Start);
        float v = Vector.Dot(eo, ray.Dir);
        float dist;
        if (v < 0)
        {
            dist = 0;
        }
        else
        {
            double disc = Math.Pow(Radius, 2) - (Vector.Dot(eo, eo) - Math.Pow(v, 2));
            dist = disc < 0 ? 0 : v - (float)Math.Sqrt(disc);
        }
        if (dist == 0) return ISect.Null;
        return new ISect(this, ray, dist);
    }

    public override Vector Normal(Vector pos)
    {
        return Vector.Norm(Vector.Minus(pos, Center));
    }
}

