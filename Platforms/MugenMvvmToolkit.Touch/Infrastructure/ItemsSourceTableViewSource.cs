#region Copyright
// ****************************************************************************
// <copyright file="ItemsSourceTableViewSource.cs">
// Copyright � Vyacheslav Volkov 2012-2014
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
using System.Collections;
using System.Collections.Specialized;
using JetBrains.Annotations;
using MonoTouch.Foundation;
using MonoTouch.UIKit;
using MugenMvvmToolkit.Binding;

namespace MugenMvvmToolkit.Infrastructure
{
    public class ItemsSourceTableViewSource : TableViewSourceBase
    {
        #region Fields

        private readonly NotifyCollectionChangedEventHandler _weakHandler;
        private IEnumerable _itemsSource;

        #endregion

        #region Constructors

        public ItemsSourceTableViewSource([NotNull] UITableView tableView,
            string itemTemplate = AttachedMemberConstants.ItemTemplate)
            : base(tableView, itemTemplate)
        {
            _weakHandler = ReflectionExtensions.MakeWeakCollectionChangedHandler(this, (adapter, o, arg3) => adapter.OnCollectionChanged(o, arg3));
        }

        #endregion

        #region Properties

        public virtual IEnumerable ItemsSource
        {
            get { return _itemsSource; }
            set { SetItemsSource(value); }
        }

        #endregion

        #region Overrides of TableViewSourceBase

        public override int RowsInSection(UITableView tableview, int section)
        {
            if (ItemsSource == null)
                return 0;
            return ItemsSource.Count();
        }

        protected override object GetItemAt(NSIndexPath indexPath)
        {
            if (indexPath == null || ItemsSource == null)
                return null;
            return ItemsSource.ElementAtIndex(indexPath.Row);
        }

        protected override void SetSelectedCellByItem(object selectedItem)
        {
            if (selectedItem == null)
                ClearSelection();
            else
            {
                int i = ItemsSource.IndexOf(selectedItem);
                ClearSelection();
                if (i >= 0)
                {
                    var indexPath = NSIndexPath.FromRowSection(i, 0);
                    TableView.SelectRow(indexPath, UseAnimations, ScrollPosition);
                    RowSelected(TableView, indexPath);
                }
            }
        }

        #endregion

        #region Methods

        protected virtual void SetItemsSource(IEnumerable value)
        {
            if (ReferenceEquals(value, _itemsSource))
                return;
            if (_weakHandler == null)
                _itemsSource = value;
            else
            {
                var oldValue = _itemsSource;
                var notifyCollectionChanged = oldValue as INotifyCollectionChanged;
                if (notifyCollectionChanged != null)
                    notifyCollectionChanged.CollectionChanged -= _weakHandler;
                _itemsSource = value;
                notifyCollectionChanged = value as INotifyCollectionChanged;
                if (notifyCollectionChanged != null)
                    notifyCollectionChanged.CollectionChanged += _weakHandler;
            }
            ReloadData();
        }

        protected virtual void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs args)
        {
            if (!UseAnimations || !TryUpdateItems(args))
                ReloadData();
        }

        protected bool TryUpdateItems(NotifyCollectionChangedEventArgs args)
        {
            switch (args.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    NSIndexPath[] newIndexPaths = PlatformExtensions.CreateNSIndexPathArray(args.NewStartingIndex, args.NewItems.Count);
                    TableView.InsertRows(newIndexPaths, AddAnimation);
                    return true;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var oldItem in args.OldItems)
                        ItemDeselected(oldItem);
                    NSIndexPath[] oldIndexPaths = PlatformExtensions.CreateNSIndexPathArray(args.OldStartingIndex, args.OldItems.Count);
                    TableView.DeleteRows(oldIndexPaths, RemoveAnimation);
                    return true;
                case NotifyCollectionChangedAction.Move:
                    if (args.NewItems.Count != 1 && args.OldItems.Count != 1)
                        return false;

                    NSIndexPath oldIndexPath = NSIndexPath.FromRowSection(args.OldStartingIndex, 0);
                    NSIndexPath newIndexPath = NSIndexPath.FromRowSection(args.NewStartingIndex, 0);
                    TableView.MoveRow(oldIndexPath, newIndexPath);
                    return true;
                case NotifyCollectionChangedAction.Replace:
                    if (args.NewItems.Count != args.OldItems.Count)
                        return false;
                    NSIndexPath indexPath = NSIndexPath.FromRowSection(args.NewStartingIndex, 0);
                    TableView.ReloadRows(new[] { indexPath }, ReplaceAnimation);
                    return true;
                default:
                    return false;
            }
        }

        #endregion
    }
}