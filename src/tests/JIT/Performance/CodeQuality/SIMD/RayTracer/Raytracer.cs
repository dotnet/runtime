// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Numerics;

internal sealed class RayTracer
{
    private int _screenWidth;
    private int _screenHeight;
    private const int MaxDepth = 5;

    public RayTracer(int screenWidth, int screenHeight)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
    }

    internal void RenderSequential(Scene scene, Int32[] rgb)
    {
        for (int y = 0; y < _screenHeight; y++)
        {
            int stride = y * _screenWidth;
            Camera camera = scene.Camera;
            for (int x = 0; x < _screenWidth; x++)
            {
                Color color = TraceRay(new Ray(camera.Pos, GetPoint(x, y, camera)), scene, 0);
                rgb[x + stride] = color.ToInt32();
            }
        }
    }

    internal void RenderParallel(Scene scene, Int32[] rgb, ParallelOptions options)
    {
        Parallel.For(0, _screenHeight, options, y =>
        {
            int stride = y * _screenWidth;
            Camera camera = scene.Camera;
            for (int x = 0; x < _screenWidth; x++)
            {
                Color color = TraceRay(new Ray(camera.Pos, GetPoint(x, y, camera)), scene, 0);
                rgb[x + stride] = color.ToInt32();
            }
        });
    }

    internal void RenderParallelShowingThreads(Scene scene, Int32[] rgb, ParallelOptions options)
    {
        int id = 0;
        Parallel.For<float>(0, _screenHeight, options, () => GetHueShift(Interlocked.Increment(ref id)), (y, state, hue) =>
        {
            int stride = y * _screenWidth;
            Camera camera = scene.Camera;
            for (int x = 0; x < _screenWidth; x++)
            {
                Color color = TraceRay(new Ray(camera.Pos, GetPoint(x, y, camera)), scene, 0);
                color.ChangeHue(hue);
                rgb[x + stride] = color.ToInt32();
            }
            return hue;
        },
        hue => Interlocked.Decrement(ref id));
    }

    public const int DefaultSeed = 20010415;
    public static int Seed = Environment.GetEnvironmentVariable("CORECLR_SEED") switch
    {
        string seedStr when seedStr.Equals("random", StringComparison.OrdinalIgnoreCase) => new Random().Next(),
        string seedStr when int.TryParse(seedStr, out int envSeed) => envSeed,
        _ => DefaultSeed
    };

    private Dictionary<int, float> _numToHueShiftLookup = new Dictionary<int, float>();
    private Random _rand = new Random(Seed);

    private float GetHueShift(int id)
    {
        float shift;
        lock (_numToHueShiftLookup)
        {
            if (!_numToHueShiftLookup.TryGetValue(id, out shift))
            {
                shift = (float)_rand.NextDouble();
                _numToHueShiftLookup.Add(id, shift);
            }
        }
        return shift;
    }

    internal readonly Scene DefaultScene = CreateDefaultScene();

    private static Scene CreateDefaultScene()
    {
        SceneObject[] things =  {
                new Sphere( new Vector(-0.5,1,1.5), 0.5, Surfaces.MatteShiny),
                new Sphere( new Vector(0,1,-0.25), 1, Surfaces.Shiny),
                new Plane( new Vector(0,1,0), 0, Surfaces.CheckerBoard)
            };
        Light[] lights = {
                new Light(new Vector(-2,2.5,0),new Color(.5,.45,.41)),
                new Light(new Vector(2,4.5,2), new Color(.99,.95,.8))
            };
        Camera camera = Camera.Create(new Vector(2.75, 2, 3.75), new Vector(-0.6, .5, 0));

        return new Scene(things, lights, camera);
    }


    private ISect MinIntersection(Ray ray, Scene scene)
    {
        ISect min = ISect.Null;
        foreach (SceneObject obj in scene.Things)
        {
            ISect isect = obj.Intersect(ray);
            if (!ISect.IsNull(isect))
            {
                if (ISect.IsNull(min) || min.Dist > isect.Dist)
                {
                    min = isect;
                }
            }
        }
        return min;
    }

    private double TestRay(Ray ray, Scene scene)
    {
        ISect isect = MinIntersection(ray, scene);
        if (ISect.IsNull(isect))
            return 0;
        return isect.Dist;
    }

    private Color TraceRay(Ray ray, Scene scene, int depth)
    {
        ISect isect = MinIntersection(ray, scene);
        if (ISect.IsNull(isect))
            return Color.Background;
        return Shade(isect, scene, depth);
    }

    private Color GetNaturalColor(SceneObject thing, Vector pos, Vector norm, Vector rd, Scene scene)
    {
        Color ret = new Color(0, 0, 0);
        foreach (Light light in scene.Lights)
        {
            Vector ldis = Vector.Minus(light.Pos, pos);
            Vector livec = Vector.Norm(ldis);
            double neatIsect = TestRay(new Ray(pos, livec), scene);
            bool isInShadow = !((neatIsect > Vector.Mag(ldis)) || (neatIsect == 0));
            if (!isInShadow)
            {
                float illum = Vector.Dot(livec, norm);
                Color lcolor = illum > 0 ? Color.Times(illum, light.Color) : new Color(0, 0, 0);
                float specular = Vector.Dot(livec, Vector.Norm(rd));
                Color scolor = specular > 0 ? Color.Times(Math.Pow(specular, thing.Surface.Roughness), light.Color) : new Color(0, 0, 0);
                ret = Color.Plus(ret, Color.Plus(Color.Times(thing.Surface.Diffuse(pos), lcolor),
                                                 Color.Times(thing.Surface.Specular(pos), scolor)));
            }
        }
        return ret;
    }

    private Color GetReflectionColor(SceneObject thing, Vector pos, Vector norm, Vector rd, Scene scene, int depth)
    {
        return Color.Times(thing.Surface.Reflect(pos), TraceRay(new Ray(pos, rd), scene, depth + 1));
    }

    private Color Shade(ISect isect, Scene scene, int depth)
    {
        Vector d = isect.Ray.Dir;
        Vector pos = Vector.Plus(Vector.Times(isect.Dist, isect.Ray.Dir), isect.Ray.Start);
        Vector normal = isect.Thing.Normal(pos);
        Vector reflectDir = Vector.Minus(d, Vector.Times(2 * Vector.Dot(normal, d), normal));
        Color ret = Color.DefaultColor;
        ret = Color.Plus(ret, GetNaturalColor(isect.Thing, pos, normal, reflectDir, scene));
        if (depth >= MaxDepth)
        {
            return Color.Plus(ret, new Color(.5, .5, .5));
        }
        return Color.Plus(ret, GetReflectionColor(isect.Thing, Vector.Plus(pos, Vector.Times(.001, reflectDir)), normal, reflectDir, scene, depth));
    }

    private double RecenterX(double x)
    {
        return (x - (_screenWidth / 2.0)) / (2.0 * _screenWidth);
    }
    private double RecenterY(double y)
    {
        return -(y - (_screenHeight / 2.0)) / (2.0 * _screenHeight);
    }

    private Vector GetPoint(double x, double y, Camera camera)
    {
        return Vector.Norm(Vector.Plus(camera.Forward, Vector.Plus(Vector.Times(RecenterX(x), camera.Right),
                                                                   Vector.Times(RecenterY(y), camera.Up))));
    }
}

