﻿using System;
using System.Threading;
using System.Windows;

namespace Scrutiny.Utilities
{
    public class DeferredAction
    {
        private readonly Timer _timer;

        public DeferredAction(Action action)
        {
            _timer = new Timer(delegate
            {
                Application.Current.Dispatcher.Invoke(action);
            });
        }

        /// <summary>
        /// Defers performing the action until after time elapses. 
        /// Repeated calls will reschedule the action
        /// if it has not already been performed.
        /// </summary>
        /// <param name="delay">
        /// The amount of time to wait before performing the action.
        /// </param>
        public void Defer(int delay)
        {
            // Fire action when time elapses (with no subsequent calls).
            _timer.Change(delay, -1);
        }
    }
}
