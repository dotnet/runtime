// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Speech.Internal;

namespace System.Speech.Recognition
{
    /// TODOC <_include file='doc\SpeechUI.uex' path='docs/doc[@for="SpeechUI"]/*' />
    public class SpeechUI
    {
        internal SpeechUI()
        {
        }

        /// TODOC <_include file='doc\SpeechUI.uex' path='docs/doc[@for="SpeechUI.SendTextFeedback"]/*' />
        public static bool SendTextFeedback(RecognitionResult result, string feedback, bool isSuccessfulAction)
        {
            Helpers.ThrowIfNull (result,  "result");
            Helpers.ThrowIfEmptyOrNull (feedback, "feedback");

            return result.SetTextFeedback(feedback, isSuccessfulAction);
      }
    }
}
