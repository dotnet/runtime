﻿// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// The MIT License(MIT)
// =====================
//
// Copyright \u00A9 `2015-2017` `Lucas Meijer`
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Mono.Linker.Tests.Extensions
{
	public class NPath : IEquatable<NPath>, IComparable
	{
		private static readonly StringComparison PathStringComparison = IsLinux () ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

		private readonly string[] _elements;
		private readonly bool _isRelative;
		private readonly string? _driveLetter;

		#region construction

		public NPath (string path)
		{
			ArgumentNullException.ThrowIfNull (path);

			path = ParseDriveLetter (path, out _driveLetter);

			if (path == "/") {
				_isRelative = false;
				_elements = Array.Empty<string> ();
			} else {
				var split = path.Split ('/', '\\');

				_isRelative = _driveLetter == null && IsRelativeFromSplitString (split);

				_elements = ParseSplitStringIntoElements (split.Where (s => s.Length > 0).ToArray ());
			}
		}

		private NPath (string[] elements, bool isRelative, string? driveLetter)
		{
			_elements = elements;
			_isRelative = isRelative;
			_driveLetter = driveLetter;
		}

		private string[] ParseSplitStringIntoElements (IEnumerable<string> inputs)
		{
			var stack = new List<string> ();

			foreach (var input in inputs.Where (input => input.Length != 0)) {
				if (input == ".") {
					if ((stack.Count > 0) && (stack.Last () != "."))
						continue;
				} else if (input == "..") {
					if (HasNonDotDotLastElement (stack)) {
						stack.RemoveAt (stack.Count - 1);
						continue;
					}
					if (!_isRelative)
						throw new ArgumentException ("You cannot create a path that tries to .. past the root");
				}
				stack.Add (input);
			}
			return stack.ToArray ();
		}

		private static bool HasNonDotDotLastElement (List<string> stack)
		{
			return stack.Count > 0 && stack[stack.Count - 1] != "..";
		}

		private static string ParseDriveLetter (string path, out string? driveLetter)
		{
			if (path.Length >= 2 && path[1] == ':') {
				driveLetter = path[0].ToString ();
				return path.Substring (2);
			}

			driveLetter = null;
			return path;
		}

		private static bool IsRelativeFromSplitString (string[] split)
		{
			if (split.Length < 2)
				return true;

			return split[0].Length != 0 || !split.Any (s => s.Length > 0);
		}

		public NPath Combine (params string[] append)
		{
			return Combine (append.Select (a => new NPath (a)).ToArray ());
		}

		public NPath Combine (params NPath[] append)
		{
			if (!append.All (p => p.IsRelative))
				throw new ArgumentException ("You cannot .Combine a non-relative path");

			return new NPath (ParseSplitStringIntoElements (_elements.Concat (append.SelectMany (p => p._elements))), _isRelative, _driveLetter);
		}

		public NPath Parent {
			get {
				if (_elements.Length == 0)
					throw new InvalidOperationException ("Parent is called on an empty path");

				var newElements = _elements.Take (_elements.Length - 1).ToArray ();

				return new NPath (newElements, _isRelative, _driveLetter);
			}
		}

		public NPath RelativeTo (NPath path)
		{
			if (!IsChildOf (path)) {
				if (!IsRelative && !path.IsRelative && _driveLetter != path._driveLetter)
					throw new ArgumentException ("Path.RelativeTo() was invoked with two paths that are on different volumes. invoked on: " + ToString () + " asked to be made relative to: " + path);

				NPath? commonParent = null;
				foreach (var parent in RecursiveParents) {
					commonParent = path.RecursiveParents.FirstOrDefault (otherParent => otherParent == parent);

					if (commonParent != null)
						break;
				}

				if (commonParent == null)
					throw new ArgumentException ("Path.RelativeTo() was unable to find a common parent between " + ToString () + " and " + path);

				if (IsRelative && path.IsRelative && commonParent.IsEmpty ())
					throw new ArgumentException ("Path.RelativeTo() was invoked with two relative paths that do not share a common parent.  Invoked on: " + ToString () + " asked to be made relative to: " + path);

				var depthDiff = path.Depth - commonParent.Depth;
				return new NPath (Enumerable.Repeat ("..", depthDiff).Concat (_elements.Skip (commonParent.Depth)).ToArray (), true, null);
			}

			return new NPath (_elements.Skip (path._elements.Length).ToArray (), true, null);
		}

		public NPath ChangeExtension (string extension)
		{
			ThrowIfRoot ();

			var newElements = (string[]) _elements.Clone ();
			newElements[newElements.Length - 1] = Path.ChangeExtension (_elements[_elements.Length - 1], WithDot (extension));
			if (extension == string.Empty)
				newElements[newElements.Length - 1] = newElements[newElements.Length - 1].TrimEnd ('.');
			return new NPath (newElements, _isRelative, _driveLetter);
		}
		#endregion construction

		#region inspection

		public bool IsRelative {
			get { return _isRelative; }
		}

		public string FileName {
			get {
				ThrowIfRoot ();

				return _elements.Last ();
			}
		}

		public string FileNameWithoutExtension {
			get { return Path.GetFileNameWithoutExtension (FileName); }
		}

		public IEnumerable<string> Elements {
			get { return _elements; }
		}

		public int Depth {
			get { return _elements.Length; }
		}

		public bool Exists (string append = "")
		{
			return Exists (new NPath (append));
		}

		public bool Exists (NPath append)
		{
			return FileExists (append) || DirectoryExists (append);
		}

		public bool DirectoryExists (string append = "")
		{
			return DirectoryExists (new NPath (append));
		}

		public bool DirectoryExists (NPath append)
		{
			return Directory.Exists (Combine (append).ToString ());
		}

		public bool FileExists (string append = "")
		{
			return FileExists (new NPath (append));
		}

		public bool FileExists (NPath append)
		{
			return File.Exists (Combine (append).ToString ());
		}

		public string ExtensionWithDot {
			get {
				if (IsRoot)
					throw new ArgumentException ("A root directory does not have an extension");

				var last = _elements.Last ();
				var index = last.LastIndexOf (".");
				if (index < 0) return string.Empty;
				return last.Substring (index);
			}
		}

		public string InQuotes ()
		{
			return "\"" + ToString () + "\"";
		}

		public string InQuotes (SlashMode slashMode)
		{
			return "\"" + ToString (slashMode) + "\"";
		}

		public override string ToString ()
		{
			return ToString (SlashMode.Native);
		}

		public string ToString (SlashMode slashMode)
		{
			// Check if it's linux root /
			if (IsRoot && string.IsNullOrEmpty (_driveLetter))
				return Slash (slashMode).ToString ();

			if (_isRelative && _elements.Length == 0)
				return ".";

			var sb = new StringBuilder ();
			if (_driveLetter != null) {
				sb.Append (_driveLetter);
				sb.Append (':');
			}
			if (!_isRelative)
				sb.Append (Slash (slashMode));
			var first = true;
			foreach (var element in _elements) {
				if (!first)
					sb.Append (Slash (slashMode));

				sb.Append (element);
				first = false;
			}
			return sb.ToString ();
		}

		public static implicit operator string (NPath path)
		{
			return path.ToString ();
		}

		private static char Slash (SlashMode slashMode)
		{
			return slashMode switch {
				SlashMode.Backward => '\\',
				SlashMode.Forward => '/',
				_ => Path.DirectorySeparatorChar,
			};
		}

		public override bool Equals (object? obj)
		{
			if (obj == null)
				return false;

			// If parameter cannot be cast to Point return false.
			if (!(obj is NPath p))
				return false;

			return Equals (p);
		}

		public bool Equals (NPath? p)
		{
			if (p == null)
				return false;

			if (p._isRelative != _isRelative)
				return false;

			if (!string.Equals (p._driveLetter, _driveLetter, PathStringComparison))
				return false;

			if (p._elements.Length != _elements.Length)
				return false;

			for (var i = 0; i < _elements.Length; i++)
				if (!string.Equals (p._elements[i], _elements[i], PathStringComparison))
					return false;

			return true;
		}

		public static bool operator == (NPath? a, NPath? b)
		{
			// If both are null, or both are same instance, return true.
			if (ReferenceEquals (a, b))
				return true;

			// If one is null, but not both, return false.
			if ((a is null) || (b is null))
				return false;

			// Return true if the fields match:
			return a.Equals (b);
		}

		public override int GetHashCode ()
		{
			unchecked {
				int hash = 17;
				// Suitable nullity checks etc, of course :)
				hash = hash * 23 + _isRelative.GetHashCode ();
				foreach (var element in _elements)
					hash = hash * 23 + element.GetHashCode ();
				if (_driveLetter != null)
					hash = hash * 23 + _driveLetter.GetHashCode ();
				return hash;
			}
		}

		public int CompareTo (object? obj)
		{
			if (obj == null)
				return -1;

			return this.ToString ().CompareTo (((NPath) obj).ToString ());
		}

		public static bool operator != (NPath? a, NPath? b)
		{
			return !(a == b);
		}

		public bool HasExtension (params string[] extensions)
		{
			var extensionWithDotLower = ExtensionWithDot.ToLowerInvariant ();
			return extensions.Any (e => WithDot (e).ToLowerInvariant () == extensionWithDotLower);
		}

		private static string WithDot (string extension)
		{
			return extension.StartsWith (".") ? extension : "." + extension;
		}

		private bool IsEmpty ()
		{
			return _elements.Length == 0;
		}

		public bool IsRoot {
			get { return _elements.Length == 0 && !_isRelative; }
		}

		#endregion inspection

		#region directory enumeration

		public IEnumerable<NPath> Files (string filter, bool recurse = false)
		{
			return Directory.GetFiles (ToString (), filter, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Select (s => new NPath (s));
		}

		public IEnumerable<NPath> Files (bool recurse = false)
		{
			return Files ("*", recurse);
		}

		public IEnumerable<NPath> Contents (string filter, bool recurse = false)
		{
			return Files (filter, recurse).Concat (Directories (filter, recurse));
		}

		public IEnumerable<NPath> Contents (bool recurse = false)
		{
			return Contents ("*", recurse);
		}

		public IEnumerable<NPath> Directories (string filter, bool recurse = false)
		{
			return Directory.GetDirectories (ToString (), filter, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly).Select (s => new NPath (s));
		}

		public IEnumerable<NPath> Directories (bool recurse = false)
		{
			return Directories ("*", recurse);
		}

		#endregion

		#region filesystem writing operations
		public NPath CreateFile ()
		{
			ThrowIfRelative ();
			ThrowIfRoot ();
			EnsureParentDirectoryExists ();
			File.WriteAllBytes (ToString (), Array.Empty<byte> ());
			return this;
		}

		public NPath CreateFile (string file)
		{
			return CreateFile (new NPath (file));
		}

		public NPath CreateFile (NPath file)
		{
			if (!file.IsRelative)
				throw new ArgumentException ("You cannot call CreateFile() on an existing path with a non relative argument");
			return Combine (file).CreateFile ();
		}

		public NPath CreateDirectory ()
		{
			ThrowIfRelative ();

			if (IsRoot)
				throw new NotSupportedException ("CreateDirectory is not supported on a root level directory because it would be dangerous:" + ToString ());

			Directory.CreateDirectory (ToString ());
			return this;
		}

		public NPath CreateDirectory (string directory)
		{
			return CreateDirectory (new NPath (directory));
		}

		public NPath CreateDirectory (NPath directory)
		{
			if (!directory.IsRelative)
				throw new ArgumentException ("Cannot call CreateDirectory with an absolute argument");

			return Combine (directory).CreateDirectory ();
		}

		public NPath? Copy (string dest)
		{
			return Copy (new NPath (dest));
		}

		public NPath? Copy (string dest, Func<NPath, bool> fileFilter)
		{
			return Copy (new NPath (dest), fileFilter);
		}

		public NPath? Copy (NPath dest)
		{
			return Copy (dest, p => true);
		}

		public NPath? Copy (NPath dest, Func<NPath, bool> fileFilter)
		{
			ThrowIfRelative ();
			if (dest.IsRelative)
				dest = Parent.Combine (dest);

			if (dest.DirectoryExists ())
				return CopyWithDeterminedDestination (dest.Combine (FileName), fileFilter);

			return CopyWithDeterminedDestination (dest, fileFilter);
		}

		public NPath MakeAbsolute ()
		{
			if (!IsRelative)
				return this;

			return CurrentDirectory.Combine (this);
		}

		private NPath? CopyWithDeterminedDestination (NPath absoluteDestination, Func<NPath, bool> fileFilter)
		{
			if (absoluteDestination.IsRelative)
				throw new ArgumentException ("absoluteDestination must be absolute");

			if (FileExists ()) {
				if (!fileFilter (absoluteDestination))
					return null;

				absoluteDestination.EnsureParentDirectoryExists ();

				File.Copy (ToString (), absoluteDestination.ToString (), true);
				return absoluteDestination;
			}

			if (DirectoryExists ()) {
				absoluteDestination.EnsureDirectoryExists ();
				foreach (var thing in Contents ())
					thing.CopyWithDeterminedDestination (absoluteDestination.Combine (thing.RelativeTo (this)), fileFilter);
				return absoluteDestination;
			}

			throw new ArgumentException ("Copy() called on path that doesnt exist: " + ToString ());
		}

		public void Delete (DeleteMode deleteMode = DeleteMode.Normal)
		{
			ThrowIfRelative ();

			if (IsRoot)
				throw new NotSupportedException ("Delete is not supported on a root level directory because it would be dangerous:" + ToString ());

			if (FileExists ())
				File.Delete (ToString ());
			else if (DirectoryExists ())
				try {
					Directory.Delete (ToString (), true);
				} catch (IOException) {
					if (deleteMode == DeleteMode.Normal)
						throw;
				}
			else
				throw new InvalidOperationException ("Trying to delete a path that does not exist: " + ToString ());
		}

		public void DeleteIfExists (DeleteMode deleteMode = DeleteMode.Normal)
		{
			ThrowIfRelative ();

			if (FileExists () || DirectoryExists ())
				Delete (deleteMode);
		}

		public NPath DeleteContents ()
		{
			ThrowIfRelative ();

			if (IsRoot)
				throw new NotSupportedException ("DeleteContents is not supported on a root level directory because it would be dangerous:" + ToString ());

			if (FileExists ())
				throw new InvalidOperationException ("It is not valid to perform this operation on a file");

			if (DirectoryExists ()) {
				try {
					Files ().Delete ();
					Directories ().Delete ();
				} catch (IOException) {
					if (Files (true).Any ())
						throw;
				}

				return this;
			}

			return EnsureDirectoryExists ();
		}

		public static NPath CreateTempDirectory (string myprefix)
		{
			var random = new Random ();
			while (true) {
				var candidate = new NPath (Path.GetTempPath () + "/" + myprefix + "_" + random.Next ());
				if (!candidate.Exists ())
					return candidate.CreateDirectory ();
			}
		}

		public NPath Move (string dest)
		{
			return Move (new NPath (dest));
		}

		public NPath Move (NPath dest)
		{
			ThrowIfRelative ();

			if (IsRoot)
				throw new NotSupportedException ("Move is not supported on a root level directory because it would be dangerous:" + ToString ());

			if (dest.IsRelative)
				return Move (Parent.Combine (dest));

			if (dest.DirectoryExists ())
				return Move (dest.Combine (FileName));

			if (FileExists ()) {
				dest.EnsureParentDirectoryExists ();
				File.Move (ToString (), dest.ToString ());
				return dest;
			}

			if (DirectoryExists ()) {
				Directory.Move (ToString (), dest.ToString ());
				return dest;
			}

			throw new ArgumentException ("Move() called on a path that doesn't exist: " + ToString ());
		}

		#endregion

		#region special paths

		public static NPath CurrentDirectory {
			get {
				return new NPath (Directory.GetCurrentDirectory ());
			}
		}

		public static NPath HomeDirectory {
			get {
				if (Path.DirectorySeparatorChar == '\\')
					return new NPath (Environment.GetEnvironmentVariable ("USERPROFILE")!);
				return new NPath (Environment.GetEnvironmentVariable ("HOME")!);
			}
		}

		public static NPath SystemTemp {
			get {
				return new NPath (Path.GetTempPath ());
			}
		}

		#endregion

		private void ThrowIfRelative ()
		{
			if (_isRelative)
				throw new ArgumentException ("You are attempting an operation on a Path that requires an absolute path, but the path is relative");
		}

		private void ThrowIfRoot ()
		{
			if (IsRoot)
				throw new ArgumentException ("You are attempting an operation that is not valid on a root level directory");
		}

		public NPath EnsureDirectoryExists (string append = "")
		{
			return EnsureDirectoryExists (new NPath (append));
		}

		public NPath EnsureDirectoryExists (NPath append)
		{
			var combined = Combine (append);
			if (combined.DirectoryExists ())
				return combined;
			combined.EnsureParentDirectoryExists ();
			combined.CreateDirectory ();
			return combined;
		}

		public NPath EnsureParentDirectoryExists ()
		{
			var parent = Parent;
			parent.EnsureDirectoryExists ();
			return parent;
		}

		public NPath FileMustExist ()
		{
			if (!FileExists ())
				throw new FileNotFoundException ("File was expected to exist : " + ToString ());

			return this;
		}

		public NPath DirectoryMustExist ()
		{
			if (!DirectoryExists ())
				throw new DirectoryNotFoundException ("Expected directory to exist : " + ToString ());

			return this;
		}

		public bool IsChildOf (string potentialBasePath)
		{
			return IsChildOf (new NPath (potentialBasePath));
		}

		public bool IsChildOf (NPath potentialBasePath)
		{
			if ((IsRelative && !potentialBasePath.IsRelative) || !IsRelative && potentialBasePath.IsRelative)
				throw new ArgumentException ("You can only call IsChildOf with two relative paths, or with two absolute paths");

			// If the other path is the root directory, then anything is a child of it as long as it's not a Windows path
			if (potentialBasePath.IsRoot) {
				if (_driveLetter != potentialBasePath._driveLetter)
					return false;
				return true;
			}

			if (IsEmpty ())
				return false;

			if (Equals (potentialBasePath))
				return true;

			return Parent.IsChildOf (potentialBasePath);
		}

		public IEnumerable<NPath> RecursiveParents {
			get {
				var candidate = this;
				while (true) {
					if (candidate.IsEmpty ())
						yield break;

					candidate = candidate.Parent;
					yield return candidate;
				}
			}
		}

		public NPath WriteAllText (string contents)
		{
			ThrowIfRelative ();
			EnsureParentDirectoryExists ();
			File.WriteAllText (ToString (), contents);
			return this;
		}

		public string ReadAllText ()
		{
			ThrowIfRelative ();
			return File.ReadAllText (ToString ());
		}

		public NPath WriteAllLines (string[] contents)
		{
			ThrowIfRelative ();
			EnsureParentDirectoryExists ();
			File.WriteAllLines (ToString (), contents);
			return this;
		}

		public string[] ReadAllLines ()
		{
			ThrowIfRelative ();
			return File.ReadAllLines (ToString ());
		}

		public IEnumerable<NPath?> CopyFiles (NPath destination, bool recurse, Func<NPath, bool>? fileFilter = null)
		{
			destination.EnsureDirectoryExists ();
			return Files (recurse).Where (fileFilter ?? AlwaysTrue).Select (file => file.Copy (destination.Combine (file.RelativeTo (this)))).ToArray ();
		}

		public IEnumerable<NPath> MoveFiles (NPath destination, bool recurse, Func<NPath, bool>? fileFilter = null)
		{
			if (IsRoot)
				throw new NotSupportedException ("MoveFiles is not supported on this directory because it would be dangerous:" + ToString ());

			destination.EnsureDirectoryExists ();
			return Files (recurse).Where (fileFilter ?? AlwaysTrue).Select (file => file.Move (destination.Combine (file.RelativeTo (this)))).ToArray ();
		}

		private static bool AlwaysTrue (NPath p)
		{
			return true;
		}

		private static bool IsLinux ()
		{
			return Directory.Exists ("/proc");
		}
	}

	public static class Extensions
	{
		public static IEnumerable<NPath?> Copy (this IEnumerable<NPath> self, string dest)
		{
			return Copy (self, new NPath (dest));
		}

		public static IEnumerable<NPath?> Copy (this IEnumerable<NPath> self, NPath dest)
		{
			if (dest.IsRelative)
				throw new ArgumentException ("When copying multiple files, the destination cannot be a relative path");
			dest.EnsureDirectoryExists ();
			return self.Select (p => p.Copy (dest.Combine (p.FileName))).ToArray ();
		}

		public static IEnumerable<NPath> Move (this IEnumerable<NPath> self, string dest)
		{
			return Move (self, new NPath (dest));
		}

		public static IEnumerable<NPath> Move (this IEnumerable<NPath> self, NPath dest)
		{
			if (dest.IsRelative)
				throw new ArgumentException ("When moving multiple files, the destination cannot be a relative path");
			dest.EnsureDirectoryExists ();
			return self.Select (p => p.Move (dest.Combine (p.FileName))).ToArray ();
		}

		public static IEnumerable<NPath> Delete (this IEnumerable<NPath> self)
		{
			foreach (var p in self)
				p.Delete ();
			return self;
		}

		public static IEnumerable<string> InQuotes (this IEnumerable<NPath> self, SlashMode forward = SlashMode.Native)
		{
			return self.Select (p => p.InQuotes (forward));
		}

		public static NPath ToNPath (this string path)
		{
			return new NPath (path);
		}
	}

	public enum SlashMode
	{
		Native,
		Forward,
		Backward
	}

	public enum DeleteMode
	{
		Normal,
		Soft
	}
}
