using System.Collections.Generic;

namespace MultiProjectValidator
{
    public class AnalysisResult
    {

        private List<string> _messages;
        private bool _passed;

        public AnalysisResult(List<string> messages, bool passed)
        {
            this._messages = messages;
            this._passed = passed;
        }

        public List<string> Messages
        {
            get
            {
                return _messages;
            }
        }

        public bool Passed
        {
            get
            {
                return _passed;
            }
        }
    }
}
