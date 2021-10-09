// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#nullable disable
using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;
using System.Globalization;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Xml.Xsl.Runtime
{
    /// <summary>
    /// Table of bound extension functions.  Once an extension function is bound and entered into the table, future bindings
    /// will be very fast.  This table is not thread-safe.
    /// </summary>
    internal sealed class XmlExtensionFunctionTable
    {
        private readonly Dictionary<XmlExtensionFunction, XmlExtensionFunction> _table;
        private XmlExtensionFunction _funcCached;

        public XmlExtensionFunctionTable()
        {
            _table = new Dictionary<XmlExtensionFunction, XmlExtensionFunction>();
        }

        public XmlExtensionFunction Bind(
            string name,
            string namespaceUri,
            int numArgs,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)] Type objectType,
            BindingFlags flags)
        {
            XmlExtensionFunction func;

            if (_funcCached == null)
                _funcCached = new XmlExtensionFunction();

            // If the extension function already exists in the table, then binding has already been performed
            _funcCached.Init(name, namespaceUri, numArgs, objectType, flags);
            if (!_table.TryGetValue(_funcCached, out func))
            {
                // Function doesn't exist, so bind it and enter it into the table
                func = _funcCached;
                _funcCached = null;

                func.Bind();
                _table.Add(func, func);
            }

            return func;
        }
    }

    /// <summary>
    /// This internal class contains methods that allow binding to extension functions and invoking them.
    /// </summary>
    internal sealed class XmlExtensionFunction
    {
        private string _namespaceUri;                // Extension object identifier
        private string _name;                        // Name of this method
        private int _numArgs;                        // Argument count
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.NonPublicMethods)]
        private Type _objectType;                    // Type of the object which will be searched for matching methods
        private BindingFlags _flags;                 // Modifiers that were used to search for a matching signature
        private int _hashCode;                       // Pre-computed hashcode

        private MethodInfo _meth;                    // MethodInfo for extension function
        private Type[] _argClrTypes;                 // Type array for extension function arguments
        private Type _retClrType;                    // Type for extension function return value
        private XmlQueryType[] _argXmlTypes;         // XmlQueryType array for extension function arguments
        private XmlQueryType _retXmlType;            // XmlQueryType for extension function return value

        /// <summary>
        /// Constructor.
        /// </summary>
        public XmlExtensionFunction()
        {
        }

        /// <summary>
        /// Constructor (directly binds to passed MethodInfo).
        /// </summary>
        public XmlExtensionFunction(string name, string namespaceUri, MethodInfo meth)
        {
            _name = name;
            _namespaceUri = namespaceUri;
            Bind(meth);
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        public XmlExtensionFunction(string name, string namespaceUri, int numArgs, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicMethods)] Type objectType, BindingFlags flags)
        {
            Init(name, namespaceUri, numArgs, objectType, flags);
        }

        /// <summary>
        /// Initialize, but do not bind.
        /// </summary>
        public void Init(string name, string namespaceUri, int numArgs, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.NonPublicMethods | DynamicallyAccessedMemberTypes.PublicMethods)] Type objectType, BindingFlags flags)
        {
            _name = name;
            _namespaceUri = namespaceUri;
            _numArgs = numArgs;
            _objectType = objectType;
            _flags = flags;
            _meth = null;
            _argClrTypes = null;
            _retClrType = null;
            _argXmlTypes = null;
            _retXmlType = null;

            // Compute hash code so that it is not recomputed each time GetHashCode() is called
            _hashCode = namespaceUri.GetHashCode() ^ name.GetHashCode() ^ ((int)flags << 16) ^ (int)numArgs;
        }

        /// <summary>
        /// Once Bind has been successfully called, Method will be non-null.
        /// </summary>
        public MethodInfo Method
        {
            get { return _meth; }
        }

        /// <summary>
        /// Once Bind has been successfully called, the Clr type of each argument can be accessed.
        /// Note that this may be different than Method.GetParameterInfo().ParameterType.
        /// </summary>
        public Type GetClrArgumentType(int index)
        {
            return _argClrTypes[index];
        }

        /// <summary>
        /// Once Bind has been successfully called, the Clr type of the return value can be accessed.
        /// Note that this may be different than Method.GetParameterInfo().ReturnType.
        /// </summary>
        public Type ClrReturnType
        {
            get { return _retClrType; }
        }

        /// <summary>
        /// Once Bind has been successfully called, the inferred Xml types of the arguments can be accessed.
        /// </summary>
        public XmlQueryType GetXmlArgumentType(int index)
        {
            return _argXmlTypes[index];
        }

        /// <summary>
        /// Once Bind has been successfully called, the inferred Xml type of the return value can be accessed.
        /// </summary>
        public XmlQueryType XmlReturnType
        {
            get { return _retXmlType; }
        }

        /// <summary>
        /// Return true if the CLR type specified in the Init() call has a matching method.
        /// </summary>
        public bool CanBind()
        {
            MethodInfo[] methods = _objectType.GetMethods(_flags);
            bool ignoreCase = (_flags & BindingFlags.IgnoreCase) != 0;
            StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            // Find method in object type
            foreach (MethodInfo methSearch in methods)
            {
                if (methSearch.Name.Equals(_name, comparison) && (_numArgs == -1 || methSearch.GetParameters().Length == _numArgs))
                {
                    // Binding to generic methods will never succeed
                    if (!methSearch.IsGenericMethodDefinition)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Bind to the CLR type specified in the Init() call.  If a matching method cannot be found, throw an exception.
        /// </summary>
        public void Bind()
        {
            MethodInfo[] methods = _objectType.GetMethods(_flags);
            MethodInfo methMatch = null;
            bool ignoreCase = (_flags & BindingFlags.IgnoreCase) != 0;
            StringComparison comparison = ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

            // Find method in object type
            foreach (MethodInfo methSearch in methods)
            {
                if (methSearch.Name.Equals(_name, comparison) && (_numArgs == -1 || methSearch.GetParameters().Length == _numArgs))
                {
                    if (methMatch != null)
                        throw new XslTransformException(/*[XT_037]*/SR.XmlIl_AmbiguousExtensionMethod, _namespaceUri, _name, _numArgs.ToString(CultureInfo.InvariantCulture));

                    methMatch = methSearch;
                }
            }

            if (methMatch == null)
            {
                methods = _objectType.GetMethods(_flags | BindingFlags.NonPublic);
                foreach (MethodInfo methSearch in methods)
                {
                    if (methSearch.Name.Equals(_name, comparison) && methSearch.GetParameters().Length == _numArgs)
                        throw new XslTransformException(/*[XT_038]*/SR.XmlIl_NonPublicExtensionMethod, _namespaceUri, _name);
                }
                throw new XslTransformException(/*[XT_039]*/SR.XmlIl_NoExtensionMethod, _namespaceUri, _name, _numArgs.ToString(CultureInfo.InvariantCulture));
            }

            if (methMatch.IsGenericMethodDefinition)
                throw new XslTransformException(/*[XT_040]*/SR.XmlIl_GenericExtensionMethod, _namespaceUri, _name);

            Debug.Assert(methMatch.ContainsGenericParameters == false);

            Bind(methMatch);
        }

        /// <summary>
        /// Bind to the specified MethodInfo.
        /// </summary>
        private void Bind(MethodInfo meth)
        {
            ParameterInfo[] paramInfo = meth.GetParameters();
            int i;

            // Save the MethodInfo
            _meth = meth;

            // Get the Clr type of each parameter
            _argClrTypes = new Type[paramInfo.Length];
            for (i = 0; i < paramInfo.Length; i++)
                _argClrTypes[i] = GetClrType(paramInfo[i].ParameterType);

            // Get the Clr type of the return value
            _retClrType = GetClrType(_meth.ReturnType);

            // Infer an Xml type for each Clr type
            _argXmlTypes = new XmlQueryType[paramInfo.Length];
            for (i = 0; i < paramInfo.Length; i++)
            {
                _argXmlTypes[i] = InferXmlType(_argClrTypes[i]);

                // BUGBUG:
                // 1. A couple built-in Xslt functions  allow Rtf as argument, which is
                //    different from what InferXmlType returns.  Until XsltEarlyBound references
                //    a Qil function, we'll work around this case by assuming that all built-in
                //    Xslt functions allow Rtf.
                // 2. Script arguments should allow node-sets which are not statically known
                //    to be Dod to be passed, so relax static typing in this case.
                if (_namespaceUri.Length == 0)
                {
                    if ((object)_argXmlTypes[i] == (object)XmlQueryTypeFactory.NodeNotRtf)
                        _argXmlTypes[i] = XmlQueryTypeFactory.Node;
                    else if ((object)_argXmlTypes[i] == (object)XmlQueryTypeFactory.NodeSDod)
                        _argXmlTypes[i] = XmlQueryTypeFactory.NodeS;
                }
                else
                {
                    if ((object)_argXmlTypes[i] == (object)XmlQueryTypeFactory.NodeSDod)
                        _argXmlTypes[i] = XmlQueryTypeFactory.NodeNotRtfS;
                }
            }

            // Infer an Xml type for the return Clr type
            _retXmlType = InferXmlType(_retClrType);
        }

        /// <summary>
        /// Convert the incoming arguments to an array of CLR objects, and then invoke the external function on the "extObj" object instance.
        /// </summary>
        public object Invoke(object extObj, object[] args)
        {
            Debug.Assert(_meth != null, "Must call Bind() before calling Invoke.");
            Debug.Assert(args.Length == _argClrTypes.Length, "Mismatched number of actual and formal arguments.");

            try
            {
                return _meth.Invoke(extObj, args);
            }
            catch (TargetInvocationException e)
            {
                throw new XslTransformException(e.InnerException, SR.XmlIl_ExtensionError, _name);
            }
            catch (Exception e)
            {
                if (!XmlException.IsCatchableException(e))
                {
                    throw;
                }
                throw new XslTransformException(e, SR.XmlIl_ExtensionError, _name);
            }
        }

        /// <summary>
        /// Return true if this XmlExtensionFunction has the same values as another XmlExtensionFunction.
        /// </summary>
        public override bool Equals(object other)
        {
            XmlExtensionFunction that = other as XmlExtensionFunction;
            Debug.Assert(that != null);

            // Compare name, argument count, object type, and binding flags
            return (_hashCode == that._hashCode && _name == that._name && _namespaceUri == that._namespaceUri &&
                    _numArgs == that._numArgs && _objectType == that._objectType && _flags == that._flags);
        }

        /// <summary>
        /// Return this object's hash code, previously computed for performance.
        /// </summary>
        public override int GetHashCode()
        {
            return _hashCode;
        }

        /// <summary>
        /// 1. Map enumerations to the underlying integral type.
        /// 2. Throw an exception if the type is ByRef
        /// </summary>
        private Type GetClrType(Type clrType)
        {
            if (clrType.IsEnum)
                return Enum.GetUnderlyingType(clrType);

            if (clrType.IsByRef)
                throw new XslTransformException(/*[XT_050]*/SR.XmlIl_ByRefType, _namespaceUri, _name);

            return clrType;
        }

        /// <summary>
        /// Infer an Xml type from a Clr type using Xslt inference rules
        /// </summary>
        private XmlQueryType InferXmlType(Type clrType)
        {
            return XsltConvert.InferXsltType(clrType);
        }
    }
}
