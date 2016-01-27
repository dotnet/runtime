// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System.Collections.Generic;

internal class Scene
{
    public SceneObject[] Things;
    public Light[] Lights;
    public Camera Camera;

    public Scene(SceneObject[] things, Light[] lights, Camera camera) { Things = things; Lights = lights; Camera = camera; }

    public IEnumerable<ISect> Intersect(Ray r)
    {
        foreach (SceneObject obj in Things)
        {
            yield return obj.Intersect(r);
        }
    }
}

