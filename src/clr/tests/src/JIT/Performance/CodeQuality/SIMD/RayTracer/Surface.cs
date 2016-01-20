// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
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

