﻿using Microsoft.Extensions.DependencyInjection;

using Speedbump.DiscordEventHandlers;

namespace Speedbump
{
    internal class Program
    {
        static void Main(string[] args)
        {
            if (args is null || args.Length < 1 || !Directory.Exists(args[0]))
            {
                Console.WriteLine("Invalid Runtime Directory");
                return;
            }

            Directory.SetCurrentDirectory(args[0]);

            var lifetime = new Lifetime();
            var config = new JsonConfiguration(lifetime);

            var collection = new ServiceCollection()
                .AddSingleton(lifetime)
                .AddSingleton<IConfiguration>(config)
                .AddSingleton<ILogger, Logger>()
                .AddSingleton<DiscordManager>()
                .AddSingleton<ModerationHandler>()
                .AddSingleton<XPHandler>()
                .AddSingleton<FeedbackHandler>()
                .AddSingleton<RoleHandler>();

            var provider = collection.BuildServiceProvider();

            var logger = provider.GetService<ILogger>();
            logger.Information("Hello, world!");

            AppDomain.CurrentDomain.UnhandledException += (s, e) => 
                CurrentDomain_UnhandledException(s, e, logger, lifetime);

            SqlInstance.Init(config, logger);

            provider.GetService<DiscordManager>();
            provider.GetService<ModerationHandler>();
            provider.GetService<XPHandler>();
            provider.GetService<FeedbackHandler>();
            provider.GetService<RoleHandler>();

            File.Create("lock").Dispose();

            while (true)
            {
                var input = Console.ReadLine().ToLower().Trim();
                switch (input)
                {
                    case "exit":
                        logger.Information("Shutting down...");
                        File.Delete("lock");
                        lifetime.End(Lifetime.ExitCause.Normal);
                        Environment.Exit(0);
                        break;
                }
            }
        }

        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e, ILogger logger, Lifetime lifetime)
        {
            try
            {
                logger.Critical(e.ExceptionObject);

            } catch { }
            if (e.IsTerminating)
            {
                try
                {
                    lifetime.End(Lifetime.ExitCause.Exception);
                } catch { }
            }
        }
    }
}