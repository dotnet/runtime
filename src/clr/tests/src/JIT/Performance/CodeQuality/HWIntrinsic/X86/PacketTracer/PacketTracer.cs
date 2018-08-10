// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System.Runtime.Intrinsics.X86;
using static System.Runtime.Intrinsics.X86.Avx;
using static System.Runtime.Intrinsics.X86.Avx2;
using static System.Runtime.Intrinsics.X86.Sse2;
using System.Runtime.Intrinsics;
using System;

using ColorPacket256 = VectorPacket256;

internal class Packet256Tracer
{
    public int Width { get; }
    public int Height { get; }
    private static readonly int MaxDepth = 5;

    private static readonly Vector256<float> SevenToZero = SetVector256(7f, 6f, 5f, 4f, 3f, 2f, 1f, 0f);

    public Packet256Tracer(int width, int height)
    {
        if ((width % VectorPacket256.Packet256Size) != 0)
        {
            width += VectorPacket256.Packet256Size - (width % VectorPacket256.Packet256Size);
        }
        Width = width;
        Height = height;
    }

    internal unsafe void RenderVectorized(Scene scene, int* rgb)
    {
        Camera camera = scene.Camera;
        // Iterate y then x in order to preserve cache locality.
        for (int y = 0; y < Height; y++)
        {
            int stride = y * Width;
            for (int x = 0; x < Width; x += VectorPacket256.Packet256Size)
            {
                float fx = x;
                Vector256<float> Xs = Add(SetAllVector256(fx), SevenToZero);
                var dirs = GetPoints(Xs, SetAllVector256<float>(y), camera);
                var rayPacket256 = new RayPacket256(camera.Pos, dirs);
                var SoAcolors = TraceRay(rayPacket256, scene, depth: 0);

                var AoS = SoAcolors.Transpose();
                var intAoS = AoS.ConvertToIntRGB();

                int* output = &rgb[(x + stride) * 3]; // Each pixel has 3 fields (RGB)
                {
                    Store(output, intAoS.Rs);
                    Store(output + 8, intAoS.Gs);
                    Store(output + 16, intAoS.Bs);
                }
                
            }
        }

    }

    private ColorPacket256 TraceRay(RayPacket256 rayPacket256, Scene scene, int depth)
    {
        var isect = MinIntersections(rayPacket256, scene);
        if (isect.AllNullIntersections())
        {
            return ColorPacket256Helper.BackgroundColor;
        }
        var color = Shade(isect, rayPacket256, scene, depth);
        var isNull = Compare(isect.Distances, Intersections.NullDistance, FloatComparisonMode.EqualOrderedNonSignaling);
        var backgroundColor = ColorPacket256Helper.BackgroundColor.Xs;
        return new ColorPacket256(BlendVariable(color.Xs, backgroundColor, isNull),
                                  BlendVariable(color.Ys, backgroundColor, isNull),
                                  BlendVariable(color.Zs, backgroundColor, isNull));
    }

    private Vector256<float> TestRay(RayPacket256 rayPacket256, Scene scene)
    {
        var isect = MinIntersections(rayPacket256, scene);
        if (isect.AllNullIntersections())
        {
            return SetZeroVector256<float>();
        }
        var isNull = Compare(isect.Distances, Intersections.NullDistance, FloatComparisonMode.EqualOrderedNonSignaling);
        return BlendVariable(isect.Distances, SetZeroVector256<float>(), isNull);
    }

    private Intersections MinIntersections(RayPacket256 rayPacket256, Scene scene)
    {
        Intersections mins = new Intersections(Intersections.NullDistance, Intersections.NullIndex);
        for (int i = 0; i < scene.Things.Length; i++)
        {
            Vector256<float> distance = scene.Things[i].Intersect(rayPacket256);

            if (!Intersections.AllNullIntersections(distance))
            {
                var notNullMask = Compare(distance, Intersections.NullDistance, FloatComparisonMode.NotEqualOrderedNonSignaling);
                var nullMinMask = Compare(mins.Distances, Intersections.NullDistance, FloatComparisonMode.EqualOrderedNonSignaling);

                var lessMinMask = Compare(mins.Distances, distance, FloatComparisonMode.GreaterThanOrderedNonSignaling);
                var minMask = And(notNullMask, Or(nullMinMask, lessMinMask));
                var minDis = BlendVariable(mins.Distances, distance, minMask);
                var minIndices = StaticCast<float, int>(BlendVariable(StaticCast<int, float>(mins.ThingIndices),
                                                                      StaticCast<int, float>(SetAllVector256<int>(i)),
                                                                      minMask));
                mins.Distances = minDis;
                mins.ThingIndices = minIndices;
            }
        }
        return mins;
    }

    private ColorPacket256 Shade(Intersections isect, RayPacket256 rays, Scene scene, int depth)
    {

        var ds = rays.Dirs;
        var pos = isect.Distances * ds + rays.Starts;
        var normals = scene.Normals(isect.ThingIndices, pos);
        var reflectDirs = ds - (Multiply(VectorPacket256.DotProduct(normals, ds), SetAllVector256<float>(2)) * normals);
        var colors = GetNaturalColor(isect.ThingIndices, pos, normals, reflectDirs, scene);

        if (depth >= MaxDepth)
        {
            return colors + new ColorPacket256(.5f, .5f, .5f);
        }

        return colors + GetReflectionColor(isect.ThingIndices, pos + (SetAllVector256<float>(0.001f) * reflectDirs), normals, reflectDirs, scene, depth);
    }

    private ColorPacket256 GetNaturalColor(Vector256<int> things, VectorPacket256 pos, VectorPacket256 norms, VectorPacket256 rds, Scene scene)
    {
        var colors = ColorPacket256Helper.DefaultColor;
        for (int i = 0; i < scene.Lights.Length; i++)
        {
            var lights = scene.Lights[i];
            var zero = SetZeroVector256<float>();
            var colorPacket = lights.Colors;
            var ldis = lights.Positions - pos;
            var livec = ldis.Normalize();
            var neatIsectDis = TestRay(new RayPacket256(pos, livec), scene);

            // is in shadow?
            var mask1 = Compare(neatIsectDis, ldis.Lengths, FloatComparisonMode.LessThanOrEqualOrderedNonSignaling);
            var mask2 = Compare(neatIsectDis, zero, FloatComparisonMode.NotEqualOrderedNonSignaling);
            var isInShadow = And(mask1, mask2);

            Vector256<float> illum = VectorPacket256.DotProduct(livec, norms);
            Vector256<float> illumGraterThanZero = Compare(illum, zero, FloatComparisonMode.GreaterThanOrderedNonSignaling);
            var tmpColor1 = illum * colorPacket;
            var defaultRGB = zero;
            Vector256<float> lcolorR = BlendVariable(defaultRGB, tmpColor1.Xs, illumGraterThanZero);
            Vector256<float> lcolorG = BlendVariable(defaultRGB, tmpColor1.Ys, illumGraterThanZero);
            Vector256<float> lcolorB = BlendVariable(defaultRGB, tmpColor1.Zs, illumGraterThanZero);
            ColorPacket256 lcolor = new ColorPacket256(lcolorR, lcolorG, lcolorB);

            Vector256<float> specular = VectorPacket256.DotProduct(livec, rds.Normalize());
            Vector256<float> specularGraterThanZero = Compare(specular, zero, FloatComparisonMode.GreaterThanOrderedNonSignaling);

            var difColor = new ColorPacket256(1, 1, 1);
            var splColor = new ColorPacket256(1, 1, 1);
            var roughness = SetAllVector256<float>(1);

            for (int j = 0; j < scene.Things.Length; j++)
            {
                Vector256<float> thingMask = StaticCast<int, float>(CompareEqual(things, SetAllVector256<int>(j)));
                var rgh = SetAllVector256<float>(scene.Things[j].Surface.Roughness);
                var dif = scene.Things[j].Surface.Diffuse(pos);
                var spl = scene.Things[j].Surface.Specular;

                roughness = BlendVariable(roughness, rgh, thingMask);

                difColor.Xs = BlendVariable(difColor.Xs, dif.Xs, thingMask);
                difColor.Ys = BlendVariable(difColor.Ys, dif.Ys, thingMask);
                difColor.Zs = BlendVariable(difColor.Zs, dif.Zs, thingMask);

                splColor.Xs = BlendVariable(splColor.Xs, spl.Xs, thingMask);
                splColor.Ys = BlendVariable(splColor.Ys, spl.Ys, thingMask);
                splColor.Zs = BlendVariable(splColor.Zs, spl.Zs, thingMask);
            }

            var tmpColor2 = VectorMath.Pow(specular, roughness) * colorPacket;
            Vector256<float> scolorR = BlendVariable(defaultRGB, tmpColor2.Xs, specularGraterThanZero);
            Vector256<float> scolorG = BlendVariable(defaultRGB, tmpColor2.Ys, specularGraterThanZero);
            Vector256<float> scolorB = BlendVariable(defaultRGB, tmpColor2.Zs, specularGraterThanZero);
            ColorPacket256 scolor = new ColorPacket256(scolorR, scolorG, scolorB);

            var oldColor = colors;

            colors = colors + ColorPacket256Helper.Times(difColor, lcolor) + ColorPacket256Helper.Times(splColor, scolor);

            colors = new ColorPacket256(BlendVariable(colors.Xs, oldColor.Xs, isInShadow), BlendVariable(colors.Ys, oldColor.Ys, isInShadow), BlendVariable(colors.Zs, oldColor.Zs, isInShadow));

        }
        return colors;
    }

    private ColorPacket256 GetReflectionColor(Vector256<int> things, VectorPacket256 pos, VectorPacket256 norms, VectorPacket256 rds, Scene scene, int depth)
    {
        return scene.Reflect(things, pos) * TraceRay(new RayPacket256(pos, rds), scene, depth + 1);
    }

    private readonly static Vector256<float> ConstTwo = SetAllVector256(2.0f);

    private VectorPacket256 GetPoints(Vector256<float> x, Vector256<float> y, Camera camera)
    {
        var widthVector = SetAllVector256<float>(Width);
        var heightVector = SetAllVector256<float>(Height);

        var widthRate1 = Divide(widthVector, ConstTwo);
        var widthRate2 = Multiply(widthVector, ConstTwo);

        var heightRate1 = Divide(heightVector, ConstTwo);
        var heightRate2 = Multiply(heightVector, ConstTwo);

        var recenteredX = Divide(Subtract(x, widthRate1), widthRate2);
        var recenteredY = Subtract(SetZeroVector256<float>(), Divide(Subtract(y, heightRate1), heightRate2));

        var result = camera.Forward + (recenteredX * camera.Right) + (recenteredY * camera.Up);

        return result.Normalize();
    }

    internal readonly Scene DefaultScene = CreateDefaultScene();

    private static Scene CreateDefaultScene()
    {
        ObjectPacket256[] things = {
            new SpherePacket256(new VectorPacket256(-0.5f, 1f, 1.5f), SetAllVector256(0.5f), Surfaces.MatteShiny),
            new SpherePacket256(new VectorPacket256(0f, 1f, -0.25f), SetAllVector256(1f), Surfaces.Shiny),
            new PlanePacket256((new VectorPacket256(0, 1, 0)), SetAllVector256(0f), Surfaces.CheckerBoard)
        };

        LightPacket256[] lights = {
            new LightPacket256(new Vector(-2f,2.5f,0f),new Color(.5f,.45f,.41f)),
            new LightPacket256(new Vector(2,4.5f,2), new Color(.99f,.95f,.8f))
        };

        Camera camera = Camera.Create(new VectorPacket256(2.75f, 2f, 3.75f), new VectorPacket256(-0.6f, .5f, 0f));

        return new Scene(things, lights, camera);
    }
}
