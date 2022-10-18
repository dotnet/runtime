// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System;
using System.Collections.Generic;

namespace LibObjectFile
{
    public static class ObjectFileExtensions
    {
        /// <summary>
        /// Adds an attribute to <see cref="Attributes"/>.
        /// </summary>
        /// <param name="element">A attribute</param>
        public static void Add<TParent, TChild>(this List<TChild> list, TParent parent, TChild element) where TChild : ObjectFileNode where TParent: ObjectFileNode
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Parent != null)
            {
                if (element.Parent == parent) throw new InvalidOperationException($"Cannot add the {element.GetType()} as it is already added");
                if (element.Parent != parent) throw new InvalidOperationException($"Cannot add the {element.GetType()}  as it is already added to another {parent.GetType()} instance");
            }

            element.Parent = parent;
            element.Index = (uint)list.Count;
            list.Add(element);
        }

        /// <summary>
        /// Adds an element to the sorted list
        /// </summary>
        /// <param name="element">An element to add</param>
        public static void AddSorted<TParent, TChild>(this List<TChild> list, TParent parent, TChild element, bool requiresUnique = false) where TChild : ObjectFileNode, IComparable<TChild> where TParent : ObjectFileNode 
        {
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Parent != null)
            {
                if (element.Parent == parent) throw new InvalidOperationException($"Cannot add the {element.GetType()} as it is already added");
                if (element.Parent != parent) throw new InvalidOperationException($"Cannot add the {element.GetType()}  as it is already added to another {parent.GetType()} instance");
            }

            int index;

            // Optimistic case, we add in order
            if (list.Count == 0 || list[^1].CompareTo(element) < 0)
            {
                element.Parent = parent;
                index = list.Count;
                list.Add(element);
            }
            else
            {
                index = list.BinarySearch(element);
                if (index < 0)
                    index = ~index;
                else if (requiresUnique && list[index].CompareTo(element) == 0)
                {
                    throw new InvalidOperationException($"A similar element to `{element}` has been already added to this collection at index {index}");
                }

                element.Parent = parent;
                list.Insert(index, element);
            }

            element.Index = (uint)index;
            
            // Update the index of following attributes
            for (int i = index + 1; i < list.Count; i++)
            {
                var nextAttribute = list[i];
                nextAttribute.Index++;
            }
        }
        
        /// <summary>
        /// Inserts an attribute into <see cref="Attributes"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Attributes"/> to insert the specified attribute</param>
        /// <param name="element">The attribute to insert</param>
        public static void InsertAt<TParent, TChild>(this List<TChild> list, TParent parent, int index, TChild element) where TChild : ObjectFileNode where TParent : ObjectFileNode
        {
            if (index < 0 || index > list.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {list.Count}");
            if (element == null) throw new ArgumentNullException(nameof(element));
            if (element.Parent != null)
            {
                if (element.Parent == parent) throw new InvalidOperationException($"Cannot add the {element.GetType()} as it is already added");
                if (element.Parent != parent) throw new InvalidOperationException($"Cannot add the {element.GetType()}  as it is already added to another {parent.GetType()} instance");
            }

            element.Index = (uint)index;
            list.Insert(index, element);
            element.Parent = parent;

            // Update the index of following attributes
            for (int i = index + 1; i < list.Count; i++)
            {
                var nextAttribute = list[i];
                nextAttribute.Index++;
            }
        }

        /// <summary>
        /// Removes an attribute from <see cref="Attributes"/>
        /// </summary>
        /// <param name="child">The attribute to remove</param>
        public static void Remove<TParent, TChild>(this List<TChild> list, TParent parent, TChild child) where TChild : ObjectFileNode where TParent : ObjectFileNode
        {
            if (child == null) throw new ArgumentNullException(nameof(child));
            if (!ReferenceEquals(child.Parent, parent))
            {
                throw new InvalidOperationException($"Cannot remove the {nameof(TChild)} as it is not part of this {parent.GetType()} instance");
            }

            var i = (int)child.Index;
            list.RemoveAt(i);
            child.Index = 0;

            // Update indices for other sections
            for (int j = i + 1; j < list.Count; j++)
            {
                var nextEntry = list[j];
                nextEntry.Index--;
            }

            child.Parent = null;
        }

        /// <summary>
        /// Removes an attribute from <see cref="Attributes"/> at the specified index.
        /// </summary>
        /// <param name="index">Index into <see cref="Attributes"/> to remove the specified attribute</param>
        public static TChild RemoveAt<TParent, TChild>(this List<TChild> list, TParent parent, int index) where TChild : ObjectFileNode where TParent : ObjectFileNode
        {
            if (index < 0 || index > list.Count) throw new ArgumentOutOfRangeException(nameof(index), $"Invalid index {index}, Must be >= 0 && <= {list.Count}");
            var child = list[index];
            Remove<TParent, TChild>(list, parent, child);
            return child;
        }
    }
}