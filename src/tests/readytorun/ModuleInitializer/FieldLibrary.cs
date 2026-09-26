// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace ModuleInitializerTest;

public sealed class FieldHolder
{
    private static bool s_isSet;

    public static bool IsSet => s_isSet;
}
