// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

internal class ISect
{
    public SceneObject Thing;
    public Ray Ray;
    public double Dist;

    public ISect(SceneObject thing, Ray ray, double dist) { Thing = thing; Ray = ray; Dist = dist; }

    public static bool IsNull(ISect sect) { return sect == null; }
    public readonly static ISect Null = null;
}
