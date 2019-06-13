// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

static partial class SR
{
	internal static string Format (string resourceFormat, object? p1)
	{
		return string.Format (CultureInfo.InvariantCulture, resourceFormat, p1);
	}

	internal static string Format (string resourceFormat, object p1, object p2)
	{
		return string.Format (CultureInfo.InvariantCulture, resourceFormat, p1, p2);
	}

	internal static string Format (CultureInfo ci, string resourceFormat, object p1, object p2)
	{
		return string.Format (ci, resourceFormat, p1, p2);
	}

	internal static string Format (string resourceFormat, object p1, object p2, object p3)
	{
		return string.Format (CultureInfo.InvariantCulture, resourceFormat, p1, p2, p3);
	}

	internal static string Format (string resourceFormat, params object[] args)
	{
		if (args != null)
			return string.Format (CultureInfo.InvariantCulture, resourceFormat, args);

		return resourceFormat;
	}

	internal static string GetResourceString (string str) => str;
}