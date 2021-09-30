// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.Framework.Internal
{
    partial class ValueGenerator
    {
        private sealed class SByteValueGenerator : ValueGenerator<sbyte>
        {
            public override bool TryCreateStep(object value, out ValueGenerator.Step step)
            {
                if (value is sbyte)
                {
                    step = new ComparableStep<sbyte>((sbyte)value, (prev, stepValue) => checked((sbyte)(prev + stepValue)));
                    return true;
                }

                return base.TryCreateStep(value, out step);
            }
        }
    }
}
