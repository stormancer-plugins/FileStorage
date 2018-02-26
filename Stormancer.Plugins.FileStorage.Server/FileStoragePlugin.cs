using Stormancer.Plugins;
using Stormancer;

namespace Stormancer.Server.FileStorage
{
    class FileStoragePlugin : IHostPlugin
    {
        internal const string METADATA_KEY = "stormancer.FileStoragePlugin";

        public void Build(HostPluginBuildContext ctx)
        {
            ctx.HostDependenciesRegistration += (IDependencyBuilder builder) =>
              {
                
                  builder.Register<AzureBlobFileStorage>().As<IFileStorage>();
              };

           
        }
    }
}
