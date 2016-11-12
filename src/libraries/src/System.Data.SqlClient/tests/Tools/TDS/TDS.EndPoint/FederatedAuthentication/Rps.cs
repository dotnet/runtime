// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Reflection;
using System.Linq;

namespace Microsoft.SqlServer.TDS.EndPoint.FederatedAuthentication
{
    /// <summary>
    /// Wrapper for dynamic loading of RPS dll
    /// </summary>
    internal class RPS
    {
        /// <summary>
        /// Instance of the dynamially loaded RPS assembly from Microsoft.Passport.RPS
        /// </summary>
        private Assembly _rpsAssembly = null;

        /// <summary>
        /// Type of Microsoft.Passport.RPS.RPS
        /// </summary>
        private readonly Type _rpsType = null;

        /// <summary>
        /// Type of Microsoft.Passport.RPS.RPSTicket
        /// </summary>
        private readonly Type _rpsTicketType = null;

        /// <summary>
        /// Type of Microsoft.Passport.RPS.RPSPropBag
        /// </summary>
        private readonly Type _rpsPropBagType = null;

        /// <summary>
        /// Type of Microsoft.Passport.RPS.RPSAuth
        /// </summary>
        private readonly Type _rpsAuthType = null;

        /// <summary>
        /// Type of Microsoft.Passport.RPS.RPSTicket.RPSTicketProperty
        /// </summary>
        private readonly Type _rpsTicketPropertyType = null;

        /// <summary>
        /// Instance of the Microsoft.Passport.RPS.RPS object
        /// </summary>
        private object _rps = null;

        /// <summary>
        /// RPS Wrapper constructor
        /// </summary>
        public RPS()
        {
            // Load dynamically the assembly
            _rpsAssembly = Assembly.Load("Microsoft.Passport.RPS, Version=6.1.6206.0, Culture=neutral, PublicKeyToken=283dd9fa4b2406c5, processorArchitecture=MSIL");

            // Extract the types that will be needed to perform authentication
            _rpsType = _rpsAssembly.GetTypes().ToList().Where(t => t.Name == "RPS").Single();
            _rpsTicketType = _rpsAssembly.GetTypes().ToList().Where(t => t.Name == "RPSTicket").Single();
            _rpsPropBagType = _rpsAssembly.GetTypes().ToList().Where(t => t.Name == "RPSPropBag").Single();
            _rpsAuthType = _rpsAssembly.GetTypes().ToList().Where(t => t.Name == "RPSAuth").Single();
            _rpsTicketPropertyType = _rpsTicketType.GetNestedTypes().ToList().Where(t => t.Name == "RPSTicketProperty").Single();

            // Create instance of the RPS object
            _rps = Activator.CreateInstance(_rpsType);
        }

        /// <summary>
        /// Calling Initialize in the RPS real object created from the dynamically loaded RPS assembly
        /// </summary>
        public void Initialize(string s)
        {
            // Call initialize in the previously created rps object
            _rpsType.GetMethod("Initialize").Invoke(_rps, new object[] { s });
        }

        /// <summary>
        /// Given an encrypted ticket, calls RPS Authenticate and returns the decrypted ticket 
        /// </summary>
        public object Authenticate(byte[] encryptedTicket, string siteName)
        {
            // Create instance of the rpsPropBag using the rps object
            object rpsPropBag = Activator.CreateInstance(_rpsPropBagType, new object[] { _rps });

            // Create instance of the rpsAuth Authenticator using the rps object
            object authenticator = Activator.CreateInstance(_rpsAuthType, new object[] { _rps });

            // Call Authenticate in the Authenticator object using the encrypted ticket and the site name provided
            return _rpsAuthType.GetMethod("Authenticate").Invoke(authenticator, new object[] { siteName, System.Text.Encoding.Unicode.GetString(encryptedTicket), (UInt32)2 /*compact ticket*/, rpsPropBag });
        }

        /// <summary>
        /// Given an rps decrypted ticket, get the session key
        /// </summary>
        public byte[] GetSessionKeyFromRpsDecryptedTicket(object rpsTicket)
        {
            // Take the Property field from the rpsTicket provided
            object rpsTicketProperty = _rpsTicketType.GetFields().ToList().Where(p => p.Name == "Property").Single().GetValue(rpsTicket);

            // Get the Property["SessionKey"] value from the rpsTicketProperty
            object sessionkey = _rpsTicketPropertyType.GetMethod("get_Item").Invoke(rpsTicketProperty, new object[] { "SessionKey" });

            // Return the Session Key as an array of bytes
            return GetStringAsBytes((string)sessionkey);
        }

        /// <summary>
        /// Convert a "string" that is actually a byte array into an actual byte array - needed for interop
        /// with COM methods that are returning binary data as a BSTR.
        /// </summary>
        private static byte[] GetStringAsBytes(string toConvert)
        {
            byte[] convertedBytes = new byte[toConvert.Length * sizeof(char)];
            for (int i = 0; i < toConvert.Length; i++)
            {
                byte[] charAsBytes = BitConverter.GetBytes(toConvert[i]);
                convertedBytes[(2 * i)] = charAsBytes[0];
                convertedBytes[(2 * i) + 1] = charAsBytes[1];
            }

            return convertedBytes;
        }
    }
}
