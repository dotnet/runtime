// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

public class C1
{
    public class I1 {}
    public class I2<T> {}
}

public struct S1 {}

public class C2<T>
{
    public class I1 {}
    public class I2<U> {}
}

public class TypicalUseCase { }

public class DuplicateTypeNameKey { }

public class InvalidTypeNameKey { }

public class MultipleTypeMapAssemblies { }

public class UnknownAssemblyReference { }
