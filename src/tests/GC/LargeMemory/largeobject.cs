// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

//[Serializable()]
public sealed class LargeObject {

    private byte[][] data;
    private uint sizeInMB;
    private LargeObject next;
    public static int FinalizedCount = 0;

    public const long MB = 1024*1024;

    public LargeObject(uint sizeInMB):this(sizeInMB, false)
    {
    }

    public LargeObject(uint sizeInMB, bool finalize) {
        this.sizeInMB = sizeInMB;

        if (!finalize) {
            GC.SuppressFinalize(this);
        }

        data = new byte[sizeInMB][];
        for (int i=0; i<sizeInMB; i++) {
            data[i] = new byte[MB];
        }
    }

    ~LargeObject() {
        Console.WriteLine("Finalized");
        FinalizedCount++;
    }

    public long Size {
        get {
            return sizeInMB*MB;
        }
    }

    public LargeObject Next {
        get { return next; }
        set { next = value; }
    }

}
