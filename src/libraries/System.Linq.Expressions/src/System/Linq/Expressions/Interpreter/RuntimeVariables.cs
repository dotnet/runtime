// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

        int IRuntimeVariables.Count
        {
            get
            {
                return _boxes.Length;
            }
        }

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

        internal static IRuntimeVariables Create(IStrongBox[] boxes)
        {
            return new RuntimeVariables(boxes);
        }
    }
}
