.assembly extern mscorlib
{
}

.assembly extern NestedForwarderLibrary
{
}

.assembly NestedForwarderLibrary_2
{
}

.class extern forwarder Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationLibrary
{
  .assembly extern 'NestedForwarderLibrary'
}
.class extern ForwardedNestedType
{
  .class extern 'Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationLibrary'
}

.class extern forwarder Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.AnotherImplementationClass
{
  .assembly extern 'NestedForwarderLibrary'
}
.class extern ForwardedNestedType
{
  .class extern 'Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.AnotherImplementationClass'
}

.module 'NestedForwarderLibrary_2.dll'
