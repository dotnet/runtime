// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;

namespace System.Text
{
    // If we find issues with this or if more libraries need this behavior we will revisit the solution.
    internal static partial class EncodingHelper
    {
        // Since only a minimum set of encodings are available by default,
        // Console encoding might not be available and require provider registering.
        // To avoid encoding exception in Console APIs we fallback to OSEncoding.
        //
        //
        // The guaranteed way to identify the above is to use a try/catch pattern, however to avoid 1st chance exceptions 
        // we do a best-effort heuristic instead. By default CodePage 0 is CP_ACP(Windows ANSI encoding)
        // and Encoding.GetEncoding(0) returns the ANSI codepage if its encoding data is available else it returns UTF8. We leverage 
        // this behavior to identify whether a potential provider is registered or not.
        //
        //
        // This pattern will always use UTF8 encoding if a custom provider is registered, and the console encoding is set to a custom encoding
        // with codepage 0 not supported by the registered custom provider. To make Console use the intended encoding in this scenario, the user will have 
        // to register CodePagesEncodingProvider or support codepage 0 in their custom provider.
        internal static Encoding GetSupportedConsoleEncoding(int codepage)
        {
            int defaultEncCodePage = Encoding.GetEncoding(0).CodePage;

            if ((defaultEncCodePage == codepage) || defaultEncCodePage != Encoding.UTF8.CodePage)
            {
                return Encoding.GetEncoding(codepage);
            }

            if (codepage != Encoding.UTF8.CodePage)
            {
                return new OSEncoding(codepage);
            }

            return new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        }
    }
}
