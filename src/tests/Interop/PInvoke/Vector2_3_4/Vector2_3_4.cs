// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Xunit;

public class Vector2_3_4Test
{
    private const int StartingIntValue = 42;
    private const int NewIntValue = 18;

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/91388", typeof(TestLibrary.PlatformDetection), nameof(TestLibrary.PlatformDetection.PlatformDoesNotSupportNativeTestAssets))]
    public static void RunVector2Tests()
    {
        Console.WriteLine($"Running {nameof(RunVector2Tests)}... ");
        float X = StartingIntValue;
        float Y = StartingIntValue + 1;
        float Z = StartingIntValue + 232;
        float W = StartingIntValue + 93719;

        float XNew = 71;
        float YNew = 999;
        float ZNew = 1203;
        float WNew = 4;

        Vector2 startingVector = new Vector2(X, Y);
        Vector2 newVector = new Vector2(XNew, YNew);

        Assert.Equal(startingVector, Vector2_3_4TestNative.CreateVector2FromFloats(X, Y));

        Assert.True(Vector2_3_4TestNative.Vector2EqualToFloats(startingVector, X, Y));

        Vector2 localVector = startingVector;
        Assert.True(Vector2_3_4TestNative.ValidateAndChangeVector2(ref localVector, X, Y, XNew, YNew));
        Assert.Equal(newVector, localVector);

        Vector2_3_4TestNative.GetVector2ForFloats(X, Y, out var vec);
        Assert.Equal(startingVector, vec);

        Assert.Equal(startingVector, Vector2_3_4TestNative.CreateWrappedVector2FromFloats(X, Y).vec);

        Assert.True(Vector2_3_4TestNative.WrappedVector2EqualToFloats(new Vector2_3_4TestNative.Vector2Wrapper { vec = startingVector }, X, Y));

        var localVectorWrapper = new Vector2_3_4TestNative.Vector2Wrapper { vec = startingVector };
        Assert.True(Vector2_3_4TestNative.ValidateAndChangeWrappedVector2(ref localVectorWrapper, X, Y, XNew, YNew));
        Assert.Equal(newVector, localVectorWrapper.vec);

        Assert.Equal(newVector, Vector2_3_4TestNative.PassThroughVector2ToCallback(startingVector, vectorParam =>
        {
            Assert.Equal(startingVector, vectorParam);
            return newVector;
        }));
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/93669", TestPlatforms.Browser)]
    public static void RunVector3Tests()
    {
        Console.WriteLine($"Running {nameof(RunVector3Tests)}... ");
        float X = StartingIntValue;
        float Y = StartingIntValue + 1;
        float Z = StartingIntValue + 232;
        float W = StartingIntValue + 93719;

        float XNew = 71;
        float YNew = 999;
        float ZNew = 1203;
        float WNew = 4;

        Vector3 startingVector = new Vector3(X, Y, Z);
        Vector3 newVector = new Vector3(XNew, YNew, ZNew);

        Assert.Equal(startingVector, Vector2_3_4TestNative.CreateVector3FromFloats(X, Y, Z));

        Assert.True(Vector2_3_4TestNative.Vector3EqualToFloats(startingVector, X, Y, Z));

        Vector3 localVector = startingVector;
        Assert.True(Vector2_3_4TestNative.ValidateAndChangeVector3(ref localVector, X, Y, Z, XNew, YNew, ZNew));
        Assert.Equal(newVector, localVector);

        Vector2_3_4TestNative.GetVector3ForFloats(X, Y, Z, out var vec);
        Assert.Equal(startingVector, vec);

        Assert.Equal(startingVector, Vector2_3_4TestNative.CreateWrappedVector3FromFloats(X, Y, Z).vec);

        Assert.True(Vector2_3_4TestNative.WrappedVector3EqualToFloats(new Vector2_3_4TestNative.Vector3Wrapper { vec = startingVector }, X, Y, Z));

        var localVectorWrapper = new Vector2_3_4TestNative.Vector3Wrapper { vec = startingVector };
        Assert.True(Vector2_3_4TestNative.ValidateAndChangeWrappedVector3(ref localVectorWrapper, X, Y, Z, XNew, YNew, ZNew));
        Assert.Equal(newVector, localVectorWrapper.vec);

        Assert.Equal(newVector, Vector2_3_4TestNative.PassThroughVector3ToCallback(startingVector, vectorParam =>
        {
            Assert.Equal(startingVector, vectorParam);
            return newVector;
        }));
    }

    [Fact]
    [ActiveIssue("https://github.com/dotnet/runtime/issues/93669", TestPlatforms.Browser)]
    public static void RunVector4Tests()
    {
        Console.WriteLine($"Running {nameof(RunVector4Tests)}... ");
        float X = StartingIntValue;
        float Y = StartingIntValue + 1;
        float Z = StartingIntValue + 232;
        float W = StartingIntValue + 93719;

        float XNew = 71;
        float YNew = 999;
        float ZNew = 1203;
        float WNew = 4;

        Vector4 startingVector = new Vector4(X, Y, Z, W);
        Vector4 newVector = new Vector4(XNew, YNew, ZNew, WNew);

        Assert.Equal(startingVector, Vector2_3_4TestNative.CreateVector4FromFloats(X, Y, Z, W));

        Assert.True(Vector2_3_4TestNative.Vector4EqualToFloats(startingVector, X, Y, Z, W));

        Vector4 localVector = startingVector;
        Assert.True(Vector2_3_4TestNative.ValidateAndChangeVector4(ref localVector, X, Y, Z, W, XNew, YNew, ZNew, WNew));
        Assert.Equal(newVector, localVector);

        Vector2_3_4TestNative.GetVector4ForFloats(X, Y, Z, W, out var vec);
        Assert.Equal(startingVector, vec);

        Assert.Equal(startingVector, Vector2_3_4TestNative.CreateWrappedVector4FromFloats(X, Y, Z, W).vec);

        Assert.True(Vector2_3_4TestNative.WrappedVector4EqualToFloats(new Vector2_3_4TestNative.Vector4Wrapper { vec = startingVector }, X, Y, Z, W));

        var localVectorWrapper = new Vector2_3_4TestNative.Vector4Wrapper { vec = startingVector };
        Assert.True(Vector2_3_4TestNative.ValidateAndChangeWrappedVector4(ref localVectorWrapper, X, Y, Z, W, XNew, YNew, ZNew, WNew));
        Assert.Equal(newVector, localVectorWrapper.vec);

        Assert.Equal(newVector, Vector2_3_4TestNative.PassThroughVector4ToCallback(startingVector, vectorParam =>
        {
            Assert.Equal(startingVector, vectorParam);
            return newVector;
        }));
    }
}
