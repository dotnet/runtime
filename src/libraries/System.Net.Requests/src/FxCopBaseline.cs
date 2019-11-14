// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Microsoft.Design", "CA2237", Scope = "type", Target = "System.Net.FtpWebRequest")]
[assembly: SuppressMessage("Microsoft.Design", "CA2237", Scope = "type", Target = "System.Net.FtpWebResponse")]

[assembly: SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Scope = "member", Target = "System.Net.FileWebRequest.#CheckAndMarkAsyncGetRequestStreamPending()")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Scope = "member", Target = "System.Net.FileWebRequest.#CheckAndMarkAsyncGetResponsePending()")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Scope = "member", Target = "System.Net.FileWebRequest.#CreateResponse():System.Net.WebResponse")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Scope = "member", Target = "System.Net.FileWebRequest.#UnblockReader()")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Scope = "member", Target = "System.Net.CommandStream.#Abort(System.Exception)")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Scope = "member", Target = "System.Net.CommandStream.#ContinueCommandPipeline():System.IO.Stream")]
[assembly: SuppressMessage("Microsoft.Reliability", "CA2002:DoNotLockOnObjectsWithWeakIdentity", Scope = "member", Target = "System.Net.FtpWebResponse.#get_Headers():System.Net.WebHeaderCollection")]
