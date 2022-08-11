// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*
 *  A Simple Framework to manage the life time of of objects
 * Objects are required to implement LifeTime Interface in order to keep track of their lifetime.
 * TODO: we need to add flexibility to the framework to control the type of datastructure used to keep track
 * of the objects. Right now we are using a simple 1 D array , but other interesting datastructures can be
 *    used instead like a HashTable.
 */
using System;
using System.Collections.Generic;

namespace LifeTimeFX
{
    public enum LifeTimeENUM
    {
        Short,
        Medium,
        Long
    }

    public interface LifeTime
    {
        LifeTimeENUM LifeTime
        {
            get;
            set;
        }

    }

    public interface LifeTimeStrategy
    {
        int  NextObject(LifeTimeENUM lifeTime);
        bool ShouldDie(LifeTime o, int index);
    }

    /// <summary>
    /// This interfact abstract the object contaienr , allowing us to specify differnt datastructures
    /// implementation.
    /// The only restriction on the ObjectContainer is that the objects contained in it must implement
    /// LifeTime interface.
    /// Right now we have a simple array container as a stock implementation for that. for more information
    /// see code:#ArrayContainer
    /// </summary>
    /// <param name="o"></param>
    /// <param name="index"></param>

    public interface ObjectContainer<T> where T:LifeTime
    {
        void Init(int numberOfObjects);
        void AddObjectAt(T o, int index);
        T GetObject(int index);
        T SetObjectAt(T o , int index);
        int Count
        {
            get;
        }
    }


    public sealed class BinaryTreeObjectContainer<T> : ObjectContainer<T> where T:LifeTime
    {

        class Node
        {
            public Node LeftChild;
            public Node RightChild;
            public int id;
            public T Data;

        }



        private Node root;
        private int count;

        public BinaryTreeObjectContainer()
        {
            root = null;
            count = 0;
        }

        public void Init(int numberOfObjects)
        {

            if (numberOfObjects<=0)
            {
                return;
            }

            root = new Node();
            root.id = 0;
            count = numberOfObjects;
            if (numberOfObjects>1)
            {
                int depth = (int)Math.Log(numberOfObjects,2)+1;

                root.LeftChild = CreateTree(depth-1, 1);
                root.RightChild = CreateTree(depth-1, 2);
            }


        }

        public void AddObjectAt(T o, int index)
        {
            Node node = Find(index);

            if (node!=null)
            {
                node.Data = o;
            }

        }


        public T GetObject(int index)
        {


            Node node = Find(index);

            if (node==null)
            {
                return default(T);
            }

            return node.Data;

        }

        public T SetObjectAt(T o , int index)
        {

            Node node = Find(index);

            if (node==null)
            {
                return default(T);
            }

            T old = node.Data;
            node.Data = o;
            return old;

        }

        public int Count
        {
            get
            {
                return count;
            }
        }



        private Node CreateTree(int depth, int id)
        {
            if (depth<=0)
            {
                return null;
            }

            Node node = new Node();
            node.id = id;
            node.LeftChild = CreateTree(depth-1, id*2+1);
            node.RightChild = CreateTree(depth-1, id*2+2);

            return node;
        }

        private Node Find(int id)
        {

            List<int> path = new List<int>();

            // find the path from node to root
            int n=id;
            while (n>0)
            {
                path.Add(n);
                n = (int)Math.Ceiling( ((double)n/2.0) ) - 1;
            }

            // follow the path from root to node
            Node node = root;
            for (int i=path.Count-1; i>=0; i--)
            {
                if (path[i]==(id*2+1))
                {
                    node = node.LeftChild;
                }
                else
                {
                    node = node.RightChild;
                }

            }

            return node;
        }

    }



//#ArrayContainer Simple Array Stock Implementation for ObjectContainer
    public sealed class ArrayObjectContainer<T> : ObjectContainer<T> where T:LifeTime
    {
        private T[] objContainer = null;
        public void Init(int numberOfObjects)
        {
            objContainer = new T[numberOfObjects];

        }

        public void AddObjectAt(T o, int index)
        {
            objContainer[index] = o;
        }

        public T GetObject(int index)
        {
            return  objContainer[index];
        }

        public T SetObjectAt(T o, int index)
        {
            T old = objContainer[index];
            objContainer[index] = o;
            return old;
        }

        public int Count
        {
            get
            {
                return objContainer.Length;
            }
        }
    }



    public delegate void ObjectDiedEventHandler(LifeTime o, int index );

    public sealed class ObjectLifeTimeManager
    {
        private LifeTimeStrategy strategy;

        private ObjectContainer<LifeTime> objectContainer = null;
       //

        public void SetObjectContainer (ObjectContainer<LifeTime> objectContainer)
        {
            this.objectContainer = objectContainer;
        }

        public event ObjectDiedEventHandler objectDied;

        public void Init(int numberObjects)
        {
            objectContainer.Init(numberObjects);
            //objContainer = new object[numberObjects];
        }

        public LifeTimeStrategy LifeTimeStrategy
        {
            set
            {
                strategy = value;
            }
        }

        public void AddObject(LifeTime o, int index)
        {
            objectContainer.AddObjectAt(o, index);
            //objContainer[index] = o;
        }

        public void Run()
        {


            LifeTime objLifeTime;

            for (int i = 0; i < objectContainer.Count; ++i)
            {
                objLifeTime = objectContainer.GetObject(i);
                //object o = objContainer[i];
                //objLifeTime = o as LifeTime;

                if (strategy.ShouldDie(objLifeTime, i))
                {
                    int index = strategy.NextObject(objLifeTime.LifeTime);
                    LifeTime oldObject  = objectContainer.SetObjectAt(null, index);
                    //objContainer[index] = null;
                    // fire the event
                    objectDied(oldObject, index);
                }

            }
        }
    }
}
