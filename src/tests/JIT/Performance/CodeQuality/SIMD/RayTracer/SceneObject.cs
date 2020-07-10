// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

internal abstract class SceneObject
{
    public Surface Surface;
    public abstract ISect Intersect(Ray ray);
    public abstract Vector Normal(Vector pos);

    public SceneObject(Surface surface) { Surface = surface; }
}

