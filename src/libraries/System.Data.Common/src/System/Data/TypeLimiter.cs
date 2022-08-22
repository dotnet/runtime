// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace System.Data
{
    internal sealed class TypeLimiter
    {
        [ThreadStatic]
        private static Scope? s_activeScope;

        private Scope m_instanceScope;

        private const string AppDomainDataSetDefaultAllowedTypesKey = "System.Data.DataSetDefaultAllowedTypes";

        private TypeLimiter(Scope scope)
        {
            Debug.Assert(scope != null);
            m_instanceScope = scope;
        }

        private static bool IsTypeLimitingDisabled
            => LocalAppContextSwitches.AllowArbitraryTypeInstantiation;

        /// <summary>
        /// Captures the current <see cref="TypeLimiter"/> instance so that future
        /// type checks can be performed against the allow list that was active during
        /// the current deserialization scope.
        /// </summary>
        /// <remarks>
        /// Returns null if no limiter is active.
        /// </remarks>
        public static TypeLimiter? Capture()
        {
            Scope? activeScope = s_activeScope;
            return (activeScope != null) ? new TypeLimiter(activeScope) : null;
        }

        /// <summary>
        /// Ensures the requested type is allowed by the rules of the active
        /// deserialization scope. If a captured scope is provided, we'll use
        /// that previously captured scope rather than the thread-static active
        /// scope.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If <paramref name="type"/> is not allowed.
        /// </exception>
        public static void EnsureTypeIsAllowed(Type? type, TypeLimiter? capturedLimiter = null)
        {
            if (type is null)
            {
                return; // nothing to check
            }

            Scope? capturedScope = capturedLimiter?.m_instanceScope ?? s_activeScope;
            if (capturedScope is null)
            {
                return; // we're not in a restricted scope
            }

            if (capturedScope.IsAllowedType(type))
            {
                return; // type was explicitly allowed
            }

            // We encountered a type that wasn't in the allow list.
            // Throw an exception to fail the current operation.

            throw ExceptionBuilder.TypeNotAllowed(type);
        }

        public static IDisposable? EnterRestrictedScope(DataSet dataSet)
        {
            if (IsTypeLimitingDisabled)
            {
                return null; // protections aren't enabled
            }

            Scope newScope = new Scope(s_activeScope, GetPreviouslyDeclaredDataTypes(dataSet));
            s_activeScope = newScope;
            return newScope;
        }

        public static IDisposable? EnterRestrictedScope(DataTable dataTable)
        {
            if (IsTypeLimitingDisabled)
            {
                return null; // protections aren't enabled
            }

            Scope newScope = new Scope(s_activeScope, GetPreviouslyDeclaredDataTypes(dataTable));
            s_activeScope = newScope;
            return newScope;
        }

        /// <summary>
        /// Given a <see cref="DataTable"/>, returns all of the <see cref="DataColumn.DataType"/>
        /// values declared on the instance.
        /// </summary>
        private static IEnumerable<Type> GetPreviouslyDeclaredDataTypes(DataTable dataTable)
        {
            return (dataTable != null)
                ? dataTable.Columns.Cast<DataColumn>().Select(column => column.DataType)
                : Enumerable.Empty<Type>();
        }

        /// <summary>
        /// Given a <see cref="DataSet"/>, returns all of the <see cref="DataColumn.DataType"/>
        /// values declared on the instance.
        /// </summary>
        private static IEnumerable<Type> GetPreviouslyDeclaredDataTypes(DataSet dataSet)
        {
            return (dataSet != null)
                ? dataSet.Tables.Cast<DataTable>().SelectMany(GetPreviouslyDeclaredDataTypes)
                : Enumerable.Empty<Type>();
        }

        private sealed class Scope : IDisposable
        {
            /// <summary>
            /// Types which are always allowed, unconditionally.
            /// </summary>
            private static readonly HashSet<Type> s_allowedTypes = new HashSet<Type>()
            {
                /* primitives */
                typeof(bool),
                typeof(char),
                typeof(sbyte),
                typeof(byte),
                typeof(short),
                typeof(ushort),
                typeof(int),
                typeof(uint),
                typeof(long),
                typeof(ulong),
                typeof(float),
                typeof(double),
                typeof(decimal),
                typeof(DateTime),
                typeof(DateTimeOffset),
                typeof(TimeSpan),
                typeof(string),
                typeof(Guid),
                typeof(SqlBinary),
                typeof(SqlBoolean),
                typeof(SqlByte),
                typeof(SqlBytes),
                typeof(SqlChars),
                typeof(SqlDateTime),
                typeof(SqlDecimal),
                typeof(SqlDouble),
                typeof(SqlGuid),
                typeof(SqlInt16),
                typeof(SqlInt32),
                typeof(SqlInt64),
                typeof(SqlMoney),
                typeof(SqlSingle),
                typeof(SqlString),

                /* non-primitives, but common */
                typeof(object),
                typeof(Type),
                typeof(BigInteger),
                typeof(Uri),

                /* frequently used System.Drawing types */
                typeof(Color),
                typeof(Point),
                typeof(PointF),
                typeof(Rectangle),
                typeof(RectangleF),
                typeof(Size),
                typeof(SizeF),
            };

            /// <summary>
            /// Types which are allowed within the context of this scope.
            /// </summary>
            private HashSet<Type> m_allowedTypes;

            /// <summary>
            /// This thread's previous scope.
            /// </summary>
            private readonly Scope? m_previousScope;

            /// <summary>
            /// The Serialization Guard token associated with this scope.
            /// </summary>
            private readonly DeserializationToken m_deserializationToken;

            internal Scope(Scope? previousScope, IEnumerable<Type> allowedTypes)
            {
                Debug.Assert(allowedTypes != null);

                m_previousScope = previousScope;
                m_allowedTypes = new HashSet<Type>(allowedTypes.Where(type => type != null));
                m_deserializationToken = SerializationInfo.StartDeserialization();
            }

            public void Dispose()
            {
                if (this != s_activeScope)
                {
                    // Stacks should never be popped out of order.
                    // We want to trap this condition in production.
                    Debug.Fail("Scope was popped out of order.");
                    throw new ObjectDisposedException(GetType().FullName);
                }

                m_deserializationToken.Dispose(); // it's a readonly struct, but Dispose still works properly
                s_activeScope = m_previousScope; // could be null
            }

            public bool IsAllowedType(Type type)
            {
                Debug.Assert(type != null);

                // Is the incoming type unconditionally allowed?

                if (IsTypeUnconditionallyAllowed(type))
                {
                    return true;
                }

                // The incoming type is allowed if the current scope or any nested inner
                // scope allowed it.

                for (Scope? currentScope = this; currentScope != null; currentScope = currentScope.m_previousScope)
                {
                    if (currentScope.m_allowedTypes.Contains(type))
                    {
                        return true;
                    }
                }

                // Did the application programmatically allow this type to be deserialized?

                Type[]? appDomainAllowedTypes = (Type[]?)AppDomain.CurrentDomain.GetData(AppDomainDataSetDefaultAllowedTypesKey);
                if (appDomainAllowedTypes != null)
                {
                    for (int i = 0; i < appDomainAllowedTypes.Length; i++)
                    {
                        if (type == appDomainAllowedTypes[i])
                        {
                            return true;
                        }
                    }
                }

                // All checks failed

                return false;
            }

            private static bool IsTypeUnconditionallyAllowed(Type type)
            {
            TryAgain:
                Debug.Assert(type != null);

                // Check the list of unconditionally allowed types.

                if (s_allowedTypes.Contains(type))
                {
                    return true;
                }

                // Enums are also always allowed, as we optimistically assume the app
                // developer didn't define a dangerous enum type.

                if (type.IsEnum)
                {
                    return true;
                }

                // Allow single-dimensional arrays of any unconditionally allowed type.

                if (type.IsSZArray)
                {
                    type = type.GetElementType()!;
                    goto TryAgain;
                }

                // Allow generic lists of any unconditionally allowed type.

                if (type.IsGenericType && !type.IsGenericTypeDefinition && type.GetGenericTypeDefinition() == typeof(List<>))
                {
                    type = type.GetGenericArguments()[0];
                    goto TryAgain;
                }

                // All checks failed.

                return false;
            }
        }
    }
}
