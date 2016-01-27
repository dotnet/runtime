// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.



//------------------------------------------------------------------------------

using System.Data.SqlClient;
using System.Diagnostics;

namespace Microsoft.SqlServer.Server
{
    internal class SmiEventSink_Default : SmiEventSink
    {
        private SqlErrorCollection _errors;
        private SqlErrorCollection _warnings;


        internal bool HasMessages
        {
            get
            {
                {
                    bool result = (null != _errors || null != _warnings);
                    return result;
                }
            }
        }

        virtual internal string ServerVersion
        {
            get
            {
                return null;
            }
        }


        protected virtual void DispatchMessages()
        {
            // virtual because we want a default implementation in the cases
            // where we don't have a connection to process stuff, but we want to
            // provide the connection the ability to fire info messages when it
            // hooks up.
            {
                SqlException errors = ProcessMessages(true);   // ignore warnings, because there's no place to send them...
                if (null != errors)
                {
                    throw errors;
                }
            }
        }

        protected SqlException ProcessMessages(bool ignoreWarnings)
        {
            SqlException result = null;
            SqlErrorCollection temp = null;  // temp variable to store that which is being thrown - so that local copies can be deleted

            if (null != _errors)
            {
                Debug.Assert(0 != _errors.Count, "empty error collection?"); // must be something in the collection

                {
                    if (null != _warnings)
                    {
                        // When we throw an exception we place all the warnings that
                        // occurred at the end of the collection - after all the errors.
                        // That way the user can see all the errors AND warnings that
                        // occurred for the exception.
                        foreach (SqlError warning in _warnings)
                        {
                            _errors.Add(warning);
                        }
                    }
                    temp = _errors;
                }

                _errors = null;
                _warnings = null;
            }
            else
            {
                Debug.Assert(null == _warnings || 0 != _warnings.Count, "empty warning collection?");// must be something in the collection

                if (!ignoreWarnings)
                {
                    temp = _warnings;
                }
                _warnings = null;
            }

            if (null != temp)
            {
                result = SqlException.CreateException(temp, ServerVersion);
            }
            return result;
        }


        internal void ProcessMessagesAndThrow()
        {
            if (HasMessages)
            {
                DispatchMessages();
            }
        }



        internal SmiEventSink_Default()
        {
        }
    }
}

