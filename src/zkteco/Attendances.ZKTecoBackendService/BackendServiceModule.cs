using Attendances.ZKTecoBackendService.Connectors;
using Attendances.ZKTecoBackendService.Events;
using Attendances.ZKTecoBackendService.Interfaces;
using Ninject.Modules;

namespace Attendances.ZKTecoBackendService
{
    internal class BackendServiceModule : NinjectModule
    {
        public override void Load()
        {            
            Bind<SqliteConnector>().ToSelf().InSingletonScope();
            Bind<IWebApiConnector>().To<WebApiConnector>().InSingletonScope();

            Bind<EventHub>().ToSelf().InSingletonScope();

            Bind<Bundle>().ToSelf();

            Bind<BackendRootService>().ToSelf();
        }
    }
}
