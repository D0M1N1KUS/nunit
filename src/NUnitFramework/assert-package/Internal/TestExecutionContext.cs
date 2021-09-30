// Copyright (c) Charlie Poole, Rob Prouse and Contributors. MIT License - see LICENSE.txt

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Security.Principal;
using System.Threading;
using NUnit.Compatibility;
using NUnit.Framework.Constraints;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal.Execution;

#if NETFRAMEWORK
using System.Runtime.Remoting.Messaging;
#endif

namespace NUnit.Framework.Internal
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
        /// Link to a prior saved context
        /// </summary>
        private readonly TestExecutionContext _priorContext;

        /// <summary>
        /// The event listener currently receiving notifications
        /// </summary>
        private ITestListener _listener = TestListener.NULL;

        /// <summary>
        /// The number of assertions for the current test
        /// </summary>
        private int _assertCount;

        /// <summary>
        /// The current test result
        /// </summary>
        private TestResult _currentResult;

        private SandboxedThreadState _sandboxedThreadState;

#endregion

#region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="TestExecutionContext"/> class.
        /// </summary>
        public TestExecutionContext()
        {
            _priorContext = null;

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
            _priorContext = other;
            
            CurrentResult = other.CurrentResult;
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
        /// The time the current test started in Ticks
        /// </summary>
        public long StartTicks { get; set; }

        /// <summary>
        /// Gets the elapsed time for running the test in seconds
        /// </summary>
        public double Duration
        {
            get
            {
                var tickCount = Stopwatch.GetTimestamp() - StartTicks;
                return (double)tickCount / Stopwatch.Frequency;
            }
        }

        /// <summary>
        /// Gets or sets the current test result
        /// </summary>
        public TestResult CurrentResult
        {
            get { return _currentResult; }
            set
            {
                _currentResult = value;
                if (value != null)
                    OutWriter = value.OutWriter;
            }
        }

        /// <summary>
        /// Gets a TextWriter that will send output to the current test result.
        /// </summary>
        public TextWriter OutWriter { get; private set; }

        /// <summary>
        /// The current test event listener
        /// </summary>
        internal ITestListener Listener
        {
            get { return _listener; }
            set { _listener = value; }
        }

        /// <summary>
        /// Default tolerance value used for floating point equality
        /// when no other tolerance is specified.
        /// </summary>
        public Tolerance DefaultFloatingPointTolerance { get; set; }

        /// <summary>
        /// Gets the assert count.
        /// </summary>
        /// <value>The assert count.</value>
        internal int AssertCount
        {
            get { return _assertCount; }
        }

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
        /// Set up the execution environment to match a context.
        /// Note that we may be running on the same thread where the
        /// context was initially created or on a different thread.
        /// </summary>
        [SecuritySafeCritical] // This gives partial trust code the ability to capture an existing
                               // SynchronizationContext.Current and restore it at any time.
                               // This simply unblocks us on .NET Framework and is not in the spirit
                               // of partial trust. If we choose to make partial trust a design priority,
                               // we’ll need to thoroughly review more than just this instance.
        public void EstablishExecutionEnvironment()
        {
            _sandboxedThreadState.Restore();
            CurrentContext = this;
        }

        /// <summary>
        /// Increments the assert count by one.
        /// </summary>
        public void IncrementAssertCount()
        {
            Interlocked.Increment(ref _assertCount);
        }

        /// <summary>
        /// Increments the assert count by a specified amount.
        /// </summary>
        public void IncrementAssertCount(int count)
        {
            // TODO: Temporary implementation
            while (count-- > 0)
                Interlocked.Increment(ref _assertCount);
        }
        
        #endregion
    }
}
