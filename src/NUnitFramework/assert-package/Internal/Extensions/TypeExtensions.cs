// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;

namespace NUnit.AssertPackage.Internal.Extensions
{
    internal static class TypeExtensions
    {
        public static bool ImplementsIComparable(this Type type) =>
            type?.GetInterface("System.IComparable") != null;
    }
}
