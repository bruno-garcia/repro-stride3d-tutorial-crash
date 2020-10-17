using Sentry;
using Sentry.Extensibility;
using Stride.Engine;
using System;

namespace CSharpBasics
{
    class CSharpBeginnerApp
    {
        static void Main(string[] args)
        {
            using (SentrySdk.Init(o =>
            {
                o.Dsn = "https://5fd7a6cda8444965bade9ccfd3df9882@o117736.ingest.sentry.io/1188141";
                o.AddInAppExclude("Stride.");
            }))
            using (var game = new Game())
            {
                game.Activated += Game_Activated;
                game.Deactivated += Game_Deactivated;
                game.Exiting += Game_Exiting;
                game.UnhandledException += Game_UnhandledException;
                game.WindowCreated += Game_WindowCreated;
                SentrySdk.ConfigureScope(s =>
                {
                    s.AddEventProcessor(new StrideEventProcessor(game));
                });
                game.Run();
            }
        }

        private static void Game_UnhandledException(object sender, Stride.Games.GameUnhandledExceptionEventArgs ex)
        {
            // Is this needed? This code already exists inside Sentry's SDK on AppDomain.UnhandledException
            if (ex.ExceptionObject is Exception e)
            {
                e.Data["Sentry:Handled"] = false;
                e.Data["Sentry:Mechanism"] = "Game.UnhandledException";
                SentrySdk.CaptureException(e);
            }

            if (ex.IsTerminating)
            {
                SentrySdk.Close(); // Flush events and close.
            }
        }

        private static void Game_WindowCreated(object sender, System.EventArgs e) => SentrySdk.AddBreadcrumb("Game Window Created", "app.lifecycle");
        private static void Game_Exiting(object sender, System.EventArgs e) => SentrySdk.AddBreadcrumb("Game Exiting", "app.lifecycle");
        private static void Game_Activated(object sender, System.EventArgs e) => SentrySdk.AddBreadcrumb("Game Activated", "app.lifecycle");
        private static void Game_Deactivated(object sender, System.EventArgs e) => SentrySdk.AddBreadcrumb("Game Deactivated", "app.lifecycle");
    }

    class StrideEventProcessor : ISentryEventProcessor
    {
        private readonly Game _game;

        public StrideEventProcessor(Game game) => _game = game;

        public SentryEvent Process(SentryEvent @event)
        {
            @event.Contexts["launch parameters"] = _game.LaunchParameters;
            foreach (var item in _game.Tags)
            {
                // TODO: Figure out what these tags are (value is of type Object, key is a complex object)
                @event.SetTag(item.Key.Name, item.Value.ToString());
            }
            return @event;
        }
    }
}
