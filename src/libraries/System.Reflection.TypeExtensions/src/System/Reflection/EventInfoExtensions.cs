// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Reflection
{
    /// <summary>
    /// Extension methods offering source-code compatibility with certain instance methods of <see cref="System.Reflection.EventInfo"/> on other platforms.
    /// </summary>
    public static class EventInfoExtensions
    {
        /// <summary>
        /// Returns the method used to add an event handler delegate to the event source.
        /// </summary>
        /// <param name="eventInfo">EventInfo object on which to perform lookup</param>
        /// <returns>A MethodInfo object representing the method used to add an event handler delegate to the event source.</returns>
        public static MethodInfo GetAddMethod(this EventInfo eventInfo)
        {
            return GetAddMethod(eventInfo, nonPublic: false);
        }

        /// <summary>
        /// Retrieves the MethodInfo object for the AddEventHandler method of the event, specifying whether to return non-public methods.
        /// </summary>
        /// <param name="eventInfo">EventInfo object on which to perform lookup</param>
        /// <param name="nonPublic">true if non-public methods can be returned; otherwise, false. </param>
        /// <returns></returns>
        public static MethodInfo GetAddMethod(this EventInfo eventInfo, bool nonPublic)
        {
            Requires.NotNull(eventInfo, nameof(eventInfo));
            return Helpers.FilterAccessor(eventInfo.AddMethod, nonPublic);
        }

        /// <summary>
        /// Returns the method that is called when the event is raised.
        /// </summary>
        /// <param name="eventInfo">EventInfo object on which to perform lookup</param>
        /// <returns>The method that is called when the event is raised.</returns>
        public static MethodInfo GetRaiseMethod(this EventInfo eventInfo)
        {
            return GetRaiseMethod(eventInfo, nonPublic: false);
        }

        /// <summary>
        /// Returns the method that is called when the event is raised, specifying whether to return non-public methods.
        /// </summary>
        /// <param name="eventInfo">EventInfo object on which to perform lookup</param>
        /// <param name="nonPublic">true if non-public methods can be returned; otherwise, false. </param>
        /// <returns>A MethodInfo object that was called when the event was raised.</returns>
        public static MethodInfo GetRaiseMethod(this EventInfo eventInfo, bool nonPublic)
        {
            Requires.NotNull(eventInfo, nameof(eventInfo));
            return Helpers.FilterAccessor(eventInfo.RaiseMethod, nonPublic);
        }

        /// <summary>
        /// Returns the method used to remove an event handler delegate from the event source.
        /// </summary>
        /// <param name="eventInfo">EventInfo object on which to perform lookup</param>
        /// <returns>A MethodInfo object representing the method used to remove an event handler delegate from the event source.</returns>
        public static MethodInfo GetRemoveMethod(this EventInfo eventInfo)
        {
            return GetRemoveMethod(eventInfo, nonPublic: false);
        }

        /// <summary>
        /// Retrieves the MethodInfo object for removing a method of the event, specifying whether to return non-public methods.
        /// </summary>
        /// <param name="eventInfo">EventInfo object on which to perform lookup</param>
        /// <param name="nonPublic">true if non-public methods can be returned; otherwise, false. </param>
        /// <returns>A MethodInfo object representing the method used to remove an event handler delegate from the event source.</returns>
        public static MethodInfo GetRemoveMethod(this EventInfo eventInfo, bool nonPublic)
        {
            Requires.NotNull(eventInfo, nameof(eventInfo));
            return Helpers.FilterAccessor(eventInfo.RemoveMethod, nonPublic);
        }
    }
}
