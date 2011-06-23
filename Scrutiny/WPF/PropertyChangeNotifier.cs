using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Data;

namespace Scrutiny.Utilities
{
    public sealed class PropertyChangeNotifier : DependencyObject, IDisposable
    {
        private readonly WeakReference _propertySource;

        public PropertyChangeNotifier(DependencyObject propertySource, string path)
            : this(propertySource, new PropertyPath(path))
        {
        }

        public PropertyChangeNotifier(DependencyObject propertySource, DependencyProperty property)
            : this(propertySource, new PropertyPath(property))
        {
        }

        public PropertyChangeNotifier(DependencyObject propertySource, PropertyPath property)
        {
            if (null == propertySource)
            {
                throw new ArgumentNullException("propertySource");
            }

            if (null == property)
            {
                throw new ArgumentNullException("property");
            }
            
            _propertySource = new WeakReference(propertySource);

            var binding = new Binding
            {
                Path = property, 
                Mode = BindingMode.OneWay, 
                Source = propertySource
            };

            BindingOperations.SetBinding(this, ValueProperty, binding);
        }

        public DependencyObject PropertySource
        {
            get
            {
                try
                {
                    return _propertySource.IsAlive ? _propertySource.Target as DependencyObject : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Identifies the <see cref="Value" /> dependency property
        /// </summary>
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register("Value",
            typeof (object),
            typeof (PropertyChangeNotifier),
            new FrameworkPropertyMetadata(null, OnPropertyChanged));

        /// <summary>
        /// Returns/sets the value of the property
        /// </summary>
        /// <seealso cref="ValueProperty" />
        [Description("Returns/sets the value of the property")]
        [Category("Behavior")]
        [Bindable(true)]
        public object Value
        {
            get
            {
                return GetValue(ValueProperty);
            }

            set
            {
                SetValue(ValueProperty, value);
            }
        }

        private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var notifier = (PropertyChangeNotifier)d;
            
            if (null != notifier.ValueChanged)
            {
                notifier.ValueChanged(notifier, EventArgs.Empty);
            }
        }

        public event EventHandler ValueChanged;

        public void Dispose()
        {
            BindingOperations.ClearBinding(this, ValueProperty);
        }
    }
}