// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
//

internal class Plane : SceneObject
{
    public Vector Norm;
    public double Offset;

    public Plane(Vector norm, double offset, Surface surface) : base(surface) { Norm = norm; Offset = offset; }

    public override ISect Intersect(Ray ray)
    {
        double denom = Vector.Dot(Norm, ray.Dir);
        if (denom > 0) return ISect.Null;
        return new ISect(this, ray, (Vector.Dot(Norm, ray.Start) + Offset) / (-denom));
    }

    public override Vector Normal(Vector pos)
    {
        return Norm;
    }
}

