// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Linq.Expressions.Interpreter
{
    internal sealed class RuntimeVariables : IRuntimeVariables
    {
        private readonly IStrongBox[] _boxes;

        private RuntimeVariables(IStrongBox[] boxes)
        {
            _boxes = boxes;
        }

        int IRuntimeVariables.Count => _boxes.Length;

        object IRuntimeVariables.this[int index]
        {
            get
            {
                return _boxes[index].Value;
            }
            set
            {
                _boxes[index].Value = value;
            }
        }

        internal static IRuntimeVariables Create(IStrongBox[] boxes) => new RuntimeVariables(boxes);
    }
}
