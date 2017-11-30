// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.ComponentModel.Composition.Primitives;
using System.Globalization;
using System.Reflection;
using System.Threading;
using Microsoft.Internal;

namespace System.ComponentModel.Composition.ReflectionModel
{
    internal class ExportingMember
    {
        private readonly ExportDefinition _definition;
        private readonly ReflectionMember _member;
        private object _cachedValue = null;
        private volatile bool _isValueCached = false;

        public ExportingMember(ExportDefinition definition, ReflectionMember member)
        {
            Assumes.NotNull(definition, member);

            _definition = definition;
            _member = member;
        }

        public bool RequiresInstance
        {
            get { return _member.RequiresInstance; }
        }

        public ExportDefinition Definition
        {
            get { return _definition; }
        }

        public object GetExportedValue(object instance, object @lock)
        {
            EnsureReadable();

            if (!_isValueCached)
            {
                object exportedValue;
                try
                {
                    exportedValue = _member.GetValue(instance);
                }
                catch (TargetInvocationException exception)
                {   // Member threw an exception. Avoid letting this 
                    // leak out as a 'raw' unhandled exception, instead,
                    // we'll add some context and rethrow.

                    throw new ComposablePartException(
                        String.Format(CultureInfo.CurrentCulture,
                            SR.ReflectionModel_ExportThrewException,
                            _member.GetDisplayName()),
                        Definition.ToElement(),
                        exception.InnerException);
                }
                catch (TargetParameterCountException exception)
                {
                    // Exception was a TargetParameterCountException this occurs when we try to get an Indexer that has an Export
                    // this is not supported in MEF currently.  Ideally we would validate against it, however, we already shipped
                    // so we will turn it into a ComposablePartException instead, that they should already be prepared for
                    throw new ComposablePartException(
                        String.Format(CultureInfo.CurrentCulture,
                        SR.ExportNotValidOnIndexers,
                        _member.GetDisplayName()),
                        Definition.ToElement(),
                        exception.InnerException);
                }

                lock (@lock)
                {
                    if (!_isValueCached)
                    {
                        _cachedValue = exportedValue;
                        Thread.MemoryBarrier();

                        _isValueCached = true;
                    }
                }
            }

            return _cachedValue;
        }

        private void EnsureReadable()
        {
            if (!_member.CanRead)
            {   // Property does not have a getter

                throw new ComposablePartException(
                    String.Format(CultureInfo.CurrentCulture, 
                        SR.ReflectionModel_ExportNotReadable,
                        _member.GetDisplayName()),
                    Definition.ToElement());
            }
        }
    }
}
