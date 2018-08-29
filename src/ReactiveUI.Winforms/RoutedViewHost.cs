﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;

namespace ReactiveUI.Winforms
{
    [DefaultProperty("ViewModel")]
    public partial class RoutedControlHost : UserControl, IReactiveObject
    {
        private readonly CompositeDisposable _disposables = new CompositeDisposable();
        private RoutingState _Router;
        private Control _defaultContent;
        private IObservable<string> _viewContractObservable;

        public RoutedControlHost()
        {
            InitializeComponent();

            _disposables.Add(this.WhenAny(x => x.DefaultContent, x => x.Value).Subscribe(x =>
            {
                if (x != null && Controls.Count == 0)
                {
                    Controls.Add(InitView(x));
                    components.Add(DefaultContent);
                }
            }));

            ViewContractObservable = Observable<string>.Default;

            var vmAndContract =
                this.WhenAnyObservable(x => x.Router.CurrentViewModel)
                    .CombineLatest(this.WhenAnyObservable(x => x.ViewContractObservable),
                        (vm, contract) => new { ViewModel = vm, Contract = contract });

            Control viewLastAdded = null;
            _disposables.Add(vmAndContract.Subscribe(x =>
            {
                // clear all hosted controls (view or default content)
                Controls.Clear();

                if (viewLastAdded != null)
                {
                    viewLastAdded.Dispose();
                }

                if (x.ViewModel == null)
                {
                    if (DefaultContent != null)
                    {
                        InitView(DefaultContent);
                        Controls.Add(DefaultContent);
                    }

                    return;
                }

                IViewLocator viewLocator = ViewLocator ?? ReactiveUI.ViewLocator.Current;
                IViewFor view = viewLocator.ResolveView(x.ViewModel, x.Contract);
                view.ViewModel = x.ViewModel;

                viewLastAdded = InitView((Control)view);
                Controls.Add(viewLastAdded);
            }, RxApp.DefaultExceptionHandler.OnNext));
        }

        public event PropertyChangingEventHandler PropertyChanging
        {
            add => PropertyChangingEventManager.AddHandler(this, value);
            remove => PropertyChangingEventManager.RemoveHandler(this, value);
        }

        void IReactiveObject.RaisePropertyChanging(PropertyChangingEventArgs args)
        {
            PropertyChangingEventManager.DeliverEvent(this, args);
        }

        public event PropertyChangedEventHandler PropertyChanged
        {
            add => PropertyChangedEventManager.AddHandler(this, value);
            remove => PropertyChangedEventManager.RemoveHandler(this, value);
        }

        void IReactiveObject.RaisePropertyChanged(PropertyChangedEventArgs args)
        {
            PropertyChangedEventManager.DeliverEvent(this, args);
        }

        [Category("ReactiveUI")]
        [Description("The default control when no viewmodel is specified")]
        public Control DefaultContent
        {
            get => _defaultContent;
            set => this.RaiseAndSetIfChanged(ref _defaultContent, value);
        }

        [Category("ReactiveUI")]
        [Description("The router.")]
        public RoutingState Router
        {
            get => _Router;
            set => this.RaiseAndSetIfChanged(ref _Router, value);
        }

        [Browsable(false)]
        public IObservable<string> ViewContractObservable
        {
            get => _viewContractObservable;
            set => this.RaiseAndSetIfChanged(ref _viewContractObservable, value);
        }

        [Browsable(false)]
        public IViewLocator ViewLocator { get; set; }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
                _disposables.Dispose();
            }

            base.Dispose(disposing);
        }

        private Control InitView(Control view)
        {
            view.Dock = DockStyle.Fill;
            return view;
        }
    }
}
