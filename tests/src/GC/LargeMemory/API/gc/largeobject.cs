// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;

//[Serializable()]
public sealed class LargeObject {

    private byte[][] data;
    private uint sizeInGB;
    private LargeObject next;
    public static int FinalizedCount = 0;

    public const long GB = 1024*1024*1024;

    public LargeObject(uint sizeInGB):this(sizeInGB, false)
    {
    }

    public LargeObject(uint sizeInGB, bool finalize) {
        this.sizeInGB = sizeInGB;

        if (!finalize) {
            GC.SuppressFinalize(this);
        }

        data = new byte[sizeInGB][];
        for (int i=0; i<sizeInGB; i++) {
            data[i] = new byte[GB];
        }
    }

    ~LargeObject() {
        Console.WriteLine("Finalized");
        FinalizedCount++;
    }

    public long Size {
        get {
            return sizeInGB*GB;
        }
    }

    public LargeObject Next {
        get { return next; }
        set { next = value; }
    }

}
