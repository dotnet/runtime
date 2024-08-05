# Memory Management

Metadata memory management
--------------------------

Most metadata structures have a lifetime which is equal to the MonoImage where they are loaded from. These structures should be allocated from the memory pool of the corresponding MonoImage. The memory pool is protected by the loader lock. Examples of metadata structures in this category:

-   MonoClass
-   MonoMethod
-   MonoType

Memory owned by these structures should be allocated from the image mempool as well. Examples include: klass-\>methods, klass-\>fields, method-\>signature etc.

Generics complicates things. A generic class could have many instantinations where the generic arguments are from different assemblies. Where should we allocate memory for instantinations ? We can allocate from the mempool of the image which contains the generic type definition, but that would mean that the instantinations would remain in memory even after the assemblies containing their type arguments are unloaded, leading to a memory leak. Therefore, we do the following:

-   data structures representing the generic definitions are allocated from the image mempool as usual. These include:

<!-- -->

     * generic class definition (MonoGenericClass->container_class)
     * generic method definitions
     * type parameters (MonoGenericParam)

-   data structures representing inflated classes/images are allocated from the heap. They are owned by an 'image-set' which is the set of all images they depend on. When an image is unloaded, all image-sets it belongs to are freed, causing the data structures owned by the image-sets to be freed too. The structures handled this way include:

<!-- -->

     * MonoGenericClass
     * MonoGenericInst
     * inflated MonoMethods

[Original version of this document in git.](https://github.com/mono/mono/blob/425844619cbce18eaa64205b9007f0c833e4a5c4/docs/memory-management.txt)

Memory management for executable code
-------------------------------------

Executable code is managed using 'code-managers', whose implementation is in utils/mono-codeman.{h,c}. These allow the allocation of memory which is suitable for storing executable code, i.e.:

-   It has the required executable (x) permission.
-   The alignment of the memory blocks allocated from the code manager matches the preferred function alignment of the platform.

Code managers also allow a certain percent of the memory they manage to be reserved for storing things like function thunks.

The runtime contains the following code managers:

-   There is a global code manager declared in mini.c which is used to manage code memory whose lifetime is equal to the lifetime of the runtime. Memory for trampolines is allocated from the global code manager.
-   Every domain has a code manager which is used for allocating memory used by JITted code belonging to that domain.
-   Every 'dynamic' method, i.e. a method whose lifetime is not equal to the runtime or a domain, has its own code manager.
