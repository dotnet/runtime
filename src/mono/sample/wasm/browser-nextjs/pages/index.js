// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

import Head from 'next/head'
import DeepThought from '../components/deepThought'

export default function Home() {
  return (
    <div className="container">
      <Head>
        <title>NextJS dotnet sample</title>
      </Head>

      <main>
        <h1 className="title">
          Welcome to dotnet sample
        </h1>
        <DeepThought />
        <DeepThought />
        <DeepThought />
        <DeepThought />
      </main>
    </div>
  )
}

