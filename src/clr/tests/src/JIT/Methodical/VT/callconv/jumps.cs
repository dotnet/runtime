// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace MS
{
    internal struct VT
    {
        private String _path;
        private int _target;

        public override String ToString()
        {
            _path += "->ToString";
            switch (_target)
            {
                case 0:
                    return "VT";
                case 1:
                    _target = 0;
                    return ToStringHelper();
                case 2:
                    _target = 0;
                    return ToString();
                default:
                    throw new Exception();
            }
        }

        public String ToStringHelper()
        {
            _path += "->ToStringHelper";
            switch (_target)
            {
                case 0:
                    return "VT";
                case 1:
                    _target = 0;
                    return ToStringHelper();
                case 2:
                    _target = 0;
                    return ToString();
                default:
                    throw new Exception();
            }
        }

        private bool RunTests()
        {
            _target = 0;
            _path = null;
            Console.WriteLine(ToStringHelper() + " : " + _path);
            if (_path != "->ToStringHelper")
                return false;
            _target = 1;
            _path = null;
            Console.WriteLine(ToStringHelper() + " : " + _path);
            if (_path != "->ToStringHelper->ToStringHelper")
                return false;
            _target = 2;
            _path = null;
            Console.WriteLine(ToStringHelper() + " : " + _path);
            if (_path != "->ToStringHelper->ToString")
                return false;
            _target = 0;
            _path = null;
            Console.WriteLine(ToString() + " : " + _path);
            if (_path != "->ToString")
                return false;
            _target = 1;
            _path = null;
            Console.WriteLine(ToString() + " : " + _path);
            if (_path != "->ToString->ToStringHelper")
                return false;
            _target = 2;
            _path = null;
            Console.WriteLine(ToString() + " : " + _path);
            if (_path != "->ToString->ToString")
                return false;
            return true;
        }

        private static int Main()
        {
            if (new VT().RunTests())
            {
                Console.WriteLine("PASSED.");
                return 100;
            }
            Console.WriteLine("FAILED.");
            return 101;
        }
    }
}
