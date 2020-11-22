using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.Net;
using System.Threading.Tasks;

namespace TcpLogger
{
    internal class Program
    {
        private static IServiceProvider serviceProvider;
        private static ILogger log;

        private static async Task<int> Main(string[] args)
        {
            var services = new ServiceCollection()
                .AddLogging(o => { o.AddConsole(); o.SetMinimumLevel(LogLevel.Information); });

            services.AddSingleton<Server>()
                .AddTransient<ServerHandler>()
                .AddTransient<ClientHandler>()
                .AddSingleton<HexDumpService>();

            serviceProvider = services.BuildServiceProvider();

            log = serviceProvider.GetService<ILoggerFactory>()
                .CreateLogger<Program>();

            RootCommand rootCommand = new RootCommand(
              description: "TCP 数据记录器。")
            {
                new Argument<ushort>("listen_port", "监听端口。") { Arity = ArgumentArity.ExactlyOne },
                new Argument<string>("target_host", "目标地址。") { Arity = ArgumentArity.ExactlyOne },
                new Argument<string>("target_port", "目标端口。") { Arity = ArgumentArity.ZeroOrOne },
            };

            rootCommand.Handler = CommandHandler.Create<ushort, string, ushort>(Build);

            return await rootCommand.InvokeAsync(args);
        }

        private static async Task<int> Build(ushort listen_port, string target_host, ushort target_port)
        {
            if (listen_port == 0)
            {
                log.LogError("监听端口不正确。");
                return 1;
            }
            if (target_port == 0)
                target_port = listen_port;

            IPEndPoint ip = null;
            try
            {
                ip = new IPEndPoint(IPAddress.Parse(target_host), target_port);
            }
            catch (Exception ex)
            {
                log.LogError(ex.Message);
                return 1;
            }

            log.LogInformation($"{nameof(listen_port)}: {listen_port}, {nameof(target_host)}: {target_host}, {nameof(target_port)}: {target_port}");
            var server = serviceProvider.GetService<Server>();
            await server.Start(listen_port, ip, default);
            return 0;
        }
    }
}