# Inside the linker

The linker is quite a small piece of code, and it's pretty simple to address.
Its only dependency is `Mono.Cecil`, that is used to read, modify and write back
the assemblies.

Everything is located in the namespace Linker, or in sub namespaces.
Being a command line utility, its entry point function is in the class Driver.

This class is in charge of analyzing the command line, and instantiating two
important objects, a LinkContext, and a Pipeline.

The LinkContext contains all the information that will be used during the
trimming process, such as the assemblies involved, the output directory and
other useful things.

The Pipeline is simply a queue of actions (steps), to be applied to the current
context. The whole process of trimming is split into these different steps
that are all located in the Linker.Steps namespace.

Here are the current steps that are implemented, in the order they are used:

## ResolveFromAssembly or ResolveFromXml

These steps are used to initialize the context and pre-mark the root code
that will be used as a source for the linker.

Resolving from an assembly or resolving from XML descriptor is a decision
taken in the command line parsing.

## LoadReferences

This step will load all the references of all the assemblies involved in the
current context.

## Blacklist

This step is used if and only if you have specified that the code should be
linked. It will load XML descriptor resources from the participating assemblies. These describe which types and methods are necessary to be properly linked and not removed (for example because they are directly used from inside the runtime).

It is doing so by inserting a ResolveFromXml step per XML descriptor into the
pipeline.

## Mark

This is the most complex step. The linker will get from the context the list
of types, fields and methods that have been pre-marked in the resolve steps,
and walk through all of them. For every method, it will analyse the CIL stream,
to find references to other fields, types, or methods.

When it encounters such a reference, it will resolve the original definition of
this reference, and add this to the queue of items to be processed. For
instance, if a source assembly has a call to `Console.WriteLine`, the linker
will resolve the appropriate method `WriteLine` in the `System.Console` type from the
`System.Console` assembly, and add it to the queue. When this `WriteLine` method is
dequeued, and processed, the linker will go through everything that is used in
it, and add it to the queue, if it hasn't been processed already.

To know if something has been marked to be linked, or processed, the linker
is using functionality of Cecil called annotations. Almost everything in
Cecil can be annotated. Specifically, it means that almost everything owns a
hashtable in which you can add what you want, using the keys and the values you
want.

So the linker will annotate assemblies, types, methods and fields to know
what should be linked or not, what has been processed and how it should
process them.

This is useful as we don't have to recreate a full hierarchy of classes
to encapsulate the different Cecil types to add the few pieces of information we want.

## Sweep

This simple step will walk through all the elements of an assembly, and based
on their annotations, remove them or keep them.

## Clean

This step will clean parts of the assemblies, like properties. If a property
used to have a getter and a setter and after the mark & sweep steps
only the getter is retained it will update the property to reflect that.

There are a few things to keep clean like properties we've seen, events,
nested classes, and probably a few others.

## Output

For each assembly in the context, this step will act on the action associated
with the assembly. If the assembly is marked as skip, it won't do anything,
if it's marked as copy, it will copy the assembly to the output directory,
and if it's linked, it will save the modified assembly to the output directory.
