// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Vec3f {
    public float x, y, z;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vec3f (float x, float y, float z = 0f) {
        this.x = x;
        this.y = y;
        this.z = z;
    }
}

public delegate void SceneObjectReader<T> (ref T obj, out float radius, out Vec3f center);

public interface ISceneObject {
}

public struct Sphere : ISceneObject {
    public Vec3f Center;
    public float Radius;
    public Vec3f Color;

    public static void Read (ref Sphere obj, out float radius, out Vec3f center) {
        center = obj.Center;
        radius = obj.Radius;
    }
}

public static unsafe class Raytrace {
    public const int BytesPerPixel = 4,
      width = 640, height = 480;

    private static byte[] FrameBuffer;
    private static Sphere[] Scene;

    // Convert a linear color value to a gamma-space int in [0, 255]
    // Square root approximates gamma-correct rendering.
    public static int l2gi (float v) {
        // sqrt, clamp to [0, 1], then scale to [0, 255] and truncate to int
        return (int)((MathF.Min(MathF.Max(MathF.Sqrt(v), 0.0f), 1.0f)) * 255.0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void vecStore (float x, float y, float z, ref Vec3f ptr) {
        ptr.x = x;
        ptr.y = y;
        ptr.z = z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void vecAdd (ref Vec3f a, ref Vec3f b, ref Vec3f ptr) {
        ptr.x = a.x + b.x;
        ptr.y = a.y + b.y;
        ptr.z = a.z + b.z;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void vecScale (ref Vec3f a, float scale, ref Vec3f ptr) {
        ptr.x = a.x * scale;
        ptr.y = a.y * scale;
        ptr.z = a.z * scale;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void vecNormalize (ref Vec3f ptr) {
        var x = ptr.x;
        var y = ptr.y;
        var z = ptr.z;

        float invLen = (1.0f / MathF.Sqrt((x * x) + (y * y) + (z * z)));
        ptr.x *= invLen;
        ptr.y *= invLen;
        ptr.z *= invLen;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float vecDot (ref Vec3f a, ref Vec3f b) {
        return (a.x * b.x) + (a.y * b.y) + (a.z * b.z);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float vecNLDot (ref Vec3f a, ref Vec3f b) {
        var value = vecDot(ref a, ref b);
        if (value < 0)
            return 0;
        else
            return value;
    }

    public static void sampleEnv (ref Vec3f dir, ref Vec3f ptr) {
        var y = dir.y;
        var amt = y * 0.5f + 0.5f;
        var keep = 1.0f - amt;
        vecStore(
          keep * 0.1f + amt * 0.1f,
          keep * 1.0f + amt * 0.1f,
          keep * 0.1f + amt * 1.0f,
          ref ptr
        );
    }

    public abstract class Intersector {
        public abstract void SetReader (Delegate d);
        public abstract unsafe bool Intersect (ref Vec3f pos, ref Vec3f dir, void* obj, ref Vec3f intersection_normal);
    }

    public class Intersector<T> : Intersector
        where T : unmanaged, ISceneObject {
        public SceneObjectReader<T> Reader;

        public override void SetReader (Delegate d) {
            Reader = (SceneObjectReader<T>)d;
        }

        public override bool Intersect (ref Vec3f pos, ref Vec3f dir, void* obj, ref Vec3f intersection_normal) {
            var so = Unsafe.AsRef<T>(obj);
            Reader(ref so, out var radius, out var center);
            return Intersect(ref pos, ref dir, radius, ref center, ref intersection_normal);
        }

        public bool Intersect (ref Vec3f pos, ref Vec3f dir, float radius, ref Vec3f center, ref Vec3f intersection_normal) {
            var vx = dir.x;
            var vy = dir.y;
            var vz = dir.z;

            // The sphere.
            var cx = center.x;
            var cy = center.y; // (float)Math.Sin(phase);
            var cz = center.z;

            // Calculate the position relative to the center of the sphere.
            var ox = pos.x - cx;
            var oy = pos.y - cy;
            var oz = pos.z - cz;

            var dot = vx * ox + vy * oy + vz * oz;

            var partial = dot * dot + radius * radius - (ox * ox + oy * oy + oz * oz);
            if (partial >= 0.0f) {
                var d = -dot - MathF.Sqrt(partial);

                if (d >= 0.0f) {
                    intersection_normal.x = pos.x + vx * d - cx;
                    intersection_normal.y = pos.y + vy * d - cy;
                    intersection_normal.z = pos.z + vz * d - cz;
                    vecNormalize(ref intersection_normal);
                    return true;
                }
            }

            return false;
        }
    }

    private static void renderPixel (int i, int j, ref Vec3f light, Intersector intersector) {
        var fb = FrameBuffer;
        var scene = Scene;

        var x = (float)(i) / (float)(width) - 0.5f;
        var y = 0.5f - (float)(j) / (float)(height);
        Vec3f pos = new Vec3f(x, y),
            dir = new Vec3f(x, y, -0.5f),
            half = default, intersection_normal = default,
            color = default;
        vecNormalize(ref dir);

        // Compute the half vector;
        vecScale(ref dir, -1.0f, ref half);
        vecAdd(ref half, ref light, ref half);
        vecNormalize(ref half);

        // Light accumulation
        var r = 0.0f;
        var g = 0.0f;
        var b = 0.0f;

        // Surface diffuse.
        var dr = 0.7f;
        var dg = 0.7f;
        var db = 0.7f;

        float hitZ = -999;
        bool didHitZ = false;
        for (int s = 0; s < scene.Length; s++) {
            var sphere = scene[s];

            if (didHitZ && (hitZ > sphere.Center.z))
                continue;

            if (intersector.Intersect(ref pos, ref dir, &sphere, ref intersection_normal)) {
                sampleEnv(ref intersection_normal, ref color);

                const float ambientScale = 0.2f;
                r = dr * color.x * ambientScale;
                g = dg * color.y * ambientScale;
                b = db * color.z * ambientScale;

                var diffuse = vecNLDot(ref intersection_normal, ref light);
                var specular = vecNLDot(ref intersection_normal, ref half);

                // Take it to the 64th power, manually.
                specular *= specular;
                specular *= specular;
                specular *= specular;
                specular *= specular;
                specular *= specular;
                specular *= specular;

                specular = specular * 0.6f;

                r += dr * (diffuse * sphere.Color.x) + specular;
                g += dg * (diffuse * sphere.Color.y) + specular;
                b += db * (diffuse * sphere.Color.z) + specular;
                // FIXME: Compute z of intersection point and check that instead
                hitZ = sphere.Center.z;
                didHitZ = true;
            }
        }

        if (!didHitZ) {
            sampleEnv(ref dir, ref color);
            r = color.x;
            g = color.y;
            b = color.z;
        }

        var index = (i + (j * width)) * BytesPerPixel;

        fb[index + 0] = (byte)l2gi(r);
        fb[index + 1] = (byte)l2gi(g);
        fb[index + 2] = (byte)l2gi(b);
        fb[index + 3] = 255;
    }

    public static byte[] renderFrame () {
        Vec3f light = default;
        vecStore(20.0f, 20.0f, 15.0f, ref light);
        vecNormalize(ref light);

        var reader = (SceneObjectReader<Sphere>)Sphere.Read;
        var tIntersector = typeof(Intersector<>).MakeGenericType(new[] { typeof(Sphere) });
        var intersector = (Intersector)Activator.CreateInstance(tIntersector);
        intersector.SetReader(reader);
        for (int j = 0; j < height; j++) {
            for (int i = 0; i < width; i++)
                renderPixel(i, j, ref light, intersector);
        }

        return FrameBuffer;
    }

    public static void init () {
        FrameBuffer = new byte[width * height * BytesPerPixel];
        var rng = new Random(1);
        const int count = 128;
        Scene = new Sphere[count];
        for (int i = 0; i < count; i++) {
            Scene[i] = new Sphere {
                Center = new Vec3f(
                    (rng.NextSingle() * 8f) - 5.5f,
                    (rng.NextSingle() * 8f) - 5.5f,
                    (rng.NextSingle() * -8f) - 2f
                ),
                Color = new Vec3f(
                    rng.NextSingle(),
                    rng.NextSingle(),
                    rng.NextSingle()
                ),
                Radius = (rng.NextSingle() * 0.85f) + 0.075f
            };
        }
    }
}

public static partial class Program {
    public static void Main () {
        Raytrace.init();
        Console.WriteLine("Hello, World!");
    }

    [JSImport("renderCanvas", "main.js")]
    static partial void RenderCanvas ([JSMarshalAs<JSType.MemoryView>] ArraySegment<byte> rgba);

    [JSExport]
    internal static void OnClick () {
        var now = DateTime.UtcNow;
        Console.WriteLine("Rendering started");

        var bytes = Raytrace.renderFrame();

        Console.WriteLine("Rendering finished in " + (DateTime.UtcNow - now).TotalMilliseconds + " ms");
        RenderCanvas(bytes);
    }
}
