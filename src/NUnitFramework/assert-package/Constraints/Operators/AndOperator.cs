// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.AssertPackage.Constraints.Operators
{
    /// <summary>
    /// Operator that requires both its arguments to succeed
    /// </summary>
    public class AndOperator : BinaryOperator
    {
        /// <summary>
        /// Construct an AndOperator
        /// </summary>
        public AndOperator()
        {
            this.left_precedence = this.right_precedence = 2;
        }

        /// <summary>
        /// Apply the operator to produce an AndConstraint
        /// </summary>
        public override IConstraint ApplyOperator(IConstraint left, IConstraint right)
        {
            return new AndConstraint(left, right);
        }
    }
}
