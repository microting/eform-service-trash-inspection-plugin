using System;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Rebus.Config;

namespace ServiceTrashInspectionPlugin.Installers
{
    public class RebusInstaller : IWindsorInstaller
    {
        private readonly string connectionString;
        private readonly int maxParallelism;
        private readonly int numberOfWorkers;

        public RebusInstaller(string connectionString, int maxParallelism, int numberOfWorkers)
        {
            if (string.IsNullOrEmpty(connectionString)) throw new ArgumentNullException(nameof(connectionString));
            this.connectionString = connectionString;
            this.maxParallelism = maxParallelism;
            this.numberOfWorkers = numberOfWorkers;
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            if (connectionString.ToLower().Contains("convert zero datetime"))
            {
                Configure.With(new CastleWindsorContainerAdapter(container))
                    .Logging(l => l.ColoredConsole())
                    .Transport(t => t.UseMySql(connectionStringOrConnectionOrConnectionStringName: connectionString, tableName: "Rebus", inputQueueName: "trash-inspection-input"))
                    .Options(o =>
                    {
                        o.SetMaxParallelism(maxParallelism);
                        o.SetNumberOfWorkers(numberOfWorkers);
                    })
                    .Start();
            }
            else
            {
                Configure.With(new CastleWindsorContainerAdapter(container))
                    .Logging(l => l.ColoredConsole())
                    .Transport(t => t.UseSqlServer(connectionString: connectionString, inputQueueName: "trash-inspection-input"))
                    //.Transport(t => t.UseSqlServer(connectionStringOrConnectionStringName: connectionString, tableName: "Rebus", inputQueueName: "eformsdk-input"))
                    .Options(o =>
                    {
                        o.SetMaxParallelism(maxParallelism);
                        o.SetNumberOfWorkers(numberOfWorkers);
                    })
                    .Start();
            }
            
        }
    }
}