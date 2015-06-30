using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SInnovations.ConfigurationManager.Configuration
{
    public interface IResetLazy
    {
        void Reset();
        void Load();
        Type DeclaringType { get; }
    }

    [ComVisible(false)]
    [HostProtection(Action = SecurityAction.LinkDemand, Resources = HostProtectionResource.Synchronization | HostProtectionResource.SharedState)]
    public class ResetLazy<T> : IResetLazy
    {
        class Box
        {
            public Box(T value)
            {
                this.Value = value;
            }

            public readonly T Value;
        }

        public ResetLazy(Func<T> valueFactory, LazyThreadSafetyMode mode = LazyThreadSafetyMode.PublicationOnly, Type declaringType = null)
        {
            if (valueFactory == null)
                throw new ArgumentNullException("valueFactory");

            this.mode = mode;
            this.valueFactory = valueFactory;
            this.declaringType = declaringType ?? valueFactory.Method.DeclaringType;
        }

        LazyThreadSafetyMode mode;
        Func<T> valueFactory;

        object syncLock = new object();

        Box box;

        Type declaringType;
        public Type DeclaringType
        {
            get { return declaringType; }
        }

        protected virtual T ValueFactory()
        {
            return valueFactory();
        }
        public T Value
        {
            get
            {
                var b1 = this.box;
                if (b1 != null)
                    return b1.Value;

                if (mode == LazyThreadSafetyMode.ExecutionAndPublication)
                {
                    lock (syncLock)
                    {
                        var b2 = box;
                        if (b2 != null)
                            return b2.Value;

                        this.box = new Box(ValueFactory());

                        return box.Value;
                    }
                }

                else if (mode == LazyThreadSafetyMode.PublicationOnly)
                {
                    var newValue = ValueFactory();

                    lock (syncLock)
                    {
                        var b2 = box;
                        if (b2 != null)
                            return b2.Value;

                        this.box = new Box(newValue);

                        return box.Value;
                    }
                }
                else
                {
                    var b = new Box(ValueFactory());
                    this.box = b;
                    return b.Value;
                }
            }
        }


        public void Load()
        {
            var a = Value;
        }

        public bool IsValueCreated
        {
            get { return box != null; }
        }

        public void Reset()
        {
            if (mode != LazyThreadSafetyMode.None)
            {
                lock (syncLock)
                {
                    this.box = null;
                }
            }
            else
            {
                this.box = null;
            }
        }
    }

    public class PeriodicResetLazy<T> : ResetLazy<T>
    {
        public static TimeSpan DEFAULT_RESET_TIMER = TimeSpan.FromMinutes(5);

        private readonly Func<bool> _onReset;
        private readonly TimeSpan _resetTimer;
        private System.Timers.Timer _idleCheckTimer;
        public PeriodicResetLazy(Func<T> valueFactory,Func<bool> onReset=null, TimeSpan? resetTimer = null, LazyThreadSafetyMode mode = LazyThreadSafetyMode.PublicationOnly, Type declaringType = null)
            : base(valueFactory,mode,declaringType)
        {
            _resetTimer = resetTimer.HasValue ? resetTimer.Value : DEFAULT_RESET_TIMER;
            _onReset = onReset;
        }

        protected override T ValueFactory()
        {
            SetResetTimer();
            return base.ValueFactory();
        }
       
        private void SetResetTimer()
        {

            _idleCheckTimer = new System.Timers.Timer(_resetTimer.TotalMilliseconds);
            _idleCheckTimer.AutoReset = false;
            _idleCheckTimer.Elapsed += new System.Timers.ElapsedEventHandler(OnIdleCheckTimer);
            _idleCheckTimer.Start();
        }

        private void OnIdleCheckTimer(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (_onReset != null && _onReset())
            {
                _idleCheckTimer = null;
                this.Reset();
            }
            else
            {
                SetResetTimer();
            }
        }
    }
}
