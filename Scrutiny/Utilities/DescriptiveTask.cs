using System;
using System.Threading;
using System.Threading.Tasks;

namespace Scrutiny.Utilities
{
    class DescriptiveTask : Task
    {
        public string Description
        {
            get;
            set;
        }

        public bool IsUiTask
        {
            get;
            set;
        }

        public DescriptiveTask(Action action, string description, bool isUiTask = false): base(action)
        {
            Description = description;
            IsUiTask = isUiTask;
        }

        public DescriptiveTask(Action action) : base(action)
        {
        }

        public DescriptiveTask(Action action, CancellationToken cancellationToken) : base(action, cancellationToken)
        {
        }

        public DescriptiveTask(Action action, TaskCreationOptions creationOptions) : base(action, creationOptions)
        {
        }

        public DescriptiveTask(Action action, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : base(action, cancellationToken, creationOptions)
        {
        }

        public DescriptiveTask(Action<object> action, object state) : base(action, state)
        {
        }

        public DescriptiveTask(Action<object> action, object state, CancellationToken cancellationToken) : base(action, state, cancellationToken)
        {
        }

        public DescriptiveTask(Action<object> action, object state, TaskCreationOptions creationOptions) : base(action, state, creationOptions)
        {
        }

        public DescriptiveTask(Action<object> action, object state, CancellationToken cancellationToken, TaskCreationOptions creationOptions) : base(action, state, cancellationToken, creationOptions)
        {
        }
    }
}
