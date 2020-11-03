using System.IO;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Splat;
using TdLib;
using Tel.Egram.Application;
using Tel.Egram.Model.Messenger.Catalog;
using Tel.Egram.Model.Messenger.Explorer;
using Tel.Egram.Model.Messenger.Explorer.Factories;
using Tel.Egram.Model.Notifications;
using Tel.Egram.Model.Popups;
using Tel.Egram.Services.Authentication;
using Tel.Egram.Services.Graphics;
using Tel.Egram.Services.Graphics.Avatars;
using Tel.Egram.Services.Graphics.Previews;
using Tel.Egram.Services.Messaging.Chats;
using Tel.Egram.Services.Messaging.Messages;
using Tel.Egram.Services.Messaging.Notifications;
using Tel.Egram.Services.Messaging.Users;
using Tel.Egram.Services.Persistance;
using Tel.Egram.Services.Settings;
using Tel.Egram.Services.Utils.Formatting;
using Tel.Egram.Services.Utils.Platforms;
using Tel.Egram.Services.Utils.TdLib;
using IBitmapLoader = Tel.Egram.Services.Graphics.IBitmapLoader;
using BitmapLoader = Tel.Egram.Services.Graphics.BitmapLoader;

namespace Tel.Egram
{
    public static class Registry
    {
        public static void AddUtils(this IMutableDependencyResolver services)
        {
            services.RegisterLazySingleton<IPlatform>(Platform.GetPlatform);
            services.RegisterLazySingleton<IStringFormatter>(() => new StringFormatter());
        }
        
        public static void AddTdLib(this IMutableDependencyResolver services)
        {
            services.RegisterLazySingleton(() =>
            {
                var storage = Locator.Current.GetService<IStorage>();
                
                Client.Log.SetFilePath(Path.Combine(storage.LogDirectory, "tdlib.log"));
                Client.Log.SetMaxFileSize(1_000_000); // 1MB
                Client.Log.SetVerbosityLevel(5);
                return new Client();
            });

            services.RegisterLazySingleton(() =>
            {
                var client = Locator.Current.GetService<Client>();
                return new Hub(client);
            });

            services.RegisterLazySingleton(() =>
            {
                var client = Locator.Current.GetService<Client>();
                var hub = Locator.Current.GetService<Hub>();
                return new Dialer(client, hub);
            });

            services.RegisterLazySingleton<IAgent>(() =>
            {
                var hub = Locator.Current.GetService<Hub>();
                var dialer = Locator.Current.GetService<Dialer>();
                return new Agent(hub, dialer);
            });
        }
        
        public static void AddPersistance(this IMutableDependencyResolver services)
        {
            services.RegisterLazySingleton<IResourceManager>(
                () => new ResourceManager(typeof(MainApplication).Assembly));
            
            services.RegisterLazySingleton<IStorage>(() => new Storage());
            
            services.RegisterLazySingleton<IFileLoader>(() =>
            {
                var agent = Locator.Current.GetService<IAgent>();
                return new FileLoader(agent);
            });
            
            services.RegisterLazySingleton<IFileExplorer>(() =>
            {
                var platform = Locator.Current.GetService<IPlatform>();
                return new FileExplorer(platform);
            });
            
            services.RegisterLazySingleton<IDatabaseContextFactory>(() => new DatabaseContextFactory());
            
            services.RegisterLazySingleton(() =>
            {
                var factory = Locator.Current.GetService<IDatabaseContextFactory>();
                return factory.CreateDbContext();
            });
            
            services.RegisterLazySingleton<IKeyValueStorage>(() =>
            {
                var db = Locator.Current.GetService<DatabaseContext>();
                return new KeyValueStorage(db);
            });
        }

        public static void AddServices(this IMutableDependencyResolver services)
        {
            // graphics
            services.RegisterLazySingleton<IColorMapper>(() => new ColorMapper());
            
            services.RegisterLazySingleton<IBitmapLoader>(() =>
            {
                var fileLoader = Locator.Current.GetService<IFileLoader>();
                return new BitmapLoader(fileLoader);
            });
            
            // avatars
            services.RegisterLazySingleton<IAvatarCache>(() =>
            {
                var options = Options.Create(new MemoryCacheOptions
                {
                    SizeLimit = 128 // maximum 128 cached bitmaps
                });
                return new AvatarCache(new MemoryCache(options));
            });
            
            services.RegisterLazySingleton<IAvatarLoader>(() =>
            {
                var platform = Locator.Current.GetService<IPlatform>();
                var storage = Locator.Current.GetService<IStorage>();
                var fileLoader = Locator.Current.GetService<IFileLoader>();
                var avatarCache = Locator.Current.GetService<IAvatarCache>();
                var colorMapper = Locator.Current.GetService<IColorMapper>();
                
                return new AvatarLoader(
                    platform,
                    storage,
                    fileLoader,
                    avatarCache,
                    colorMapper);
            });
            
            // previews
            services.RegisterLazySingleton<IPreviewCache>(() =>
            {
                var options = Options.Create(new MemoryCacheOptions
                {
                    SizeLimit = 16 // maximum 16 cached bitmaps
                });
                return new PreviewCache(new MemoryCache(options));
            });
            
            services.RegisterLazySingleton<IPreviewLoader>(() =>
            {
                var fileLoader = Locator.Current.GetService<IFileLoader>();
                var previewCache = Locator.Current.GetService<IPreviewCache>();
                
                return new PreviewLoader(
                    fileLoader,
                    previewCache);
            });
            
            // chats
            services.RegisterLazySingleton<IChatLoader>(() =>
            {
                var agent = Locator.Current.GetService<IAgent>();
                return new ChatLoader(agent);
            });
            
            services.RegisterLazySingleton<IChatUpdater>(() =>
            {
                var agent = Locator.Current.GetService<IAgent>();
                return new ChatUpdater(agent);
            });
            
            services.RegisterLazySingleton<IFeedLoader>(() =>
            {
                var agent = Locator.Current.GetService<IAgent>();
                return new FeedLoader(agent);
            });
            
            // messages
            services.RegisterLazySingleton<IMessageLoader>(() =>
            {
                var agent = Locator.Current.GetService<IAgent>();
                return new MessageLoader(agent);
            });
            services.RegisterLazySingleton<IMessageSender>(() =>
            {
                var agent = Locator.Current.GetService<IAgent>();
                return new MessageSender(agent);
            });
            
            // notifications
            services.RegisterLazySingleton<INotificationSource>(() =>
            {
                var agent = Locator.Current.GetService<IAgent>();
                return new NotificationSource(agent);
            });
            
            // users
            services.RegisterLazySingleton<IUserLoader>(() =>
            {
                var agent = Locator.Current.GetService<IAgent>();
                return new UserLoader(agent);
            });
            
            // auth
            services.RegisterLazySingleton<IAuthenticator>(() =>
            {
                var agent = Locator.Current.GetService<IAgent>();
                var storage = Locator.Current.GetService<IStorage>();
                return new Authenticator(agent, storage);
            });
            
            // settings
            services.RegisterLazySingleton<IProxyManager>(() =>
            {
                var agent = Locator.Current.GetService<IAgent>();
                return new ProxyManager(agent);
            });
        }

        public static void AddComponents(this IMutableDependencyResolver services)
        {
            services.RegisterLazySingleton<INotificationController>(() => new NotificationController());
            services.RegisterLazySingleton<IPopupController>(() => new PopupController());
        }
        
        public static void AddApplication(this IMutableDependencyResolver services)
        {
            services.RegisterLazySingleton(() =>
            {
                var application = new MainApplication();
                
                application.Initializing += (sender, args) =>
                {
                    var db = Locator.Current.GetService<DatabaseContext>();
                    db.Database.Migrate();
                
                    var hub = Locator.Current.GetService<Hub>();
                    var task = Task.Factory.StartNew(
                        () => hub.Start(),
                        TaskCreationOptions.LongRunning);
                    
                    task.ContinueWith(t =>
                    {
                        var exception = t.Exception;
                        if (exception != null)
                        {
                            // TODO: handle exception and shutdown
                        }
                    });
                };

                application.Disposing += (sender, args) =>
                {
                    var hub = Locator.Current.GetService<Hub>();
                    hub.Stop();
                };
                
                return application;
            });
        }
        
        public static void AddAuthentication(this IMutableDependencyResolver services)
        {
            //
        }
        
        public static void AddMessenger(this IMutableDependencyResolver services)
        {   
            // messenger
            services.RegisterLazySingleton<IBasicMessageModelFactory>(() =>
            {
                return new BasicMessageModelFactory();
            });
            
            services.RegisterLazySingleton<INoteMessageModelFactory>(() =>
            {
                return new NoteMessageModelFactory();
            });
            
            services.RegisterLazySingleton<ISpecialMessageModelFactory>(() =>
            {
                var stringFormatter = new StringFormatter();
                return new SpecialMessageModelFactory(stringFormatter);
            });
            
            services.RegisterLazySingleton<IVisualMessageModelFactory>(() =>
            {
                return new VisualMessageModelFactory();
            });
            
            services.RegisterLazySingleton<IMessageModelFactory>(() =>
            {
                var basicMessageModelFactory = Locator.Current.GetService<IBasicMessageModelFactory>();
                var noteMessageModelFactory = Locator.Current.GetService<INoteMessageModelFactory>();
                var specialMessageModelFactory = Locator.Current.GetService<ISpecialMessageModelFactory>();
                var visualMessageModelFactory = Locator.Current.GetService<IVisualMessageModelFactory>();
                
                var stringFormatter = new StringFormatter();
                
                return new MessageModelFactory(
                    basicMessageModelFactory,
                    noteMessageModelFactory,
                    specialMessageModelFactory,
                    visualMessageModelFactory,
                    stringFormatter);
            });
        }
        
        public static void AddSettings(this IMutableDependencyResolver services)
        {
            //
        }
        
        public static void AddWorkspace(this IMutableDependencyResolver services)
        {
            //
        }
    }
}