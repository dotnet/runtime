// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Reflection.Emit
{
    public abstract class LocalBuilder : LocalVariableInfo
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LocalBuilder"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor is invoked by derived classes.
        /// </remarks>
        protected LocalBuilder() { }

        /// <summary>
        /// Sets the name of this local variable.
        /// </summary>
        /// <param name="name">The name of the local variable</param>
        /// <exception cref="ArgumentNullException">The <paramref name="name"/> is null.</exception>
        /// <exception cref="InvalidOperationException">The containing type has been created with CreateType() or
        /// containing type doesn't support symbol writing.</exception>"
        public void SetLocalSymInfo(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            SetLocalSymInfoCore(name);
        }

        /// <summary>
        /// When overridden in a derived class, sets the name of this local variable.
        /// </summary>
        /// <param name="name">The name of the local variable.</param>
        /// <exception cref="NotSupportedException">Emitting debug info is not supported.</exception>"
        /// <exception cref="InvalidOperationException">The containing type has been created with CreateType().</exception>"
        protected virtual void SetLocalSymInfoCore(string name) => throw new NotSupportedException(SR.NotSupported_EmitDebugInfo);
    }
}
