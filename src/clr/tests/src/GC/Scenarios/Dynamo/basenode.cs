// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace Dynamo {
    using System;

    public class BaseNode
    {
        protected int iValue = 0;
        protected int iType = 111111;
        public static Dynamo Cv_Dynamo;

        public int Value {
            get { return iValue; }
            set { iValue = value; }
        }

        public int Type {
            get { return iType; }
        }

        ~BaseNode()
        {
            if (Cv_Dynamo!=null)
                Cv_Dynamo.RegisterCleanup( Type, Value );
        }

    }
}
