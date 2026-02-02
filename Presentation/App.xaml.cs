using ApplicationLayer.Interfaces;
using ApplicationLayer.Services;
using Domain.Entities;
using Domain.Interfaces;
using Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Presentation.Interfaces;
using Presentation.Services;
using Presentation.ViewModels;
using Presentation.Views;
using System.IO;
using System.Windows;

namespace Presentation
{
    public partial class App : Application
    {
        private IHost _host;

        public App()
        {
            _host = Host.CreateDefaultBuilder()
                .ConfigureServices(services =>
                {
                    services.AddScoped<IProductService, ProductService>();

                    string dataDirectory = Path.Combine(AppContext.BaseDirectory, "Data");
                    services.AddScoped<IRepository<Product>>(serviceProvider => new JsonRepository<Product>(dataDirectory, "products.json"));
                    services.AddScoped<IRepository<Category>>(serviceProvider => new JsonRepository<Category>(dataDirectory, "categories.json"));
                    services.AddScoped<IRepository<Manufacturer>>(serviceProvider => new JsonRepository<Manufacturer>(dataDirectory, "manufacturers.json"));

                    //Presentation
                    services.AddSingleton<IViewNavigationService, ViewNavigationService>();
                    services.AddSingleton<MainWindow>();
                    services.AddSingleton<MainViewModel>();

                    services.AddScoped<ProductListViewModel>();
                    services.AddScoped<ProductListView>();

                    services.AddTransient<ProductAddViewModel>();
                    services.AddTransient<ProductAddView>();

                    services.AddScoped<ProductEditViewModel>();
                    services.AddScoped<ProductEditView>();


                })
                .Build();

        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            MainViewModel mainViewModel = _host!.Services.GetRequiredService<MainViewModel>();
            mainViewModel.CurrentViewModel = _host!.Services.GetRequiredService<ProductListViewModel>(); 
            
            MainWindow mainWindow = _host!.Services.GetRequiredService<MainWindow>();
            mainWindow.DataContext = mainViewModel;

            mainWindow.Show();

        }

        
    }
}




