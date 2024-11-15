using System;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;
using Prism.Ioc;
using Prism.Modularity;
using SnowyRiver.VisionMaster.Modules.ModuleName;
using SnowyRiver.VisionMaster.Services;
using SnowyRiver.VisionMaster.Services.Interfaces;
using SnowyRiver.VisionMaster.Views;
using SnowyRiver.WPF.MaterialDesignInPrism.Service;
using SnowyRiver.WPF.MaterialDesignInPrism.Windows;

namespace SnowyRiver.VisionMaster
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("Configs/NLog.config");
        }

        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(config =>
            {
                config.ClearProviders();
                config.AddNLog();
            });

            services.AddAutoMapper(config
                => config.AddMaps(AppDomain.CurrentDomain.GetAssemblies()));
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.RegisterDialogWindow<MaterialDesignMetroDialogWindow>();
            containerRegistry.RegisterSingleton<IDialogHostService, DialogHostService>();

            containerRegistry.RegisterSingleton<IMessageService, MessageService>();
        }

        protected override void ConfigureModuleCatalog(IModuleCatalog moduleCatalog)
        {
            moduleCatalog.AddModule<ModuleNameModule>();
        }
    }
}
