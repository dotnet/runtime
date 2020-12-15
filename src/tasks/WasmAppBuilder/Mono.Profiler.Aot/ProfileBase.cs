// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
namespace Mono.Profiler.Aot {
    public abstract class ProfileBase {

        internal enum RecordType {
            NONE = 0,
            IMAGE = 1,
            TYPE = 2,
            GINST = 3,
            METHOD = 4
        }

        internal enum MonoTypeEnum {
            MONO_TYPE_CLASS = 0x12,
        }

        internal const string MAGIC = "AOTPROFILE";
        internal const int MAJOR_VERSION = 1;
        internal const int MINOR_VERSION = 0;
    }
}
