// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Xml.Xsl.Qil;

namespace System.Xml.Xsl.IlGen
{
    /// <summary>
    /// Several annotations are created and attached to Qil nodes during the optimization and code generation phase.
    /// </summary>
    internal sealed class XmlILAnnotation : ListBase<object?>
    {
        private readonly object? _annPrev;
        private MethodInfo? _funcMethod;
        private int _argPos;
        private IteratorDescriptor? _iterInfo;
        private XmlILConstructInfo? _constrInfo;
        private OptimizerPatterns? _optPatt;


        //-----------------------------------------------
        // Constructor
        //-----------------------------------------------

        /// <summary>
        /// Create and initialize XmlILAnnotation for the specified node.
        /// </summary>
        public static XmlILAnnotation Write(QilNode nd)
        {
            XmlILAnnotation? ann = nd.Annotation as XmlILAnnotation;

            if (ann == null)
            {
                ann = new XmlILAnnotation(nd.Annotation);
                nd.Annotation = ann;
            }

            return ann;
        }

        private XmlILAnnotation(object? annPrev)
        {
            _annPrev = annPrev;
        }


        //-----------------------------------------------
        // Annotations
        //-----------------------------------------------

        /// <summary>
        /// User-defined functions and global variables and parameters are bound to Clr MethodInfo objects.
        /// Attached to Function, global Let, and global Parameter nodes.
        /// </summary>
        public MethodInfo? FunctionBinding
        {
            get { return _funcMethod; }
            set { _funcMethod = value; }
        }

        /// <summary>
        /// Function arguments are tracked by position.
        /// Attached to function Parameter nodes.
        /// </summary>
        public int ArgumentPosition
        {
            get { return _argPos; }
            set { _argPos = value; }
        }

        /// <summary>
        /// The IteratorDescriptor that is derived for Qil For and Let nodes is cached so that it can be used when the
        /// For/Let node is referenced.
        /// Attached to For and Let nodes.
        /// </summary>
        public IteratorDescriptor? CachedIteratorDescriptor
        {
            get { return _iterInfo; }
            set { _iterInfo = value; }
        }

        /// <summary>
        /// Contains information about how this expression will be constructed by ILGen.
        /// Attached to any kind of Qil node.
        /// </summary>
        public XmlILConstructInfo? ConstructInfo
        {
            get { return _constrInfo; }
            set { _constrInfo = value; }
        }

        /// <summary>
        /// Contains patterns that the subtree rooted at this node matches.
        /// Attached to any kind of Qil node.
        /// </summary>
        public OptimizerPatterns? Patterns
        {
            get { return _optPatt; }
            set { _optPatt = value; }
        }


        //-----------------------------------------------
        // ListBase implementation
        //-----------------------------------------------

        /// <summary>
        /// Return the count of sub-annotations maintained by this annotation.
        /// </summary>
        public override int Count
        {
            get { return (_annPrev != null) ? 3 : 2; }
        }

        /// <summary>
        /// Return the annotation at the specified index.
        /// </summary>
        public override object? this[int index]
        {
            get
            {
                if (_annPrev != null)
                {
                    if (index == 0)
                        return _annPrev;

                    index--;
                }

                return index switch
                {
                    0 => _constrInfo,
                    1 => _optPatt,

                    _ => throw new IndexOutOfRangeException(),
                };
            }
            set
            {
                throw new NotSupportedException();
            }
        }
    }
}
