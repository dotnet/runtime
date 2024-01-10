// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//Dev10 bug #512868: Invalid context used for generic types during constraint verification leads to BadImageFormatException

using System;
using Xunit;


public class GenericNode
{
    public GenericNode()
    {
    }
}

public interface IFactory<TNode>
    where TNode : GenericNode
{
}

public static class FactoryGenerator<TNode>
    where TNode : GenericNode
{
    public static IFactory<TNode> Instance
    {
        get { return null; }
    }
}

public class ItemX : InternalItemServices<ContainerX, ItemX>
{
    public ItemX()
        : base()
    {
    }
}

public class ContainerX : InternalContainerServices<ContainerX, ItemX>
{
    public ContainerX()
        : base()
    {
    }
}

public abstract class InternalItemServices<TContainer, TItem> : ExternalItemServices<TContainer, TItem>
    where TContainer : GenericNode
    where TItem : InternalItemServices<TContainer, TItem>
{
    protected InternalItemServices()
        : base(FactoryGenerator<TContainer>.Instance, FactoryGenerator<TItem>.Instance)
    {

    }
}

public abstract class ExternalItemServices<TContainer, TItem> : GenericNode
    where TContainer : GenericNode
    where TItem : GenericNode
{
    protected ExternalItemServices(IFactory<TContainer> containerFactory, IFactory<TItem> itemFactory)
        : base()
    {
    }
}

public abstract class ExternalContainerServices<TContainer, TItem> : GenericNode
    where TContainer : GenericNode
    where TItem : ExternalItemServices<TContainer, TItem>
{
    protected ExternalContainerServices(IFactory<TItem> itemFactory)
        : base()
    {
    }
}
public abstract class InternalContainerServices<TContainer, TItem> : ExternalContainerServices<TContainer, TItem>
    where TContainer : GenericNode
    where TItem : ExternalItemServices<TContainer, TItem>
{
    protected InternalContainerServices()
        : base(FactoryGenerator<TItem>.Instance)
    {
    }
}

public class Test
{
    [Fact]
    public static void TestEntryPoint()
    {
        ItemX treeItem = new ItemX();

        Console.WriteLine("Item: {0}", treeItem);
    }
}


// The inheritance relationships here are a little hard to follow, however
// they are valid and in fact map to conceivable usage models.
//
// Consider a tree structure with the following properties:
//
//  - All tree elements derive from a GenericNode type.
//
//  - The fundamental tree element is an `item'.  The children of an item can
//    be other items or containers.
//
//  - The children of `containers' can only be items, which can in turn have
//    additional children of item or container type.
//
// Say that each tree instance is built using exactly one item type and
// exactly one corresponding container type.  This means the tree structure is
// parameterized on the <container, item> pair.
//
// The hierarchy shape in this testcase could be used to provide services for
// a <ContainerX, ItemX> instance of the tree.  
// ------------
//
// // Generates objects of type TNode.
// IFactory<TNode>
//     where TNode: GenericNode
//
//
// // Static class exposing a property returning a factory interface for a
// // specific GenericNode subtype.
// FactoryGenerator<TNode>
//     where TNode: GenericNode
//
//   IFactory<TNode> Factory { get; }
//
//
// // Implements this item, inheriting base types parameterized on an arbitrary
// // <container, item> pair that implement common operations that can be applied
// // to the specified item in the context of that pair.
// ItemX : InternalItemServices<ContainerX, ItemX>
//
//     // Implements one level of item services.  The item type is constrained to
//     // be a subclass of this type.  This might be useful for various reasons
//     // (e.g., so that routines here can access private InternalItemServices
//     // members in any TItem objects that are passed in).
//     InternalItemServices<TContainer, TItem> : ExternalItemServices<TContainer, TItem>
//         where TContainer: GenericNode
//         where TItem: InternalItemServices<TContainer, TItem> (i.e., is the type of a subclass)
//
//       // Pass factories to the base class for both the container and item
//       // types.
//       .ctor() : base(FactoryGenerator<TContainer>.Factory, FactoryGenerator<TItem>.Factory)
//
//         // Implements the next level of item services, leveraging container
//         // and item factories passed to the .ctor (perhaps to generate child
//         // nodes).
//         ExternalItemServices<TContainer, TItem> : GenericNode
//             where TContainer: GenericNode
//             where TItem: GenericNode
//
//           .ctor(IFactory<TContainer> useToBuildChildContainers,
//                 IFactory<TItem> useToBuildChildItems)
//
//
// // Implements this container, inheriting base types parameterized on an arbitrary
// // <container, item> pair that implement common operations that can be applied
// // to the specified container in the context of that pair.
// ContainerX : InternalContainerServices<ContainerX, ItemX>
//
//     // Implements one level of container services.  The item type is
//     // constrained in a manner forcing it to in fact be an item type that
//     // includes the common item services supplied for this <container, item>
//     // pair.
//     InternalContainerServices<TContainer, TItem> : ExternalContainerServices<TContainer, TItem>
//         where TContainer: GenericNode
//         where TItem: ExternalItemServices<TContainer, TItem>
//
//       // Pass an item factory to the base class.
//       .ctor() : base(FactoryGenerator<TItem>.Factory)
//
//         // Implements the next level of container services, leveraging an item
//         // factory passed to the .ctor (perhaps to generate child nodes).
//         //
//         // The item type is again constrained in a manner forcing it to in
//         // fact be an item type that includes the common item services
//         // supplied for this <container, item> pair.
//         ExternalContainerServices<TContainer, TItem> : GenericNode
//             where TContainer: GenericNode
//             where TItem: ExternalItemServices<TContainer, TItem>
//
//           .ctor(IFactory<TItem> useToBuildChildItems)
//
