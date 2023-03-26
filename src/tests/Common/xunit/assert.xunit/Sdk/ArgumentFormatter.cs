#if XUNIT_NULLABLE
#nullable enable
#endif

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Xunit.Sdk
{
	/// <summary>
	/// Formats arguments for display in theories.
	/// </summary>
	static class ArgumentFormatter
	{
		const int MAX_DEPTH = 3;
		const int MAX_ENUMERABLE_LENGTH = 5;
		const int MAX_OBJECT_PARAMETER_COUNT = 5;
		const int MAX_STRING_LENGTH = 50;

		static readonly object[] EmptyObjects = new object[0];
		static readonly Type[] EmptyTypes = new Type[0];

		// List of system types => C# type names
		static readonly Dictionary<TypeInfo, string> TypeMappings = new Dictionary<TypeInfo, string>
		{
			{ typeof(bool).GetTypeInfo(), "bool" },
			{ typeof(byte).GetTypeInfo(), "byte" },
			{ typeof(sbyte).GetTypeInfo(), "sbyte" },
			{ typeof(char).GetTypeInfo(), "char" },
			{ typeof(decimal).GetTypeInfo(), "decimal" },
			{ typeof(double).GetTypeInfo(), "double" },
			{ typeof(float).GetTypeInfo(), "float" },
			{ typeof(int).GetTypeInfo(), "int" },
			{ typeof(uint).GetTypeInfo(), "uint" },
			{ typeof(long).GetTypeInfo(), "long" },
			{ typeof(ulong).GetTypeInfo(), "ulong" },
			{ typeof(object).GetTypeInfo(), "object" },
			{ typeof(short).GetTypeInfo(), "short" },
			{ typeof(ushort).GetTypeInfo(), "ushort" },
			{ typeof(string).GetTypeInfo(), "string" },
		};

		/// <summary>
		/// Format the value for presentation.
		/// </summary>
		/// <param name="value">The value to be formatted.</param>
		/// <param name="pointerPosition">The position where the difference starts</param>
		/// <param name="errorIndex"></param>
		/// <returns>The formatted value.</returns>
		public static string Format(
#if XUNIT_NULLABLE
			object? value,
#else
			object value,
#endif
			out int? pointerPosition,
			int? errorIndex = null)
		{
			return Format(value, 1, out pointerPosition, errorIndex);
		}

		/// <summary>
		/// Format the value for presentation.
		/// </summary>
		/// <param name="value">The value to be formatted.</param>
		/// <param name="errorIndex"></param>
		/// <returns>The formatted value.</returns>
		public static string Format(
#if XUNIT_NULLABLE
			object? value,
#else
			object value,
#endif
			int? errorIndex = null)
		{
			int? _;

			return Format(value, 1, out _, errorIndex);
		}

		static string FormatInner(
#if XUNIT_NULLABLE
			object? value,
#else
			object value,
#endif
			int depth)
		{
			int? _;

			return Format(value, depth, out _, null);
		}

		static string Format(
#if XUNIT_NULLABLE
			object? value,
#else
			object value,
#endif
			int depth,
			out int? pointerPostion,
			int? errorIndex = null)
		{
			pointerPostion = null;

			if (value == null)
				return "null";

			var valueAsType = value as Type;
			if (valueAsType != null)
				return $"typeof({FormatTypeName(valueAsType)})";

			try
			{
				if (value.GetType().GetTypeInfo().IsEnum)
					return value.ToString()?.Replace(", ", " | ") ?? "null";

				if (value is char)
				{
					var charValue = (char)value;

					if (charValue == '\'')
						return @"'\''";

					// Take care of all of the escape sequences
#if XUNIT_NULLABLE
					string? escapeSequence;
#else
					string escapeSequence;
#endif
					if (TryGetEscapeSequence(charValue, out escapeSequence))
						return $"'{escapeSequence}'";

					if (char.IsLetterOrDigit(charValue) || char.IsPunctuation(charValue) || char.IsSymbol(charValue) || charValue == ' ')
						return $"'{charValue}'";

					// Fallback to hex
					return $"0x{(int)charValue:x4}";
				}

				if (value is DateTime || value is DateTimeOffset)
					return $"{value:o}";

				var stringParameter = value as string;
				if (stringParameter != null)
				{
					stringParameter = EscapeString(stringParameter);
					stringParameter = stringParameter.Replace(@"""", @"\"""); // escape double quotes
					if (stringParameter.Length > MAX_STRING_LENGTH)
					{
						var displayed = stringParameter.Substring(0, MAX_STRING_LENGTH);
						return $"\"{displayed}\"...";
					}

					return $"\"{stringParameter}\"";
				}

				try
				{
					var enumerable = value as IEnumerable;
					if (enumerable != null)
						return FormatEnumerable(enumerable.Cast<object>(), depth, errorIndex, out pointerPostion);
				}
				catch
				{
					// Sometimes enumerables cannot be enumerated when being, and instead thrown an exception.
					// This could be, for example, because they require state that is not provided by Xunit.
					// In these cases, just continue formatting.
				}

				if (value is float)
					return $"{value:G9}";

				if (value is double)
					return $"{value:G17}";

				var type = value.GetType();
				var typeInfo = type.GetTypeInfo();
				if (typeInfo.IsValueType)
				{
					if (typeInfo.IsGenericType && typeInfo.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
					{
						var k = typeInfo.GetDeclaredProperty("Key")?.GetValue(value, null);
						var v = typeInfo.GetDeclaredProperty("Value")?.GetValue(value, null);

						return $"[{Format(k)}] = {Format(v)}";
					}

					return Convert.ToString(value, CultureInfo.CurrentCulture) ?? "null";
				}

				var task = value as Task;
				if (task != null)
				{
					var typeParameters = typeInfo.GenericTypeArguments;
					var typeName = typeParameters.Length == 0 ? "Task" : $"Task<{string.Join(",", typeParameters.Select(FormatTypeName))}>";
					return $"{typeName} {{ Status = {task.Status} }}";
				}

				var toString = type.GetRuntimeMethod("ToString", EmptyTypes);

				if (toString != null && toString.DeclaringType != typeof(object))
#if XUNIT_NULLABLE
					return ((string?)toString.Invoke(value, EmptyObjects)) ?? "null";
#else
					return ((string)toString.Invoke(value, EmptyObjects)) ?? "null";
#endif

				return FormatComplexValue(value, depth, type);
			}
			catch (Exception ex)
			{
				// Sometimes an exception is thrown when formatting an argument, such as in ToString.
				// In these cases, we don't want xunit to crash, as tests may have passed despite this.
				return $"{ex.GetType().Name} was thrown formatting an object of type \"{value.GetType()}\"";
			}
		}

		static string FormatComplexValue(
			object value,
			int depth,
			Type type)
		{
			if (depth == MAX_DEPTH)
				return $"{type.Name} {{ ... }}";

			var fields =
				type
					.GetRuntimeFields()
					.Where(f => f.IsPublic && !f.IsStatic)
					.Select(f => new { name = f.Name, value = WrapAndGetFormattedValue(() => f.GetValue(value), depth) });

			var properties =
				type
					.GetRuntimeProperties()
					.Where(p => p.GetMethod != null && p.GetMethod.IsPublic && !p.GetMethod.IsStatic)
					.Select(p => new { name = p.Name, value = WrapAndGetFormattedValue(() => p.GetValue(value), depth) });

			var parameters =
				fields
					.Concat(properties)
					.OrderBy(p => p.name)
					.Take(MAX_OBJECT_PARAMETER_COUNT + 1)
					.ToList();

			if (parameters.Count == 0)
				return $"{type.Name} {{ }}";

			var formattedParameters = string.Join(", ", parameters.Take(MAX_OBJECT_PARAMETER_COUNT).Select(p => $"{p.name} = {p.value}"));

			if (parameters.Count > MAX_OBJECT_PARAMETER_COUNT)
				formattedParameters += ", ...";

			return $"{type.Name} {{ {formattedParameters} }}";
		}

		static string FormatEnumerable(
			IEnumerable<object> enumerableValues,
			int depth,
			int? neededIndex,
			out int? pointerPostion)
		{
			pointerPostion = null;

			if (depth == MAX_DEPTH)
				return "[...]";

			var printedValues = string.Empty;

			if (neededIndex.HasValue)
			{
				var enumeratedValues = enumerableValues.ToList();

				var half = (int)Math.Floor(MAX_ENUMERABLE_LENGTH / 2m);
				var startIndex = Math.Max(0, neededIndex.Value - half);
				var endIndex = Math.Min(enumeratedValues.Count, startIndex + MAX_ENUMERABLE_LENGTH);
				startIndex = Math.Max(0, endIndex - MAX_ENUMERABLE_LENGTH);

				var leftCount = neededIndex.Value - startIndex;

				if (startIndex != 0)
					printedValues += "..., ";

				var leftValues = enumeratedValues.Skip(startIndex).Take(leftCount).ToList();
				var rightValues = enumeratedValues.Skip(startIndex + leftCount).Take(MAX_ENUMERABLE_LENGTH - leftCount + 1).ToList();

				// Values to the left of the difference
				if (leftValues.Count > 0)
				{
					printedValues += string.Join(", ", leftValues.Select(x => FormatInner(x, depth + 1)));

					if (rightValues.Count > 0)
						printedValues += ", ";
				}

				pointerPostion = printedValues.Length + 1;

				// Difference value and values to the right
				printedValues += string.Join(", ", rightValues.Take(MAX_ENUMERABLE_LENGTH - leftCount).Select(x => FormatInner(x, depth + 1)));
				if (leftValues.Count + rightValues.Count > MAX_ENUMERABLE_LENGTH)
					printedValues += ", ...";
			}
			else
			{
				var values = enumerableValues.Take(MAX_ENUMERABLE_LENGTH + 1).ToList();
				printedValues += string.Join(", ", values.Take(MAX_ENUMERABLE_LENGTH).Select(x => FormatInner(x, depth + 1)));
				if (values.Count > MAX_ENUMERABLE_LENGTH)
					printedValues += ", ...";
			}

			return $"[{printedValues}]";
		}

		static bool IsSZArrayType(this TypeInfo typeInfo)
		{
#if NETCOREAPP2_0_OR_GREATER
			return typeInfo.IsSZArray;
#elif XUNIT_NULLABLE
			return typeInfo == typeInfo.GetElementType()!.MakeArrayType().GetTypeInfo();
#else
			return typeInfo == typeInfo.GetElementType().MakeArrayType().GetTypeInfo();
#endif
		}

		static string FormatTypeName(Type type)
		{
			var typeInfo = type.GetTypeInfo();
			var arraySuffix = "";

			// Deconstruct and re-construct array
			while (typeInfo.IsArray)
			{
				if (typeInfo.IsSZArrayType())
					arraySuffix += "[]";
				else
				{
					var rank = typeInfo.GetArrayRank();
					if (rank == 1)
						arraySuffix += "[*]";
					else
						arraySuffix += $"[{new string(',', rank - 1)}]";
				}

#if XUNIT_NULLABLE
				typeInfo = typeInfo.GetElementType()!.GetTypeInfo();
#else
				typeInfo = typeInfo.GetElementType().GetTypeInfo();
#endif
			}

			// Map C# built-in type names
#if XUNIT_NULLABLE
			string? result;
#else
			string result;
#endif
			if (TypeMappings.TryGetValue(typeInfo, out result))
				return result + arraySuffix;

			// Strip off generic suffix
			var name = typeInfo.FullName;

			// catch special case of generic parameters not being bound to a specific type:
			if (name == null)
				return typeInfo.Name;

			var tickIdx = name.IndexOf('`');
			if (tickIdx > 0)
				name = name.Substring(0, tickIdx);

			if (typeInfo.IsGenericTypeDefinition)
				name = $"{name}<{new string(',', typeInfo.GenericTypeParameters.Length - 1)}>";
			else if (typeInfo.IsGenericType)
			{
				if (typeInfo.GetGenericTypeDefinition() == typeof(Nullable<>))
					name = FormatTypeName(typeInfo.GenericTypeArguments[0]) + "?";
				else
					name = $"{name}<{string.Join(", ", typeInfo.GenericTypeArguments.Select(FormatTypeName))}>";
			}

			return name + arraySuffix;
		}

		static string WrapAndGetFormattedValue(
#if XUNIT_NULLABLE
			Func<object?> getter,
#else
			Func<object> getter,
#endif
			int depth)
		{
			try
			{
				return FormatInner(getter(), depth + 1);
			}
			catch (Exception ex)
			{
				return $"(throws {UnwrapException(ex)?.GetType().Name})";
			}
		}

		static Exception UnwrapException(Exception ex)
		{
			while (true)
			{
				var tiex = ex as TargetInvocationException;
				if (tiex == null || tiex.InnerException == null)
					return ex;

				ex = tiex.InnerException;
			}
		}

		internal static string EscapeString(string s)
		{
			var builder = new StringBuilder(s.Length);
			for (var i = 0; i < s.Length; i++)
			{
				var ch = s[i];
#if XUNIT_NULLABLE
				string? escapeSequence;
#else
				string escapeSequence;
#endif
				if (TryGetEscapeSequence(ch, out escapeSequence))
					builder.Append(escapeSequence);
				else if (ch < 32) // C0 control char
					builder.AppendFormat(@"\x{0}", (+ch).ToString("x2"));
				else if (char.IsSurrogatePair(s, i)) // should handle the case of ch being the last one
				{
					// For valid surrogates, append like normal
					builder.Append(ch);
					builder.Append(s[++i]);
				}
				// Check for stray surrogates/other invalid chars
				else if (char.IsSurrogate(ch) || ch == '\uFFFE' || ch == '\uFFFF')
				{
					builder.AppendFormat(@"\x{0}", (+ch).ToString("x4"));
				}
				else
					builder.Append(ch); // Append the char like normal
			}
			return builder.ToString();
		}

		static bool TryGetEscapeSequence(
			char ch,
#if XUNIT_NULLABLE
			out string? value)
#else
			out string value)
#endif
		{
			value = null;

			if (ch == '\t') // tab
				value = @"\t";
			if (ch == '\n') // newline
				value = @"\n";
			if (ch == '\v') // vertical tab
				value = @"\v";
			if (ch == '\a') // alert
				value = @"\a";
			if (ch == '\r') // carriage return
				value = @"\r";
			if (ch == '\f') // formfeed
				value = @"\f";
			if (ch == '\b') // backspace
				value = @"\b";
			if (ch == '\0') // null char
				value = @"\0";
			if (ch == '\\') // backslash
				value = @"\\";

			return value != null;
		}
	}
}
