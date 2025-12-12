using Microsoft.Extensions.DependencyInjection;

namespace NavigationTracker.Helpers
{
    public static class ServiceHelper
    {
        private static IServiceProvider? _services;

        public static void Initialize(IServiceProvider serviceProvider) =>
            _services = serviceProvider;

        public static T GetRequiredService<T>() where T : notnull
        {
            if (_services is null)
            {
                throw new InvalidOperationException("Service provider is not initialized.");
            }

            return _services.GetRequiredService<T>();
        }
    }
}
