// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Splat;

namespace ReactiveUI
{
    /// <summary>
    /// This is an Activity that is both an Activity and has ReactiveObject powers
    /// (i.e. you can call RaiseAndSetIfChanged).
    /// </summary>
    /// <typeparam name="TViewModel"></typeparam>
    public class ReactiveActivity<TViewModel> : ReactiveActivity, IViewFor<TViewModel>, ICanActivate
        where TViewModel : class
    {
        private TViewModel _ViewModel;

        public TViewModel ViewModel
        {
            get { return _ViewModel; }
            set { this.RaiseAndSetIfChanged(ref _ViewModel, value); }
        }

        object IViewFor.ViewModel
        {
            get { return _ViewModel; }
            set { _ViewModel = (TViewModel)value; }
        }

        protected ReactiveActivity()
        {
        }

        protected ReactiveActivity(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }
    }

    /// <summary>
    /// This is an Activity that is both an Activity and has ReactiveObject powers
    /// (i.e. you can call RaiseAndSetIfChanged).
    /// </summary>
    public class ReactiveActivity : Activity, IReactiveObject, IReactiveNotifyPropertyChanged<ReactiveActivity>, IHandleObservableErrors
    {
        public event PropertyChangingEventHandler PropertyChanging
        {
            add { PropertyChangingEventManager.AddHandler(this, value); }
            remove { PropertyChangingEventManager.RemoveHandler(this, value); }
        }

        void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args)
        {
            PropertyChangingEventManager.DeliverEvent(this, args);
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add { PropertyChangedEventManager.AddHandler(this, value); }
            remove { PropertyChangedEventManager.RemoveHandler(this, value); }
        }

        void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChangedEventManager.DeliverEvent(this, args);
        }

        /// <summary>
        /// Represents an Observable that fires *before* a property is about to
        /// be changed.
        /// </summary>
        public IObservable<IReactivePropertyChangedEventArgs<ReactiveActivity>> Changing
        {
            get { return this.GetChangingObservable(); }
        }

        /// <summary>
        /// Represents an Observable that fires *after* a property has changed.
        /// </summary>
        public IObservable<IReactivePropertyChangedEventArgs<ReactiveActivity>> Changed
        {
            get { return this.GetChangedObservable(); }
        }

        protected ReactiveActivity()
        {
        }

        protected ReactiveActivity(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        {
        }

        /// <summary>
        /// When this method is called, an object will not fire change
        /// notifications (neither traditional nor Observable notifications)
        /// until the return value is disposed.
        /// </summary>
        /// <returns>An object that, when disposed, reenables change
        /// notifications.</returns>
        public IDisposable SuppressChangeNotifications()
        {
            return IReactiveObjectExtensions.SuppressChangeNotifications(this);
        }

        public IObservable<Exception> ThrownExceptions
        {
            get { return this.GetThrownExceptionsObservable(); }
        }

        private readonly Subject<Unit> _activated = new Subject<Unit>();

        public IObservable<Unit> Activated
        {
            get { return _activated.AsObservable(); }
        }

        private readonly Subject<Unit> _deactivated = new Subject<Unit>();

        public IObservable<Unit> Deactivated
        {
            get { return _deactivated.AsObservable(); }
        }

        protected override void OnPause()
        {
            base.OnPause();
            _deactivated.OnNext(Unit.Default);
        }

        protected override void OnResume()
        {
            base.OnResume();
            _activated.OnNext(Unit.Default);
        }

        private readonly Subject<Tuple<int, Result, Intent>> _activityResult = new Subject<Tuple<int, Result, Intent>>();

        public IObservable<Tuple<int, Result, Intent>> ActivityResult
        {
            get { return _activityResult.AsObservable(); }
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            base.OnActivityResult(requestCode, resultCode, data);
            _activityResult.OnNext(Tuple.Create(requestCode, resultCode, data));
        }

        public Task<Tuple<Result, Intent>> StartActivityForResultAsync(Intent intent, int requestCode)
        {
            // NB: It's important that we set up the subscription *before* we
            // call ActivityForResult
            var ret = ActivityResult
                .Where(x => x.Item1 == requestCode)
                .Select(x => Tuple.Create(x.Item2, x.Item3))
                .FirstAsync()
                .ToTask();

            StartActivityForResult(intent, requestCode);
            return ret;
        }

        public Task<Tuple<Result, Intent>> StartActivityForResultAsync(Type type, int requestCode)
        {
            // NB: It's important that we set up the subscription *before* we
            // call ActivityForResult
            var ret = ActivityResult
                .Where(x => x.Item1 == requestCode)
                .Select(x => Tuple.Create(x.Item2, x.Item3))
                .FirstAsync()
                .ToTask();

            StartActivityForResult(type, requestCode);
            return ret;
        }
    }
}
