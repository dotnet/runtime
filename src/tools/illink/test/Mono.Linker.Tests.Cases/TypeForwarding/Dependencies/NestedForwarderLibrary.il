.assembly extern mscorlib
{
}

.assembly extern Implementation
{
}

.assembly NestedForwarderLibrary
{
}

.class extern forwarder Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationLibrary
{
  .assembly extern 'Implementation'
}
.class extern ForwardedNestedType
{
  .class extern 'Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.ImplementationLibrary'
}

.class extern forwarder Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.AnotherImplementationClass
{
  .assembly extern 'Implementation'
}
.class extern ForwardedNestedType
{
  .class extern 'Mono.Linker.Tests.Cases.TypeForwarding.Dependencies.AnotherImplementationClass'
}

.module 'NestedForwarderLibrary.dll'
