using System;
using System.Collections.Generic;

class Program
{
    public abstract class NamedObject { }

    public class FooNamedObject : NamedObject { }

    public abstract class NamedObjectComponent { }

    public abstract class NamedObjectComponent<TNamedObject> : NamedObjectComponent where TNamedObject : NamedObject { }

    public class FooNamedObjectComponent : NamedObjectComponent<FooNamedObject> { }

    public abstract class SingleTypeNamedObjectContainer
    {
        internal SingleTypeNamedObjectContainer() { }
        internal abstract Type NamedObjectType { get; }
    }

    public class SingleTypeNamedObjectContainer<TNamedObject> : SingleTypeNamedObjectContainer
                  where TNamedObject : NamedObject
    {
        private readonly ComponentRegistry<NamedObjectComponent<TNamedObject>> components =
            new ComponentRegistry<NamedObjectComponent<TNamedObject>>();

        internal override Type NamedObjectType => typeof(TNamedObject);

        public void Register<TComponentA>()
            where TComponentA : NamedObjectComponent<TNamedObject>, new()
        {
            components.Register<TComponentA>();
        }
    }

    public class ComponentRegistry<TBaseComponent> where TBaseComponent : class
    {
        private readonly HashSet<Type> componentTypes = new HashSet<Type>();

        private readonly Dictionary<Type, Func<TBaseComponent>> componentFactories =
            new Dictionary<Type, Func<TBaseComponent>>();

        public void Register<TComponent>()
            where TComponent : class, TBaseComponent, new()
        {
            Register(() => new TComponent());
        }

        public void Register<TComponent>(Func<TComponent> componentFactory)
            where TComponent : class, TBaseComponent
        {
            componentTypes.Add(typeof(TComponent));
                componentFactories.Add(typeof(TComponent), componentFactory);
        }
    }

    public class NamedObjectContainer
    {
        private readonly Dictionary<Type, SingleTypeNamedObjectContainer> subcontainersByNamedObjectType =
            new Dictionary<Type, SingleTypeNamedObjectContainer>();

        private readonly Dictionary<Type, SingleTypeNamedObjectContainer> subcontainersByRegisteredType =
            new Dictionary<Type, SingleTypeNamedObjectContainer>();

        public SingleTypeNamedObjectContainer<TNamedObject> RegisterNamedObjectType<TNamedObject>()
            where TNamedObject : NamedObject
        {
            return RegisterSubcontainer(new SingleTypeNamedObjectContainer<TNamedObject>());
        }

        public TSubcontainer RegisterSubcontainer<TSubcontainer>(TSubcontainer subcontainer)
            where TSubcontainer : SingleTypeNamedObjectContainer
        {
            subcontainersByNamedObjectType.Add(subcontainer.NamedObjectType, subcontainer);
            subcontainersByRegisteredType.Add(typeof(TSubcontainer), subcontainer);
            return subcontainer;
        }

        internal void Register<TNamedObject, TComponent>()
            where TNamedObject : NamedObject
            where TComponent : NamedObjectComponent<TNamedObject>, new()
        {
            GetSubcontainerFor<TNamedObject>().Register<TComponent>();
        }

        public SingleTypeNamedObjectContainer<TNamedObject> GetSubcontainerFor<TNamedObject>()
            where TNamedObject : NamedObject
        {
            return (SingleTypeNamedObjectContainer<TNamedObject>)GetSubcontainerFor(typeof(TNamedObject));
        }

        public SingleTypeNamedObjectContainer GetSubcontainerFor(Type baseNamedObjectType)
        {
            return subcontainersByNamedObjectType[baseNamedObjectType];
        }
    }

        static int Main(string[] args)
        {
            var contaner = new NamedObjectContainer();
            contaner.RegisterNamedObjectType<FooNamedObject>();
            contaner.Register<FooNamedObject, FooNamedObjectComponent>();
            return 100;
        }		
    }