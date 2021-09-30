// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

namespace NUnit.AssertPackage.Constraints.Operators
{
    /// <summary>
    /// Operator that requires at least one of its arguments to succeed
    /// </summary>
    public class OrOperator : BinaryOperator
    {
        /// <summary>
        /// Construct an OrOperator
        /// </summary>
        public OrOperator()
        {
            this.left_precedence = this.right_precedence = 3;
        }

        /// <summary>
        /// Apply the operator to produce an OrConstraint
        /// </summary>
        public override IConstraint ApplyOperator(IConstraint left, IConstraint right)
        {
            return new OrConstraint(left, right);
        }
    }
}
