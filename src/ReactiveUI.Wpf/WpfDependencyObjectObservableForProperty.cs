﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Splat;

namespace ReactiveUI
{
    public class DependencyObjectObservableForProperty : ICreatesObservableForProperty
    {
        public int GetAffinityForObject(Type type, string propertyName, bool beforeChanged = false)
        {
            if (!typeof(DependencyObject).GetTypeInfo().IsAssignableFrom(type.GetTypeInfo()))
            {
                return 0;
            }

            return GetDependencyProperty(type, propertyName) != null ? 4 : 0;
        }

        public IObservable<IObservedChange<object, object>> GetNotificationForProperty(object sender, System.Linq.Expressions.Expression expression, string propertyName, bool beforeChanged = false)
        {
            var type = sender.GetType();
            var dpd = DependencyPropertyDescriptor.FromProperty(GetDependencyProperty(type, propertyName), type);

            if (dpd == null)
            {
                this.Log().Error("Couldn't find dependency property " + propertyName + " on " + type.Name);
                throw new NullReferenceException("Couldn't find dependency property " + propertyName + " on " + type.Name);
            }

            return Observable.Create<IObservedChange<object, object>>(subj =>
            {
                var handler = new EventHandler((o, e) =>
                {
                    subj.OnNext(new ObservedChange<object, object>(sender, expression));
                });

                dpd.AddValueChanged(sender, handler);
                return Disposable.Create(() => dpd.RemoveValueChanged(sender, handler));
            });
        }

        private DependencyProperty GetDependencyProperty(Type type, string propertyName)
        {
            var fi = type.GetTypeInfo().GetFields(BindingFlags.FlattenHierarchy | BindingFlags.Static | BindingFlags.Public)
                .FirstOrDefault(x => x.Name == propertyName + "Property" && x.IsStatic);

            if (fi != null)
            {
                return (DependencyProperty)fi.GetValue(null);
            }

            return null;
        }
    }
}
