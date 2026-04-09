using System;
using System.Runtime.CompilerServices;
using Mono.Linker.Tests.Cases.Attributes.OnlyKeepUsed.Dependencies;

[assembly: TypeForwardedTo (typeof (UnusedAttributeWithTypeForwarderIsRemoved_LibAttribute))]
[assembly: TypeForwardedTo (typeof (UnusedAttributeWithTypeForwarderIsRemoved_OtherUsedClass))]
