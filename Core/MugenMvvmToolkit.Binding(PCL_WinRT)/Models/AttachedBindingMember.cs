﻿#region Copyright
// ****************************************************************************
// <copyright file="AttachedBindingMember.cs">
// Copyright © Vyacheslav Volkov 2012-2014
// </copyright>
// ****************************************************************************
// <author>Vyacheslav Volkov</author>
// <email>vvs0205@outlook.com</email>
// <project>MugenMvvmToolkit</project>
// <web>https://github.com/MugenMvvmToolkit/MugenMvvmToolkit</web>
// <license>
// See license.txt in this solution or http://opensource.org/licenses/MS-PL
// </license>
// ****************************************************************************
#endregion
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using JetBrains.Annotations;
using MugenMvvmToolkit.Binding.Core;
using MugenMvvmToolkit.Binding.Interfaces.Models;
using MugenMvvmToolkit.Binding.Models.EventArg;
using MugenMvvmToolkit.Infrastructure;

namespace MugenMvvmToolkit.Binding.Models
{
    /// <summary>
    ///     Represents the helper class to create attached binding members.
    /// </summary>
    public static class AttachedBindingMember
    {
        #region Nested types

        private sealed class ObservableProperty<TTarget>
        {
            #region Nested types

            private sealed class EventInvoker
            {
                #region Events

                public event EventHandler Changed;

                public void RaiseChanged(object sender)
                {
                    var handler = Changed;
                    if (handler != null)
                        handler(sender, EventArgs.Empty);
                }

                #endregion
            }

            #endregion

            #region Fields

            private const string ListenerMember = ".EventInvoker";
            private static readonly Func<object, object, EventInvoker> EventInvokerCreator = (o, state) => new EventInvoker();
            private readonly Func<IBindingMemberInfo, TTarget, object[], bool> _setValue;

            #endregion

            #region Constructors

            public ObservableProperty(Func<IBindingMemberInfo, TTarget, object[], bool> setValue)
            {
                Should.NotBeNull(setValue, "setValue");
                _setValue = setValue;
            }

            #endregion

            #region Methods

            public IDisposable ObserveMember(IBindingMemberInfo arg1, TTarget arg2, IEventListener arg3)
            {
                var invoker = ServiceProvider
                    .AttachedValueProvider
                    .GetOrAdd(arg2, GetMemberPath(arg1), EventInvokerCreator, null);
                var handler = arg3.ToWeakEventHandler<EventArgs>(false);
                invoker.Changed += handler.Handle;
                handler.Unsubscriber = WeakActionToken.Create(invoker, handler, (eventInvoker, eventHandler) => eventInvoker.Changed -= eventHandler.Handle, false);
                return handler;
            }

            public object SetValue(IBindingMemberInfo arg1, TTarget arg2, object[] arg3)
            {
                if (!_setValue(arg1, arg2, arg3))
                    return null;
                var invoker = ServiceProvider
                    .AttachedValueProvider
                    .GetValue<EventInvoker>(arg2, GetMemberPath(arg1), false);
                if (invoker != null)
                    invoker.RaiseChanged(arg2);
                return null;
            }

            private static string GetMemberPath(IBindingMemberInfo info)
            {
                return ((IAttachedBindingMemberInternal)info).Id + ListenerMember;
            }

            #endregion
        }

        private class AttachedBindingMemberInfo<TTarget, TType> : IAttachedBindingMemberInternal, IAttachedBindingMemberInfo<TTarget, TType>
        {
            #region Fields

            private const string IsAttachedHandlerInvokedMember = ".IsAttachedHandlerInvoked";
            private readonly Func<IBindingMemberInfo, TTarget, object[], TType> _getValue;
            private readonly Action<TTarget, MemberAttachedEventArgs> _memberAttachedHandler;
            private readonly Action<TTarget, AttachedMemberChangedEventArgs<TType>> _memberChangedHandler;
            private readonly Func<TTarget, IBindingMemberInfo, TType> _defaultValue;
            private readonly Func<IBindingMemberInfo, TTarget, object[], object> _setValue;
            private readonly Func<IBindingMemberInfo, TTarget, IEventListener, IDisposable> _observeMemberDelegate;
            private readonly string _path;
            private readonly Type _type;
            private readonly BindingMemberType _memberType;
            private readonly MemberInfo _member;
            private readonly bool _canRead;
            private readonly bool _canWrite;
            private readonly string _id;

            #endregion

            #region Constructors

            /// <summary>
            ///     Initializes a new instance of the <see cref="AttachedBindingMemberInfo{TTarget,TType}" /> class.
            /// </summary>
            public AttachedBindingMemberInfo(string path, Type type,
                Action<TTarget, MemberAttachedEventArgs> memberAttachedHandler,
                Action<TTarget, AttachedMemberChangedEventArgs<TType>> memberChangedHandler, Func<TTarget, IBindingMemberInfo, TType> defaultValue, BindingMemberType memberType = null)
                : this(path, type, memberAttachedHandler, ObserveAttached, GetAttachedValue, SetAttachedValue, null, memberType)
            {
                _memberChangedHandler = memberChangedHandler;
                _defaultValue = defaultValue;
            }

            /// <summary>
            ///     Initializes a new instance of the <see cref="AttachedBindingMemberInfo{TTarget,TType}" /> class.
            /// </summary>
            public AttachedBindingMemberInfo(string path, Type type,
                Action<TTarget, MemberAttachedEventArgs> memberAttachedHandler,
                Func<IBindingMemberInfo, TTarget, IEventListener, IDisposable> observeMemberDelegate,
                Func<IBindingMemberInfo, TTarget, object[], TType> getValue,
                Func<IBindingMemberInfo, TTarget, object[], object> setValue, MemberInfo member, BindingMemberType memberType = null)
            {
                Should.NotBeNullOrWhitespace(path, "path");
                Should.NotBeNull(type, "type");
                _path = path;
                _memberAttachedHandler = memberAttachedHandler;
                _getValue = getValue ?? GetValueThrow<TType, TTarget>;
                _setValue = setValue ?? SetValueThrow;
                _observeMemberDelegate = observeMemberDelegate;
                _type = type ?? typeof(object);
                _member = member;
                _memberType = memberType ?? BindingMemberType.Attached;
                _canRead = getValue != null;
                _canWrite = setValue != null;
                _id = MemberPrefix + Interlocked.Increment(ref _counter) + "." + path;
                Tracer.Info("The attached property with path: {0}, type: {1}, target type: {2} was created.", path, type,
                    typeof(TTarget));
            }

            #endregion

            #region Implementation of IBindingMemberInfo

            /// <summary>
            ///     Gets the path of member.
            /// </summary>
            public string Path
            {
                get { return _path; }
            }

            /// <summary>
            ///     Gets the type of member.
            /// </summary>
            public Type Type
            {
                get { return _type; }
            }

            /// <summary>
            ///     Gets the underlying member, if any.
            /// </summary>
            public MemberInfo Member
            {
                get { return _member; }
            }

            /// <summary>
            ///     Gets the member type.
            /// </summary>
            public BindingMemberType MemberType
            {
                get { return _memberType; }
            }

            /// <summary>
            ///     Gets a value indicating whether the member can be read.
            /// </summary>
            public bool CanRead
            {
                get { return _canRead; }
            }

            /// <summary>
            ///     Gets a value indicating whether the property can be written to.
            /// </summary>
            public bool CanWrite
            {
                get { return _canWrite; }
            }

            /// <summary>
            ///     Returns the member value of a specified object.
            /// </summary>
            /// <param name="source">The object whose member value will be returned.</param>
            /// <param name="args">Optional values for members.</param>
            /// <returns>The member value of the specified object.</returns>
            object IBindingMemberInfo.GetValue(object source, object[] args)
            {
                return GetValue((TTarget)source, args);
            }

            /// <summary>
            ///     Sets the member value of a specified object.
            /// </summary>
            /// <param name="source">The object whose member value will be set.</param>
            /// <param name="args">Optional values for members..</param>
            object IBindingMemberInfo.SetValue(object source, object[] args)
            {
                return SetValue((TTarget)source, args);
            }

            /// <summary>
            ///     Attempts to track the value change.
            /// </summary>
            IDisposable IBindingMemberInfo.TryObserveMember(object source, IEventListener listener)
            {
                return TryObserveMember((TTarget)source, listener);
            }

            #endregion

            #region Methods

            private static IDisposable ObserveAttached(IBindingMemberInfo member, TTarget source, IEventListener listener)
            {
                var property = GetAttachedProperty((IAttachedBindingMemberInternal)member, source);
                var eventHandler = listener.ToWeakEventHandler<EventArgs>(false);
                property.ValueChanged += eventHandler.Handle;
                eventHandler.Unsubscriber = WeakActionToken.Create(property, eventHandler, (attachedProperty, handler) => attachedProperty.ValueChanged -= handler.Handle, false);
                return eventHandler;
            }

            private static object SetAttachedValue(IBindingMemberInfo member, TTarget source, object[] args)
            {
                GetAttachedProperty((IAttachedBindingMemberInternal)member, source).SetValue(source, args);
                return null;
            }

            private static TType GetAttachedValue(IBindingMemberInfo member, TTarget source, object[] args)
            {
                return (TType)GetAttachedProperty((IAttachedBindingMemberInternal)member, source).Value;
            }

            private void RaiseAttached(object source)
            {
                var id = ServiceProvider
                    .AttachedValueProvider
                    .GetValue<object>(source, Id + IsAttachedHandlerInvokedMember, false);
                if (id != null)
                    return;
                id = new object();
                var attachId = ServiceProvider
                    .AttachedValueProvider
                    .GetOrAdd(source, Id + IsAttachedHandlerInvokedMember, (o, o1) => o1, id);
                if (ReferenceEquals(id, attachId))
                    _memberAttachedHandler((TTarget)source, new MemberAttachedEventArgs(this));
            }

            #endregion

            #region Implementation of IAttachedBindingMember

            public string MemberChangeEventName { get; set; }

            bool IAttachedBindingMemberInternal.HasMemberChangedHandler
            {
                get { return _memberChangedHandler != null; }
            }

            public object GetDefaultValue(object source)
            {
                if (_defaultValue == null)
                    return Type.GetDefaultValue();
                return _defaultValue((TTarget)source, this);
            }

            void IAttachedBindingMemberInternal.MemberChanged(object sender, object oldValue, object newValue, object[] args)
            {
                _memberChangedHandler((TTarget)sender,
                    new AttachedMemberChangedEventArgs<TType>((TType)oldValue, (TType)newValue, args, this));
            }

            public string Id
            {
                get { return _id; }
            }

            #endregion

            #region Implementation of IAttachedBindingMemberInfo<in TTarget,out TType>

            /// <summary>
            ///     Returns the member value of a specified object.
            /// </summary>
            /// <param name="source">The object whose member value will be returned.</param>
            /// <param name="args">Optional values for members.</param>
            /// <returns>The member value of the specified object.</returns>
            public TType GetValue(TTarget source, object[] args)
            {
                if (_memberAttachedHandler != null)
                    RaiseAttached(source);
                return _getValue(this, source, args);
            }

            /// <summary>
            ///     Sets the member value of a specified object.
            /// </summary>
            /// <param name="source">The object whose member value will be set.</param>
            /// <param name="args">Optional values for members..</param>
            public object SetValue(TTarget source, object[] args)
            {
                if (_memberAttachedHandler != null)
                    RaiseAttached(source);
                return _setValue(this, source, args);
            }

            /// <summary>
            ///     Attempts to track the value change.
            /// </summary>
            public IDisposable TryObserveMember(TTarget source, IEventListener listener)
            {
                if (_observeMemberDelegate == null)
                    return null;
                return _observeMemberDelegate(this, source, listener);
            }

            #endregion
        }

        private sealed class AttachedProperty
        {
            #region Fields

            private ICollection<IAttachedBindingMemberInternal> _members;
            public object Value;

            #endregion

            #region Events

            public event EventHandler ValueChanged;

            #endregion

            #region Methods

            public void AddListener(IAttachedBindingMemberInternal listener)
            {
                if (_members == null)
                    _members = new HashSet<IAttachedBindingMemberInternal>();
                lock (_members)
                    _members.Add(listener);
            }

            public void SetValue(object source, object[] args)
            {
                var value = args[0];
                if (Equals(Value, value))
                    return;
                object oldValue = Value;
                Value = value;
                if (_members != null)
                {
                    IAttachedBindingMemberInternal[] members;
                    lock (_members)
                        members = _members.ToArrayFast();
                    for (int index = 0; index < members.Length; index++)
                        members[index].MemberChanged(source, oldValue, value, args);
                }
                EventHandler handler = ValueChanged;
                if (handler != null)
                    handler(this, EventArgs.Empty);
            }

            #endregion

            #region Overrides of Object

            public override string ToString()
            {
                return "Attached property value: " + Value;
            }

            #endregion
        }

        private interface IAttachedBindingMemberInternal : IBindingMemberInfo
        {
            string MemberChangeEventName { get; }

            bool HasMemberChangedHandler { get; }

            object GetDefaultValue(object source);

            void MemberChanged(object sender, object oldValue, object newValue, object[] args);

            string Id { get; }
        }

        #endregion

        #region Fields

        private static int _counter;
        private const string MemberPrefix = "#@$Attached";
        private static readonly Func<object, object, AttachedProperty> AttachedPropertyFactoryDelegate;

        #endregion

        #region Constructors

        static AttachedBindingMember()
        {
            AttachedPropertyFactoryDelegate = AttachedPropertyFactory;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Creates a new instance of the <see cref="IBindingMemberInfo" /> class.
        /// </summary>
        public static IAttachedBindingMemberInfo<TTarget, object> CreateEvent<TTarget>([NotNull] string path,
            Func<IBindingMemberInfo, TTarget, IEventListener, IDisposable> setValue, Action<TTarget, MemberAttachedEventArgs> memberAttachedHandler = null)
        {
            return new AttachedBindingMemberInfo<TTarget, object>(path, typeof(Delegate), memberAttachedHandler, null,
                GetBindingMemberValue, (info, o, arg3) => setValue(info, o, (IEventListener)arg3[0]), null, BindingMemberType.Event);
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="IBindingMemberInfo" /> class.
        /// </summary>
        public static IAttachedBindingMemberInfo<object, object> CreateEvent([NotNull] string path,
            Func<IBindingMemberInfo, object, IEventListener, IDisposable> setValue, Action<object, MemberAttachedEventArgs> memberAttachedHandler = null)
        {
            return new AttachedBindingMemberInfo<object, object>(path, typeof(Delegate), memberAttachedHandler, null,
                GetBindingMemberValue, (info, o, arg3) => setValue(info, o, (IEventListener)arg3[0]), null, BindingMemberType.Event);
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="IBindingMemberInfo" /> class.
        /// </summary>
        public static IAttachedBindingMemberInfo<TTarget, TType> CreateAutoProperty<TTarget, TType>([NotNull] string path,
            Action<TTarget, AttachedMemberChangedEventArgs<TType>> memberChangedHandler = null,
            Action<TTarget, MemberAttachedEventArgs> memberAttachedHandler = null, Func<TTarget, IBindingMemberInfo, TType> defaultValue = null)
        {
            return new AttachedBindingMemberInfo<TTarget, TType>(path, typeof(TType), memberAttachedHandler, memberChangedHandler, defaultValue);
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="IBindingMemberInfo" /> class.
        /// </summary>
        public static IAttachedBindingMemberInfo<object, object> CreateAutoProperty([NotNull] string path, [NotNull] Type type,
            Action<object, AttachedMemberChangedEventArgs<object>> memberChangedHandler = null,
            Action<object, MemberAttachedEventArgs> memberAttachedHandler = null, Func<object, IBindingMemberInfo, object> defaultValue = null)
        {
            return new AttachedBindingMemberInfo<object, object>(path, type, memberAttachedHandler, memberChangedHandler, defaultValue);
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="IBindingMemberInfo" /> class.
        /// </summary>
        public static IAttachedBindingMemberInfo<TTarget, TType> CreateMember<TTarget, TType>([NotNull] string path,
            [CanBeNull]Func<IBindingMemberInfo, TTarget, object[], TType> getValue, [CanBeNull] Func<IBindingMemberInfo, TTarget, object[], object> setValue,
            Action<TTarget, MemberAttachedEventArgs> memberAttachedHandler = null, string memberChangeEventName = null,
            MemberInfo member = null)
        {
            return new AttachedBindingMemberInfo<TTarget, TType>(path, typeof(TType), memberAttachedHandler,
                string.IsNullOrEmpty(memberChangeEventName)
                    ? (Func<IBindingMemberInfo, TTarget, IEventListener, IDisposable>)null
                    : ObserveMemberChangeEvent, getValue, setValue, member)
            {
                MemberChangeEventName = memberChangeEventName
            };
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="IBindingMemberInfo" /> class.
        /// </summary>
        public static IAttachedBindingMemberInfo<object, object> CreateMember([NotNull] string path, [NotNull] Type type,
            [CanBeNull]Func<IBindingMemberInfo, object, object[], object> getValue, [CanBeNull]Func<IBindingMemberInfo, object, object[], object> setValue,
            Action<object, MemberAttachedEventArgs> memberAttachedHandler = null, string memberChangeEventName = null,
            MemberInfo member = null)
        {
            return new AttachedBindingMemberInfo<object, object>(path, type, memberAttachedHandler,
                string.IsNullOrEmpty(memberChangeEventName)
                    ? (Func<IBindingMemberInfo, object, IEventListener, IDisposable>)null
                    : ObserveMemberChangeEvent, getValue, setValue, member)
            {
                MemberChangeEventName = memberChangeEventName
            };
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="IBindingMemberInfo" /> class.
        /// </summary>
        public static IAttachedBindingMemberInfo<TTarget, TType> CreateMember<TTarget, TType>([NotNull] string path,
            [CanBeNull]Func<IBindingMemberInfo, TTarget, object[], TType> getValue,
            [CanBeNull]Func<IBindingMemberInfo, TTarget, object[], object> setValue,
            [CanBeNull]Func<IBindingMemberInfo, TTarget, IEventListener, IDisposable> observeMemberDelegate,
            Action<TTarget, MemberAttachedEventArgs> memberAttachedHandler = null, MemberInfo member = null)
        {
            return new AttachedBindingMemberInfo<TTarget, TType>(path, typeof(TType), memberAttachedHandler,
                observeMemberDelegate, getValue, setValue, member);
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="IBindingMemberInfo" /> class.
        /// </summary>
        public static IAttachedBindingMemberInfo<object, object> CreateMember([NotNull] string path, [NotNull] Type type,
            [CanBeNull]Func<IBindingMemberInfo, object, object[], object> getValue,
            [CanBeNull]Func<IBindingMemberInfo, object, object[], object> setValue,
            [CanBeNull]Func<IBindingMemberInfo, object, IEventListener, IDisposable> observeMemberDelegate,
            Action<object, MemberAttachedEventArgs> memberAttachedHandler = null, MemberInfo member = null)
        {
            return new AttachedBindingMemberInfo<object, object>(path, type, memberAttachedHandler,
                observeMemberDelegate, getValue, setValue, member);
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="IBindingMemberInfo" /> class.
        /// </summary>
        public static IAttachedBindingMemberInfo<TTarget, TType> CreateNotifiableMember<TTarget, TType>([NotNull] string path,
            [CanBeNull]Func<IBindingMemberInfo, TTarget, object[], TType> getValue,
            [NotNull]Func<IBindingMemberInfo, TTarget, object[], bool> setValue,
            Action<TTarget, MemberAttachedEventArgs> memberAttachedHandler = null, MemberInfo member = null)
        {
            var observableProperty = new ObservableProperty<TTarget>(setValue);
            return new AttachedBindingMemberInfo<TTarget, TType>(path, typeof(TType), memberAttachedHandler,
                observableProperty.ObserveMember, getValue, observableProperty.SetValue, member);
        }

        /// <summary>
        ///     Creates a new instance of the <see cref="IBindingMemberInfo" /> class.
        /// </summary>
        public static IAttachedBindingMemberInfo<object, object> CreateNotifiableMember([NotNull] string path, [NotNull] Type type,
            [CanBeNull]Func<IBindingMemberInfo, object, object[], object> getValue, [NotNull]Func<IBindingMemberInfo, object, object[], bool> setValue,
            Action<object, MemberAttachedEventArgs> memberAttachedHandler = null, MemberInfo member = null)
        {
            var observableProperty = new ObservableProperty<object>(setValue);
            return new AttachedBindingMemberInfo<object, object>(path, type, memberAttachedHandler,
                observableProperty.ObserveMember, getValue, observableProperty.SetValue, member);
        }


        private static TType GetValueThrow<TType, TTarget>(IBindingMemberInfo member, TTarget source, object[] args)
        {
            throw BindingExceptionManager.BindingMemberMustBeReadable(member);
        }

        private static object SetValueThrow<TTarget>(IBindingMemberInfo member, TTarget source, object[] args)
        {
            throw BindingExceptionManager.BindingMemberMustBeWriteable(member);
        }

        private static object GetBindingMemberValue<TTarget>(IBindingMemberInfo bindingMemberInfo, TTarget target1, object[] arg3)
        {
            return new BindingMemberValue(target1, bindingMemberInfo);
        }

        private static AttachedProperty GetAttachedProperty(IAttachedBindingMemberInternal member, object source)
        {
            return ServiceProvider
                .AttachedValueProvider
                .GetOrAdd(source, member.Id, AttachedPropertyFactoryDelegate, member);
        }

        private static AttachedProperty AttachedPropertyFactory(object source, object state)
        {
            var member = (IAttachedBindingMemberInternal)state;
            var property = new AttachedProperty { Value = member.GetDefaultValue(source) };
            if (member.HasMemberChangedHandler)
                property.AddListener(member);
            return property;
        }

        private static IDisposable ObserveMemberChangeEvent<TTarget>(IBindingMemberInfo member, TTarget source,
            IEventListener arg3)
        {
            string eventName = ((IAttachedBindingMemberInternal)member).MemberChangeEventName;
            return BindingProvider.Instance.WeakEventManager.TrySubscribe(source, eventName, arg3);
        }

        #endregion
    }
}