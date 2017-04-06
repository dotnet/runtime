// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;

// We have to run them with no parallelism because ServicePointManager.DefaultConnectionLimit is left as 2 in netfx and when they are running in parallel
// it leaves too many connections open at once which causes tests to hang since they are not able to get a new HttpWebRequest connection opened.
[assembly: CollectionBehavior(CollectionBehavior.CollectionPerAssembly, DisableTestParallelization = true, MaxParallelThreads = 1)]
