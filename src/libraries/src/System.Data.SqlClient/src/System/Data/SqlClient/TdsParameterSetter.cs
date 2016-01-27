// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.




//------------------------------------------------------------------------------

using System.Diagnostics;

using Microsoft.SqlServer.Server;

namespace System.Data.SqlClient
{
    // Simple Getter/Setter for structured parameters to allow using common ValueUtilsSmi code.
    //  This is a stand-in to having a true SmiRequestExecutor class for TDS.
    internal class TdsParameterSetter : SmiTypedGetterSetter
    {
        #region Private fields

        private TdsRecordBufferSetter _target;

        #endregion

        #region ctor & control

        internal TdsParameterSetter(TdsParserStateObject stateObj, SmiMetaData md)
        {
            _target = new TdsRecordBufferSetter(stateObj, md);
        }

        #endregion

        #region TypedGetterSetter overrides
        // Are calls to Get methods allowed?
        internal override bool CanGet
        {
            get
            {
                return false;
            }
        }

        // Are calls to Set methods allowed?
        internal override bool CanSet
        {
            get
            {
                return true;
            }
        }

        // valid for structured types
        //  This method called for both get and set.
        internal override SmiTypedGetterSetter GetTypedGetterSetter(SmiEventSink sink, int ordinal)
        {
            Debug.Assert(0 == ordinal, "TdsParameterSetter only supports 0 for ordinal.  Actual = " + ordinal);
            return _target;
        }

        // Set value to null
        //  valid for all types
        public override void SetDBNull(SmiEventSink sink, int ordinal)
        {
            Debug.Assert(0 == ordinal, "TdsParameterSetter only supports 0 for ordinal.  Actual = " + ordinal);

            _target.EndElements(sink);
        }
        #endregion
    }
}
