using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Windows.Threading;

using Scrutiny.Extensions;

namespace Scrutiny.Utilities
{
    public class ThreadSafeObservableCollection<T> : ObservableCollection<T>
    {
        private readonly SynchronizationContext _synchronizationContext;

        public ThreadSafeObservableCollection(IEnumerable<T> list = null, int? capacity = null, SynchronizationContext synchronizationContext = null)
        {
            // XXX: Use Dispatcher instead?
            if (synchronizationContext == null)
            {
               _synchronizationContext = SynchronizationContext.Current;

               // Synchronization context will be null if we're not in the UI Thread
               if (_synchronizationContext == null)
               {
                   throw new InvalidOperationException(
                       "This collection must be instantiated from UI Thread, if not, you have to pass SynchronizationContext to constructor.");
               }
            }
            else
            {
                _synchronizationContext = synchronizationContext;
            }

            var itemsList = Items as List<T>;

            if (itemsList == null)
            {
                return;
            }

            if (capacity.HasValue)
            {
                itemsList.Capacity = capacity.Value;
            }

            if (list != null)
            {
                itemsList.AddRange(list);

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
            }
        }

        public override event NotifyCollectionChangedEventHandler CollectionChanged;

        protected override sealed void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            using (BlockReentrancy())
            {
                var eventHandler = CollectionChanged;

                if (eventHandler == null)
                {
                    return;
                }

                var dispatcher = (from NotifyCollectionChangedEventHandler
                                      handler in eventHandler.GetInvocationList()
                                  let dispatcherObject = handler.Target as DispatcherObject
                                  where dispatcherObject != null
                                  select dispatcherObject.Dispatcher).FirstOrDefault();

                if (dispatcher != null && dispatcher.CheckAccess() == false)
                {
                    dispatcher.Invoke(DispatcherPriority.DataBind, (Action)(() => OnCollectionChanged(e)));
                }
                else
                {
                    foreach (NotifyCollectionChangedEventHandler handler
                        in eventHandler.GetInvocationList())
                    {
                        handler.Invoke(this, e);
                    }
                }
            }
        }

        public void SerializeToList(string path)
        {
            using (var stream = new FileStream(path, FileMode.OpenOrCreate))
            {
                var items = (Items as List<T>);

                if (items == null)
                {
                    throw new Exception("items was null");
                }

                new BinaryFormatter().Serialize(stream, (Items as List<T>));
            }
        }

        public static ThreadSafeObservableCollection<T> DeserializeFromList(string path, SynchronizationContext synchronizationContext = null)
        {
            using (var stream = new FileStream(path, FileMode.Open))
            {
                var list = (List<T>)new BinaryFormatter().Deserialize(stream);

                return new ThreadSafeObservableCollection<T>(list, synchronizationContext: synchronizationContext);
            }
        }

        public void Sort<TValue>(Func<T, TValue> selector, IComparer<TValue> comparer = null)
        {
            var list = Items as List<T>;

            if (list == null)
            {
                return;
            }

            list.Sort(selector, comparer);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public void SortDescending<TValue>(Func<T, TValue> selector, IComparer<TValue> comparer = null)
        {
            var list = Items as List<T>;

            if (list == null)
            {
                return;
            }

            list.SortDescending(selector, comparer);

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        protected override void InsertItem(int index, T item)
        {
            // XXX: Use dispatcher instead?
            _synchronizationContext.Send(InsertItem, new InsertItemParameter(index, item));
        }

        protected override void MoveItem(int oldIndex, int newIndex)
        {
            // XXX: Use dispatcher instead?
            _synchronizationContext.Send(MoveItem, new MoveItemParameter(oldIndex, newIndex));
        }

        void InsertItem(object parameter)
        {
            var insertItemParameter = parameter as InsertItemParameter;

            if (insertItemParameter != null)
            {
                base.InsertItem(insertItemParameter.Index, insertItemParameter.Item);
            }
        }

        void MoveItem(object parameter)
        {
            var moveItemParameter = parameter as MoveItemParameter;

            if (moveItemParameter != null)
            {
                base.MoveItem(moveItemParameter.OldIndex, moveItemParameter.NewIndex);
            }
        }

        class InsertItemParameter
        {
            public readonly int Index;
            public readonly T Item;

            public InsertItemParameter(int index, T item)
            {
                Index = index;
                Item = item;
            }
        }

        class MoveItemParameter
        {
            public readonly int OldIndex;
            public readonly int NewIndex;

            public MoveItemParameter(int oldIndex, int newIndex)
            {
                OldIndex = oldIndex;
                NewIndex = newIndex;
            }
        }
    }
}