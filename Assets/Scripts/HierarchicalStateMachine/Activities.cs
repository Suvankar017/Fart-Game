using System;
using System.Threading;
using System.Threading.Tasks;

namespace HierarchicalStateMachine
{
    public enum ActivityMode
    {
        Inactive,
        Active,
        Activating,
        Deactivating
    }

    public interface IActivity
    {
        public ActivityMode Mode { get; }

        public Task ActivateAsync(CancellationToken cancellationToken);
        public Task DeactivateAsync(CancellationToken cancellationToken);
    }
}