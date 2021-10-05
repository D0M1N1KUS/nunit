// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.IO;
using System.Security;
using System.Threading;
using NUnit.AssertPackage.Compatibility;
using NUnit.AssertPackage.Constraints;
using NUnit.AssertPackage.Interfaces;
#if NETFRAMEWORK
using System.Runtime.Remoting.Messaging;
#endif

namespace NUnit.AssertPackage.Internal
{
    /// <summary>
    /// Helper class used to save and restore certain static or
    /// singleton settings in the environment that affect tests
    /// or which might be changed by the user tests.
    /// </summary>
    public class TestExecutionContext : LongLivedMarshalByRefObject
#if NETFRAMEWORK
        , ILogicalThreadAffinative
#endif
    {
        // NOTE: Be very careful when modifying this class. It uses
        // conditional compilation extensively and you must give
        // thought to whether any new features will be supported
        // on each platform. In particular, instance fields,
        // properties, initialization and restoration must all
        // use the same conditions for each feature.

        #region Instance Fields

        /// <summary>
        /// The event listener currently receiving notifications
        /// </summary>
        private ITestListener _listener = TestListener.NULL;

        /// <summary>
        /// The number of assertions for the current test
        /// </summary>
        private int _assertCount;

        private SandboxedThreadState _sandboxedThreadState;

#endregion

#region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestExecutionContext"/> class.
        /// </summary>
        public TestExecutionContext()
        {
            UpdateContextFromEnvironment();

            CurrentValueFormatter = (val) => MsgUtils.DefaultValueFormatter(val);
            DefaultFloatingPointTolerance = Tolerance.Default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TestExecutionContext"/> class.
        /// </summary>
        /// <param name="other">An existing instance of TestExecutionContext.</param>
        public TestExecutionContext(TestExecutionContext other)
        {
            _listener = other._listener;

            _sandboxedThreadState = other._sandboxedThreadState;

            DefaultFloatingPointTolerance = other.DefaultFloatingPointTolerance;

            CurrentValueFormatter = other.CurrentValueFormatter;
        }

#endregion

#region CurrentContext Instance

        // NOTE: We use different implementations for various platforms.

#if !NETFRAMEWORK
        private static readonly AsyncLocal<TestExecutionContext> _currentContext = new AsyncLocal<TestExecutionContext>();
        /// <summary>
        /// Gets and sets the current context.
        /// </summary>
        public static TestExecutionContext CurrentContext
        {
            get
            {
                return _currentContext.Value ?? (_currentContext.Value = new AdhocContext());
            }
            internal set // internal so that AdhocTestExecutionTests can get at it
            {
                _currentContext.Value = value;
            }
        }
#else
        // In all other builds, we use the CallContext
        /// <summary>
        /// Gets and sets the current context.
        /// </summary>
        public static TestExecutionContext CurrentContext
        {
            // This method invokes security critical members on the 'System.Runtime.Remoting.Messaging.CallContext' class.
            // Callers of this method have no influence on how these methods are used so we define a 'SecuritySafeCriticalAttribute'
            // rather than a 'SecurityCriticalAttribute' to enable use by security transparent callers.
            [SecuritySafeCritical]
            get
            {
                var context = CallContext.GetData(NUnitCallContext.TestExecutionContextKey) as TestExecutionContext;

                if (context == null)
                {
                    context = new TestExecutionContext();
                    CallContext.SetData(NUnitCallContext.TestExecutionContextKey, context);
                }

                return context;
            }
            // This method invokes security critical members on the 'System.Runtime.Remoting.Messaging.CallContext' class.
            // Callers of this method have no influence on how these methods are used so we define a 'SecuritySafeCriticalAttribute'
            // rather than a 'SecurityCriticalAttribute' to enable use by security transparent callers.
            [SecuritySafeCritical]
            private set
            {
                if (value == null)
                    CallContext.FreeNamedDataSlot(NUnitCallContext.TestExecutionContextKey);
                else
                    CallContext.SetData(NUnitCallContext.TestExecutionContextKey, value);
            }
        }
#endif

#endregion

#region Properties

/// <summary>
        /// Gets a TextWriter that will send output to the current test result.
        /// </summary>
        public TextWriter OutWriter { get; private set; }

/// <summary>
        /// Default tolerance value used for floating point equality
        /// when no other tolerance is specified.
        /// </summary>
        public Tolerance DefaultFloatingPointTolerance { get; set; }

/// <summary>
        /// The current nesting level of multiple assert blocks
        /// </summary>
        internal int MultipleAssertLevel { get; set; }
        
        /// <summary>
        /// The current head of the ValueFormatter chain, copied from MsgUtils.ValueFormatter
        /// </summary>
        public ValueFormatter CurrentValueFormatter { get; private set; }
        
#endregion

#region Instance Methods

        /// <summary>
        /// Record any changes in the environment made by
        /// the test code in the execution context so it
        /// will be passed on to lower level tests.
        /// </summary>
        public void UpdateContextFromEnvironment()
        {
            _sandboxedThreadState = SandboxedThreadState.Capture();
        }

        /// <summary>
        /// Increments the assert count by one.
        /// </summary>
        public void IncrementAssertCount()
        {
            Interlocked.Increment(ref _assertCount);
        }

        #endregion
        
        #region Nested IsolatedContext Class

        /// <summary>
        /// An IsolatedContext is used when running code
        /// that may effect the current result in ways that
        /// should not impact the final result of the test.
        /// A new TestExecutionContext is created with an
        /// initially clear result, which is discarded on
        /// exiting the context.
        /// </summary>
        /// <example>
        ///     using (new TestExecutionContext.IsolatedContext())
        ///     {
        ///         // Code that should not impact the result
        ///     }
        /// </example>
        public class IsolatedContext : IDisposable
        {
            private readonly TestExecutionContext _originalContext;

            /// <summary>
            /// Save the original current TestExecutionContext and
            /// make a new isolated context current.
            /// </summary>
            public IsolatedContext()
            {
                _originalContext = CurrentContext;
                CurrentContext = new TestExecutionContext(_originalContext);
            }

            /// <summary>
            /// Restore the original TestExecutionContext.
            /// </summary>
            public void Dispose()
            {
                // _originalContext.OutWriter.Write(CurrentContext.Output);
                CurrentContext = _originalContext;
            }
        }

        #endregion
        
        #region Nested AdhocTestExecutionContext

        /// <summary>
        /// An AdhocTestExecutionContext is created whenever a context is needed
        /// but not available in CurrentContext. This happens when tests are run
        /// on an ad-hoc basis or Asserts are used outside of tests.
        /// </summary>
        public class AdhocContext : TestExecutionContext
        {
        }

        #endregion
    }
}
