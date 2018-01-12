using Attendances.ZKTecoBackendService.Connectors;
using Attendances.ZKTecoBackendService.Events;
using Attendances.ZKTecoBackendService.Handlers;
using Attendances.ZKTecoBackendService.Interfaces;
using Ninject.Modules;

namespace Attendances.ZKTecoBackendService
{
    internal class BackendServiceModule : NinjectModule
    {
        public override void Load()
        {            
            Bind<SqliteConnector>().ToSelf().InSingletonScope();
            Bind<WebApiConnector>().ToSelf().InSingletonScope();

            Bind<EventHub>().ToSelf().InSingletonScope();

            Bind<Bundle>().ToSelf();

            Bind<BackendRootService>().ToSelf();
        }
    }
}
