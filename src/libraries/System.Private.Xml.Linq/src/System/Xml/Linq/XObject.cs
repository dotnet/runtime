// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;

using Debug = System.Diagnostics.Debug;
using SuppressMessageAttribute = System.Diagnostics.CodeAnalysis.SuppressMessageAttribute;

namespace System.Xml.Linq
{
    /// <summary>
    /// Represents a node or an attribute in an XML tree.
    /// </summary>
    public abstract class XObject : IXmlLineInfo
    {
        internal XContainer? parent;
        internal object? annotations;

        internal XObject() { }

        /// <summary>
        /// Get the BaseUri for this <see cref="XObject"/>.
        /// </summary>
        public string BaseUri
        {
            get
            {
                XObject? o = this;
                while (true)
                {
                    while (o != null && o.annotations == null)
                    {
                        o = o.parent;
                    }
                    if (o == null) break;
                    BaseUriAnnotation? a = o.Annotation<BaseUriAnnotation>();
                    if (a != null) return a.baseUri;
                    o = o.parent;
                }
                return string.Empty;
            }
        }

        /// <summary>
        /// Gets the XDocument object for this <see cref="XObject"/>.
        /// </summary>
        public XDocument? Document
        {
            get
            {
                XObject n = this;
                while (n.parent != null) n = n.parent;

                XDocument? doc = n as XDocument;

                return doc;
            }
        }

        /// <summary>
        /// Gets the node type for this <see cref="XObject"/>.
        /// </summary>
        public abstract XmlNodeType NodeType { get; }

        /// <summary>
        /// Gets the parent <see cref="XElement"/> of this <see cref="XObject"/>.
        /// </summary>
        /// <remarks>
        /// If this <see cref="XObject"/> has no parent <see cref="XElement"/>, this property returns null.
        /// </remarks>
        public XElement? Parent
        {
            get { return parent as XElement; }
        }

        /// <summary>
        /// Adds an object to the annotation list of this <see cref="XObject"/>.
        /// </summary>
        /// <param name="annotation">The annotation to add.</param>
        public void AddAnnotation(object annotation)
        {
            ArgumentNullException.ThrowIfNull(annotation);

            if (annotations == null)
            {
                annotations = annotation is object[]? new object[] { annotation } : annotation;
            }
            else
            {
                object?[]? a = annotations as object[];
                if (a == null)
                {
                    annotations = new object[] { annotations, annotation };
                }
                else
                {
                    int i = 0;
                    while (i < a.Length && a[i] != null) i++;
                    if (i == a.Length)
                    {
                        Array.Resize(ref a, i * 2);
                        annotations = a;
                    }
                    a[i] = annotation;
                }
            }
        }

        /// <summary>
        /// Returns the first annotation object of the specified type from the list of annotations
        /// of this <see cref="XObject"/>.
        /// </summary>
        /// <param name="type">The type of the annotation to retrieve.</param>
        /// <returns>
        /// The first matching annotation object, or null
        /// if no annotation is the specified type.
        /// </returns>
        public object? Annotation(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (annotations != null)
            {
                object?[]? a = annotations as object[];
                if (a == null)
                {
                    if (XHelper.IsInstanceOfType(annotations, type)) return annotations;
                }
                else
                {
                    for (int i = 0; i < a.Length; i++)
                    {
                        object? obj = a[i];
                        if (obj == null) break;
                        if (XHelper.IsInstanceOfType(obj, type)) return obj;
                    }
                }
            }
            return null;
        }

        private object? AnnotationForSealedType(Type type)
        {
            Debug.Assert(type != null);

            if (annotations != null)
            {
                object?[]? a = annotations as object[];
                if (a == null)
                {
                    if (annotations.GetType() == type) return annotations;
                }
                else
                {
                    for (int i = 0; i < a.Length; i++)
                    {
                        object? obj = a[i];
                        if (obj == null) break;
                        if (obj.GetType() == type) return obj;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// Returns the first annotation object of the specified type from the list of annotations
        /// of this <see cref="XObject"/>.
        /// </summary>
        /// <typeparam name="T">The type of the annotation to retrieve.</typeparam>
        /// <returns>
        /// The first matching annotation object, or null if no annotation
        /// is the specified type.
        /// </returns>
        public T? Annotation<T>() where T : class
        {
            if (annotations != null)
            {
                object?[]? a = annotations as object[];
                if (a == null) return annotations as T;
                for (int i = 0; i < a.Length; i++)
                {
                    object? obj = a[i];
                    if (obj == null) break;
                    T? result = obj as T;
                    if (result != null) return result;
                }
            }
            return null;
        }

        /// <summary>
        /// Returns an enumerable collection of annotations of the specified type
        /// for this <see cref="XObject"/>.
        /// </summary>
        /// <param name="type">The type of the annotations to retrieve.</param>
        /// <returns>An enumerable collection of annotations for this XObject.</returns>
        public IEnumerable<object> Annotations(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return AnnotationsIterator(type);
        }

        private IEnumerable<object> AnnotationsIterator(Type type)
        {
            if (annotations != null)
            {
                object?[]? a = annotations as object[];
                if (a == null)
                {
                    if (XHelper.IsInstanceOfType(annotations, type)) yield return annotations;
                }
                else
                {
                    for (int i = 0; i < a.Length; i++)
                    {
                        object? obj = a[i];
                        if (obj == null) break;
                        if (XHelper.IsInstanceOfType(obj, type)) yield return obj;
                    }
                }
            }
        }

        /// <summary>
        /// Returns an enumerable collection of annotations of the specified type
        /// for this <see cref="XObject"/>.
        /// </summary>
        /// <typeparam name="T">The type of the annotations to retrieve.</typeparam>
        /// <returns>An enumerable collection of annotations for this XObject.</returns>
        public IEnumerable<T> Annotations<T>() where T : class
        {
            if (annotations != null)
            {
                object?[]? a = annotations as object[];
                if (a == null)
                {
                    T? result = annotations as T;
                    if (result != null) yield return result;
                }
                else
                {
                    for (int i = 0; i < a.Length; i++)
                    {
                        object? obj = a[i];
                        if (obj == null) break;
                        T? result = obj as T;
                        if (result != null) yield return result;
                    }
                }
            }
        }

        /// <summary>
        /// Removes the annotations of the specified type from this <see cref="XObject"/>.
        /// </summary>
        /// <param name="type">The type of annotations to remove.</param>
        public void RemoveAnnotations(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            if (annotations != null)
            {
                object?[]? a = annotations as object[];
                if (a == null)
                {
                    if (XHelper.IsInstanceOfType(annotations, type)) annotations = null;
                }
                else
                {
                    int i = 0, j = 0;
                    while (i < a.Length)
                    {
                        object? obj = a[i];
                        if (obj == null) break;
                        if (!XHelper.IsInstanceOfType(obj, type)) a[j++] = obj;
                        i++;
                    }
                    if (j == 0)
                    {
                        annotations = null;
                    }
                    else
                    {
                        while (j < i) a[j++] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Removes the annotations of the specified type from this <see cref="XObject"/>.
        /// </summary>
        /// <typeparam name="T">The type of annotations to remove.</typeparam>
        public void RemoveAnnotations<T>() where T : class
        {
            if (annotations != null)
            {
                object?[]? a = annotations as object[];
                if (a == null)
                {
                    if (annotations is T) annotations = null;
                }
                else
                {
                    int i = 0, j = 0;
                    while (i < a.Length)
                    {
                        object? obj = a[i];
                        if (obj == null) break;
                        if (!(obj is T)) a[j++] = obj;
                        i++;
                    }
                    if (j == 0)
                    {
                        annotations = null;
                    }
                    else
                    {
                        while (j < i) a[j++] = null;
                    }
                }
            }
        }

        /// <summary>
        /// Occurs when this <see cref="XObject"/> or any of its descendants have changed.
        /// </summary>
        public event EventHandler<XObjectChangeEventArgs> Changed
        {
            add
            {
                if (value == null) return;
                XObjectChangeAnnotation? a = Annotation<XObjectChangeAnnotation>();
                if (a == null)
                {
                    a = new XObjectChangeAnnotation();
                    AddAnnotation(a);
                }
                a.changed += value;
            }
            remove
            {
                if (value == null) return;
                XObjectChangeAnnotation? a = Annotation<XObjectChangeAnnotation>();
                if (a == null) return;
                a.changed -= value;
                if (a.changing == null && a.changed == null)
                {
                    RemoveAnnotations<XObjectChangeAnnotation>();
                }
            }
        }

        /// <summary>
        /// Occurs when this <see cref="XObject"/> or any of its descendants are about to change.
        /// </summary>
        public event EventHandler<XObjectChangeEventArgs> Changing
        {
            add
            {
                if (value == null) return;
                XObjectChangeAnnotation? a = Annotation<XObjectChangeAnnotation>();
                if (a == null)
                {
                    a = new XObjectChangeAnnotation();
                    AddAnnotation(a);
                }
                a.changing += value;
            }
            remove
            {
                if (value == null) return;
                XObjectChangeAnnotation? a = Annotation<XObjectChangeAnnotation>();
                if (a == null) return;
                a.changing -= value;
                if (a.changing == null && a.changed == null)
                {
                    RemoveAnnotations<XObjectChangeAnnotation>();
                }
            }
        }

        bool IXmlLineInfo.HasLineInfo()
        {
            return Annotation<LineInfoAnnotation>() != null;
        }

        int IXmlLineInfo.LineNumber
        {
            get
            {
                LineInfoAnnotation? a = Annotation<LineInfoAnnotation>();
                if (a != null) return a.lineNumber;
                return 0;
            }
        }

        int IXmlLineInfo.LinePosition
        {
            get
            {
                LineInfoAnnotation? a = Annotation<LineInfoAnnotation>();
                if (a != null) return a.linePosition;
                return 0;
            }
        }

        internal bool HasBaseUri
        {
            get
            {
                return Annotation<BaseUriAnnotation>() != null;
            }
        }

        internal bool NotifyChanged(object sender, XObjectChangeEventArgs e)
        {
            bool notify = false;
            XObject? o = this;
            while (true)
            {
                while (o != null && o.annotations == null)
                {
                    o = o.parent;
                }
                if (o == null) break;
                XObjectChangeAnnotation? a = o.Annotation<XObjectChangeAnnotation>();
                if (a != null)
                {
                    notify = true;
                    a.changed?.Invoke(sender, e);
                }
                o = o.parent;
            }
            return notify;
        }

        internal bool NotifyChanging(object sender, XObjectChangeEventArgs e)
        {
            bool notify = false;
            XObject? o = this;
            while (true)
            {
                while (o != null && o.annotations == null)
                {
                    o = o.parent;
                }
                if (o == null) break;
                XObjectChangeAnnotation? a = o.Annotation<XObjectChangeAnnotation>();
                if (a != null)
                {
                    notify = true;
                    a.changing?.Invoke(sender, e);
                }
                o = o.parent;
            }
            return notify;
        }

        internal void SetBaseUri(string baseUri)
        {
            AddAnnotation(new BaseUriAnnotation(baseUri));
        }

        internal void SetLineInfo(int lineNumber, int linePosition)
        {
            AddAnnotation(new LineInfoAnnotation(lineNumber, linePosition));
        }

        internal bool SkipNotify()
        {
            XObject? o = this;
            while (true)
            {
                while (o != null && o.annotations == null)
                {
                    o = o.parent;
                }
                if (o == null) return true;
                if (o.Annotation<XObjectChangeAnnotation>() != null) return false;
                o = o.parent;
            }
        }

        /// <summary>
        /// Walks the tree starting with "this" node and returns first annotation of type <see cref="SaveOptions"/>
        ///   found in the ancestors.
        /// </summary>
        /// <returns>The effective <see cref="SaveOptions"/> for this <see cref="XObject"/></returns>
        internal SaveOptions GetSaveOptionsFromAnnotations()
        {
            XObject? o = this;
            while (true)
            {
                while (o != null && o.annotations == null)
                {
                    o = o.parent;
                }
                if (o == null)
                {
                    return SaveOptions.None;
                }
                object? saveOptions = o.AnnotationForSealedType(typeof(SaveOptions));
                if (saveOptions != null)
                {
                    return (SaveOptions)saveOptions;
                }
                o = o.parent;
            }
        }
    }
}
