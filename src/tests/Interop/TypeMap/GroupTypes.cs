// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

[assembly: TypeMap<DefinedInLibrary>("local", typeof(string))]
[assembly: TypeMapAssociation<DefinedInLibrary>(typeof(object), typeof(string))]
[assembly: TypeMapAssemblyTarget<DefinedInLibrary>("TypeMapLib1")]
[assembly: TypeMapAssemblyTarget<DefinedInLibrary>("TypeMapLib3")]
[assembly: TypeMap<DefinedGenericGroup<int>>("generic", typeof(object))]
[assembly: TypeMapAssociation<DefinedGenericGroup<int>>(typeof(object), typeof(string))]
[assembly: TypeMap<DefinedInLibrary[]>("array", typeof(object))]
[assembly: TypeMapAssociation<DefinedInLibrary[]>(typeof(object), typeof(string))]
[assembly: TypeMap<DefinedProxyFirst>("local", typeof(string))]
[assembly: TypeMapAssociation<DefinedProxyFirst>(typeof(object), typeof(string))]
[assembly: TypeMapAssemblyTarget<UnusedDefinedGroup>("DoesNotExist")]
[assembly: TypeMapAssemblyTarget<DefinedMissingTargetGroup>("DoesNotExist")]
[assembly: TypeMap<DefinedDuplicateGroup>("duplicate", typeof(object))]

public class DefinedInLibrary { }
public class DefinedGenericGroup<T> { }
public class DefinedProxyFirst { }
public class DefinedEmptyGroup { }
public class UnusedDefinedGroup { }
public class DefinedMissingTargetGroup { }
public class DefinedDuplicateGroup { }

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

public class DuplicateTypeMapEntriesAcrossAssemblies { }

public class UnknownAssemblyReference { }

public class BlobOnlyAttributeTypeNames { }
