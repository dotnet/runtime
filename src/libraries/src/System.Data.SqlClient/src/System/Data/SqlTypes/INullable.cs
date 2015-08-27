// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.



//------------------------------------------------------------------------------

namespace System.Data.SqlTypes
{
    public interface INullable
    {
        bool IsNull
        {
            get;
        }
    }
}
