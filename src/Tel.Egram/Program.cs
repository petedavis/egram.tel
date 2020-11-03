using System;
using Avalonia;
using Avalonia.Platform;
using Avalonia.ReactiveUI;
using Splat;
using Tel.Egram.Application;
using Tel.Egram.Views.Application;
using Tel.Egram.Model.Application;

namespace Tel.Egram
{
    public class Program
    {
        public static void Main(string[] args)
        {
            ConfigureServices(Locator.CurrentMutable);
            BuildAvaloniaApp()
                .StartWithClassicDesktopLifetime(args);
        }

        private static void ConfigureServices(
            IMutableDependencyResolver services)
        {
            services.AddUtils();
            services.AddTdLib();
            services.AddPersistance();
            services.AddServices();
            
            services.AddComponents();
            services.AddApplication();
            services.AddAuthentication();
            services.AddWorkspace();
            services.AddSettings();
            services.AddMessenger();
        }

        private static AppBuilder BuildAvaloniaApp()
        {
            var builder = AppBuilder.Configure<MainApplication>();
            var runtime = builder.RuntimePlatform.GetRuntimeInfo();
            
            switch (runtime.OperatingSystem)
            {
                case OperatingSystemType.OSX:
                    builder.UseAvaloniaNative()
                        .With(new AvaloniaNativePlatformOptions
                        {
                            UseGpu = true,
                            UseDeferredRendering = true
                        })
                        .UseSkia();
                    break;
                
                case OperatingSystemType.Linux:
                    builder.UseX11()
                        .With(new X11PlatformOptions
                        {
                            UseGpu = true
                        })
                        .UseSkia();
                    break;
                
                default:
                    builder.UseWin32()
                        .With(new Win32PlatformOptions
                        {
                            UseDeferredRendering = true
                        })
                        .UseSkia();
                    break;
            }

            builder.UseReactiveUI();

            return builder;
        }
    }
}