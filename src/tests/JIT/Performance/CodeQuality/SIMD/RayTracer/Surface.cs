// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;

internal class Surface
{
    public Func<Vector, Color> Diffuse;
    public Func<Vector, Color> Specular;
    public Func<Vector, double> Reflect;
    public double Roughness;

    public Surface(Func<Vector, Color> Diffuse,
                    Func<Vector, Color> Specular,
                    Func<Vector, double> Reflect,
                    double Roughness)
    {
        this.Diffuse = Diffuse;
        this.Specular = Specular;
        this.Reflect = Reflect;
        this.Roughness = Roughness;
    }
}

