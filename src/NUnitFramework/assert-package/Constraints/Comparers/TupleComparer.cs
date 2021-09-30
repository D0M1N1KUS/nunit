// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Reflection;
using NUnit.AssertPackage.Internal;

namespace NUnit.AssertPackage.Constraints.Comparers
{
    /// <summary>
    /// Comparator for two <c>Tuple</c>s.
    /// </summary>
    internal sealed class TupleComparer : TupleComparerBase
    {
        internal TupleComparer(NUnitEqualityComparer equalityComparer)
            : base(equalityComparer)
        { }

        protected override bool IsCorrectType(Type type)
        {
            return TypeHelper.IsTuple(type);
        }

        protected override object GetValue(Type type, string propertyName, object obj)
        {
            return type.GetProperty(propertyName).GetValue(obj, null);
        }
    }
}
